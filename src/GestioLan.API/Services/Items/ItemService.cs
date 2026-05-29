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
        _logger.LogInformation("Retrieving items with filters: has_category={HasCategory}, id_category={IdCategory}, name={Name}, has_image={HasImage}, quantity={Quantity}, type_quantity={TypeQuantity}",
            has_category, id_category, name, has_image, quantity, type_quantity);

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

        var items = await query.ToListAsync();
        _logger.LogInformation("Returned {Count} items", items.Count);
        return items;

    }

    // Ottiene un singolo oggetto del DB tramite il suo ID
    public async Task<Item> GetItemByIdAsync(int id)
    {
        _logger.LogInformation("Retrieving item with ID: {Id}", id);

        var item = await _context.Items.FindAsync(id);

        if (item == null){
            _logger.LogWarning("Item with ID {Id} not found", id);
            throw new KeyNotFoundException($"Item with id {id} not found");
        }

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
        _logger.LogInformation("Creating new item: {ItemName}, Barcode: {Barcode}", item.ItemName ?? "unknown", item.Barcode ?? "none");

        bool nomeAssente = string.IsNullOrWhiteSpace(item.ItemName);
        bool tentaFetchNome = !string.IsNullOrEmpty(item.Barcode)
                              && item.IdCategory != null
                              && (nomeAssente || _apiOverrideMetadata);

        if (tentaFetchNome)
        {
            _logger.LogInformation("Attempting name fetch for barcode: {Barcode}", item.Barcode);
            try
            {
                string? fetchedName = await _metadataService.FetchNameAsync(
                    searchKey: item.Barcode!,
                    idCategory: item.IdCategory);

                if (!string.IsNullOrWhiteSpace(fetchedName)) {
                    _logger.LogInformation("Name fetched successfully: {FetchedName}", fetchedName);
                    item.ItemName = fetchedName;
                }
                else
                {
                    _logger.LogInformation("Name fetch returned no result for barcode: {Barcode}", item.Barcode);
                }

                // Se null e l'utente aveva un nome, rimane invariato
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Name fetch failed for barcode: {Barcode}. Reason: {Message}", item.Barcode, ex.Message);
            }
        }


        // BLOCCO IMMAGINE
        
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
                _logger.LogWarning("Image with ID {IdImage} not found in DB, item will be created without image", item.IdImage);
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

        // se esiste il bacILogger<UsersController> loggerode e l'immagine dell'utente è null oppure è possibile fare override, e ha una categoriaprova a fetchare
        if(tentaFetchImmagine)
        {
            _logger.LogInformation("Attempting image fetch for barcode: {Barcode}", item.Barcode);
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
                    _logger.LogWarning("Image fetch failed for barcode: {Barcode}. Reason: {Message}", item.Barcode, ex.Message);
                    // image rimane quella dell'utente, se c'era
                }

                // se sono qui allora sto facendo override oppure non ho caricato immagini
                // se non è null, l'immagine è stata scaricata e salvata, tornando il suo id
                if (fetchedImageId != null)
                {
                    // trovo l'immagine scaricata e la prendo
                    _logger.LogInformation("Image fetched successfully with ID: {FetchedImageId}", fetchedImageId);
                    image = await _context.Images.FindAsync(fetchedImageId);
                    // Se c'era già un'immagine manuale il plugin la sovrascrive,
                }
                else
                {
                    _logger.LogInformation("Image fetch returned no result for barcode: {Barcode}", item.Barcode);
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
            _logger.LogInformation("Image assigned to item: ID {IdImage}, FileName {FileName}", image.IdImage, image.FileName);
        }
        else
        {
            // non è stata caricata alcuna immagine valita, e non è stato possibile trovare un immaine dal provider
            _logger.LogInformation("No image assigned to item");
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
            _logger.LogInformation("Attempting description fetch for barcode: {Barcode}", item.Barcode);
            try
            {
                string? fetchedDescription = await _metadataService.FetchDescriptionAsync(
                    searchKey: item.Barcode!,
                    idCategory: item.IdCategory);

                if (!string.IsNullOrWhiteSpace(fetchedDescription)) {
                    _logger.LogInformation("Description fetched successfully for barcode: {Barcode}", item.Barcode);
                    item.Description = fetchedDescription;
                }
                else
                {
                    _logger.LogInformation("Description fetch returned no result for barcode: {Barcode}", item.Barcode);
                }

                // Se null e l'utente aveva una descrizione, rimane invariata
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Description fetch failed for barcode: {Barcode}. Reason: {Message}", item.Barcode, ex.Message);
            }
        }


        // aggiunge il nuovo oggetto al DB, con l'immagine se è stata specificata
        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Item created successfully with ID: {IdItem}, Name: {ItemName}", item.IdItem, item.ItemName);
        return item;
    }

    // Elimina un oggetto dal DB tramite il suo ID
    public async Task DeleteItemAsync(int id)
    {
        _logger.LogInformation("Deleting item with ID: {Id}", id);

        var item = await _context.Items.FindAsync(id);
        if (item == null)
        {
            _logger.LogWarning("Item with ID {Id} not found", id);
            throw new KeyNotFoundException($"Item con id {id} non trovato");
        }

        var image = await _context.Images.FindAsync(item.IdImage);
        if (image != null)
        {
            image.ItemsCount--;
            _logger.LogInformation("Decremented ItemsCount for image ID {IdImage}, new count: {ItemsCount}", image.IdImage, image.ItemsCount);
        }

        _context.Items.Remove(item);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Item with ID {Id} deleted successfully", id);
    }

    // Aggiorna un oggetto esistente nel DB
    public async Task UpdateItemAsync(int id, Item updatedItem)
    {
        _logger.LogInformation("Updating item with ID: {Id}", id);

        if (id != updatedItem.IdItem)
        {
            _logger.LogWarning("ID mismatch: route ID {Id} does not match body ID {BodyId}", id, updatedItem.IdItem);
            throw new ArgumentException("Id mismatch");
        }

        var item = await _context.Items.FindAsync(id);
        if (item == null)
        {
            _logger.LogWarning("Item with ID {Id} not found", id);
            throw new KeyNotFoundException($"Item wit id {id} not founf");
        }


        item.ItemName = updatedItem.ItemName;
        item.Description = updatedItem.Description;

        item.IdCategory = updatedItem.IdCategory;
        item.Quantity = updatedItem.Quantity;
        item.TypeQuantity = updatedItem.TypeQuantity;

        // se il vecchio e nuovo item sono senza immagine, "pulisce" i campi immagine e non fa nulla
        if ((item.IdImage != updatedItem.IdImage) && updatedItem.IdImage != null)
        {
            _logger.LogInformation("Image changed for item ID {Id}: {OldImageId} -> {NewImageId}", id, item.IdImage, updatedItem.IdImage);

            var newImage = await _context.Images.FindAsync(updatedItem.IdImage);
            if (newImage != null)
            {
                // abbassa contatore vecchia immagine
                if (item.IdImage != null)
                {
                    var oldImage = await _context.Images.FindAsync(item.IdImage);
                    if (oldImage != null)
                    {
                        oldImage.ItemsCount--;
                        _logger.LogInformation("Decremented ItemsCount for old image ID {IdImage}, new count: {ItemsCount}", oldImage.IdImage, oldImage.ItemsCount);
                    }

                }

                // aumenta contatore nuova immagine
                newImage.ItemsCount++;
                item.ImageName = newImage.FileName;
                item.IdImage = updatedItem.IdImage;
                _logger.LogInformation("Incremented ItemsCount for new image ID {IdImage}, new count: {ItemsCount}", newImage.IdImage, newImage.ItemsCount);
            }
            else
            {
                _logger.LogWarning("New image with ID {IdImage} not found, item updated without image", updatedItem.IdImage);
                // immagine specificata non esiste, pulisce i campi e avvisa
                if (item.IdImage != null)
                {
                    var oldImage = await _context.Images.FindAsync(item.IdImage);
                    if (oldImage != null)
                    {
                        oldImage.ItemsCount--;
                        _logger.LogInformation("Decremented ItemsCount for old image ID {IdImage}, new count: {ItemsCount}", oldImage.IdImage, oldImage.ItemsCount);
                    }

                }
                item.IdImage = null;
                item.ImageName = null;
                Console.WriteLine($"Immagine con id {updatedItem.IdImage} non trovata, item aggiornato senza immagine");
            }
        }
        else if (updatedItem.IdImage == null)
        // se il nuovo item è senza immagine, abbassa il contatore della vecchia immagine (se esiste) e pulisce i campi immagine senza fare query inutili
        {
            _logger.LogInformation("Image removed from item ID {Id}", id);

            if (item.IdImage != null)
            {
                var oldImage = await _context.Images.FindAsync(item.IdImage);
                if (oldImage != null)
                {
                    oldImage.ItemsCount--;
                    _logger.LogInformation("Decremented ItemsCount for image ID {IdImage}, new count: {ItemsCount}", oldImage.IdImage, oldImage.ItemsCount);
                }

            }
            item.IdImage = null;
            item.ImageName = null;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Item with ID {Id} updated successfully", id);
    }
}