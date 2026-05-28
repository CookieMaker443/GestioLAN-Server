using Microsoft.EntityFrameworkCore;
using GestioLan.API.Models;
using GestioLan.API.Services.Metadata;

namespace GestioLan.API.Services.Items;

public class ItemService : IItemService
{
    private readonly GestioLanContext _context;
    private readonly ILogger<ItemService> _logger;
    private readonly IMetadataService _metadataService;
    private readonly bool _apiOverrideMetadata;


    public ItemService(GestioLanContext context, IMetadataService metadataService, 
        IConfiguration config, ILogger<ItemService> logger)
    {
        _context = context;
        _logger = logger;
       _metadataService = metadataService;

    _apiOverrideMetadata = config.GetValue<bool>("Metadata:ApiOverrideMetadata");


    }

    // Ottiene tutti gli oggetti del DB con filtri opzionali
    public async Task<IEnumerable<Item>> GetItemsAsync(
        bool? has_category,
        int? id_category,
        string? name,
        bool? has_image,
        int? quantity,
        string? type_quantity)
    {
        IQueryable<Item> query = _context.Items;

        if (has_category.HasValue)
        {
            if (has_category.Value)
                query = query.Where(item => item.IdCategory != null);
            else
                query = query.Where(item => item.IdCategory == null);
        }

        if (id_category.HasValue)
        {
            // controllare il WHERE per la bitmask
            query = query.Where(item =>
                        (item.IdCategory & id_category.Value) == id_category.Value &&
                        item.IdCategory != null);
        }

        if (!string.IsNullOrEmpty(name))
        {
            query = query.Where(item => item.ItemName.Contains(name));
        }

        if (has_image.HasValue)
        {
            if (has_image.Value)
                query = query.Where(item => item.IdImage != null);
            else
                query = query.Where(item => item.IdImage == null);
        }

        if (quantity.HasValue)
            query = query.Where(item => item.Quantity == quantity.Value);

        if (!string.IsNullOrEmpty(type_quantity))
            query = query.Where(item => item.TypeQuantity == type_quantity);

        return await query.ToListAsync();
    }

    // Ottiene un singolo oggetto del DB tramite il suo ID
    public async Task<Item> GetItemByIdAsync(int id)
    {
        var item = await _context.Items.FindAsync(id);

        if (item == null)
            throw new KeyNotFoundException($"Item con id {id} non trovato");

        return item;
    }

    // Crea un nuovo oggetto nel DB
    public async Task<Item> CreateItemAsync(Item item)
    {

        // BLOCCO NOME 
        // nome assente OPPURE override → tenta fetch
        // 
        // NomeAssente | Override → Fetch?
        //      0      |    0    →  No
        //      0      |    1    →  Sì
        //      1      |    0    →  No
        //      1      |    1    →  Sì   (override sovrascrive)

        bool nomeAssente = string.IsNullOrWhiteSpace(item.ItemName);
        bool tentaFetchNome = !string.IsNullOrEmpty(item.Barcode)
                              && item.IdCategory != null
                              && (nomeAssente || _apiOverrideMetadata);

        if (tentaFetchNome)
        {
            try
            {
                string? fetchedName = await _metadataService.FetchNameAsync(
                    searchKey: item.Barcode!,
                    idCategory: item.IdCategory);

                if (!string.IsNullOrWhiteSpace(fetchedName)) {
                    _logger.LogInformation("Fetch nome avvenuto");
                    item.ItemName = fetchedName;
                }
                // Se null e l'utente aveva un nome, rimane invariato
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Fetch nome fallito: {ex.Message}", ex.Message);
            }
        }


        // BLOCCO IMMAGINE
        bool userHasImage = false;
        Image? image = null;
        // Normalizza: 0 equivale a "nessuna immagine"
        if (item.IdImage == 0 || item.IdImage == null)
        {
            item.IdImage = null;
            item.ImageName = null;
        }

        // se l'immagine dell'item non è null, controlla se esiste nel db(quindi che l id non è un errore e sia valido)
        if (item.IdImage != null)
        {
            image = await _context.Images.FindAsync(item.IdImage);
            if (image == null)
            {
                // immagine specificata non esiste, pulisce i campi e avvisa
                item.IdImage = null;
                item.ImageName = null;
                userHasImage = false;
                _logger.LogInformation("Immagine con id {item.IdImage} non trovata, item aggiunto senza immagine", item.IdImage);
            }
            // se l'utente ha caricato un immagine valida, image la conterrà
        }

        // 0 (Immagine Null / Non Esiste) + 0 (Override False) = True
        // 0 (Immagine Null / Non Esiste) + 1 (Override True) = True
        // 1 (Immagine Valida / Esiste) + 0 (Override False) = False
        // 1 (Immagine Valida / Esiste) + 1 (Override True) = True
        // L'espressione è vera se l'immagine non esiste OPPURE se l'override è attivo
        bool immagineNonEsiste = image == null;
        bool risultato = immagineNonEsiste || _apiOverrideMetadata;

        bool tentaFetchImmagine = !string.IsNullOrEmpty(item.Barcode) 
                                    && risultato 
                                    && item.IdCategory!= null;

        //prova a fare override

        // se esiste il bacode e l'immagine dell'utente è null oppure è possibile fare override, e ha una categoriaprova a fetchare
        if(tentaFetchImmagine)
        {
            // vede se è possibile fetchare lì'immagine da provider esterni 
            int? fetchedImageId = null;
                try
                {
                    fetchedImageId = await _metadataService.FetchAndSaveImageAsync(
                    searchKey: item.Barcode!,
                    idCategory: item.IdCategory,
                    itemName: item.ItemName);
                } catch (Exception ex)
                {
                    _logger.LogInformation("Fetch immagine fallito: {ex.Message}", ex.Message);
                    // image rimane quella dell'utente, se c'era
                }

                // se sono qui allora sto facendo override oppure non ho caricato immagini
                // se non è null, l'immagine è stata scaricata e salvata, tornando il suo id
                if (fetchedImageId != null)
                {
                    // trovo l'immagine scaricata e la prendo
                    _logger.LogInformation("Fetch immagine avvenuto");
                    image = await _context.Images.FindAsync(fetchedImageId);
                    // Se c'era già un'immagine manuale il plugin la sovrascrive,
                }
                // Se fetchedImageId è null e l'utente aveva messo un'immagine manuale,
                // non si tocca nulla — item.IdImage è già valorizzato correttamente
        }

        // l'immagine, se esiste è quella dell'utente, oppure trovata dall'api con override
        if(image != null)
        {
            image.ItemsCount++;
            item.ImageName = image.FileName;   
            item.IdImage = image.IdImage;
        }
        else
        {
            // non è stata caricata alcuna immagine valita, e non è stato possibile trovare un immaine dal provider
            item.ImageName=null;
            item.IdImage = null;
        }


        // BLOCCO DESCRIZIONE
        // descrizione assente OPPURE override → tenta fetch
        //
        // DescrizioneAssente | Override → Fetch?
        //         0          |    0    →  No
        //         0          |    1    →  Sì
        //         1          |    0    →  No
        //         1          |    1    →  Sì   (override sovrascrive)

        bool descrizioneAssente = string.IsNullOrWhiteSpace(item.Description);
        bool tentaFetchDescrizione = !string.IsNullOrEmpty(item.Barcode)
                                     && item.IdCategory != null
                                     && (descrizioneAssente || _apiOverrideMetadata);

        if (tentaFetchDescrizione)
        {
            try
            {
                string? fetchedDescription = await _metadataService.FetchDescriptionAsync(
                    searchKey: item.Barcode!,
                    idCategory: item.IdCategory);

                if (!string.IsNullOrWhiteSpace(fetchedDescription)) {
                    _logger.LogInformation("Fetch descrizione avvenuto");
                    item.Description = fetchedDescription;
                }
                // Se null e l'utente aveva una descrizione, rimane invariata
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Fetch descrizione fallito: {ex.Message}", ex.Message);
            }
        }


        // aggiunge il nuovo oggetto al DB, con l'immagine se è stata specificata
        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        return item;
    }

    // Elimina un oggetto dal DB tramite il suo ID
    public async Task DeleteItemAsync(int id)
    {
        var item = await _context.Items.FindAsync(id);
        if (item == null)
            throw new KeyNotFoundException($"Item con id {id} non trovato");

        var image = await _context.Images.FindAsync(item.IdImage);
        if (image != null)
        {
            image.ItemsCount--;
            // image.LastModified = DateTime.Now;
        }

        _context.Items.Remove(item);
        await _context.SaveChangesAsync();
    }

    // Aggiorna un oggetto esistente nel DB
    public async Task UpdateItemAsync(int id, Item updatedItem)
    {
        if (id != updatedItem.IdItem)
            throw new ArgumentException("Id mismatch");

        var item = await _context.Items.FindAsync(id);
        if (item == null)
            throw new KeyNotFoundException($"Item con id {id} non trovato");

        item.ItemName = updatedItem.ItemName;
        item.Description = updatedItem.Description;

        item.IdCategory = updatedItem.IdCategory;
        item.Quantity = updatedItem.Quantity;
        item.TypeQuantity = updatedItem.TypeQuantity;

        // se il vecchio e nuovo item sono senza immagine, "pulisce" i campi immagine e non fa nulla
        if ((item.IdImage != updatedItem.IdImage) && updatedItem.IdImage != null)
        {
            var newImage = await _context.Images.FindAsync(updatedItem.IdImage);
            if (newImage != null)
            {
                // abbassa contatore vecchia immagine
                if (item.IdImage != null)
                {
                    var oldImage = await _context.Images.FindAsync(item.IdImage);
                    if (oldImage != null)
                        oldImage.ItemsCount--;
                }

                // aumenta contatore nuova immagine
                newImage.ItemsCount++;
                item.ImageName = newImage.FileName;
                item.IdImage = updatedItem.IdImage;
            }
            else
            {
                // immagine specificata non esiste, pulisce i campi e avvisa
                if (item.IdImage != null)
                {
                    var oldImage = await _context.Images.FindAsync(item.IdImage);
                    if (oldImage != null)
                        oldImage.ItemsCount--;
                }
                item.IdImage = null;
                item.ImageName = null;
                Console.WriteLine($"Immagine con id {updatedItem.IdImage} non trovata, item aggiornato senza immagine");
            }
        }
        else if (updatedItem.IdImage == null)
        // se il nuovo item è senza immagine, abbassa il contatore della vecchia immagine (se esiste) e pulisce i campi immagine senza fare query inutili
        {
            if (item.IdImage != null)
            {
                var oldImage = await _context.Images.FindAsync(item.IdImage);
                if (oldImage != null)
                    oldImage.ItemsCount--;
            }
            item.IdImage = null;
            item.ImageName = null;
        }

        await _context.SaveChangesAsync();
    }
}