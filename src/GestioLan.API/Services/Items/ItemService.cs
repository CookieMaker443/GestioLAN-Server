using Microsoft.EntityFrameworkCore;
using GestioLan.API.Models;

namespace GestioLan.API.Services.Items;

public class ItemService : IItemService
{
    private readonly GestioLanContext _context;

    public ItemService(GestioLanContext context)
    {
        _context = context;
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
        if (item.IdImage == 0 || item.IdImage == null)
        {
            item.IdImage = null;
            item.ImageName = null;
        }

        // se l'immagine dell'item non è null, aggiorna il counter di quell'immagine
        // se ha un'immagine, verifica che esista e aggiorna il contatore
        if (item.IdImage != null)
        {
            var image = await _context.Images.FindAsync(item.IdImage);
            if (image == null)
            {
                // immagine specificata non esiste, pulisce i campi e avvisa
                item.IdImage = null;
                item.ImageName = null;
                Console.WriteLine($"Immagine con id {item.IdImage} non trovata, item aggiunto senza immagine");
            }
            else
            {
                image.ItemsCount++;
                item.ImageName = image.FileName;
            }
        }
        else
        {
            item.ImageName = null;
        }

        /* pseudocodice
        if item.barcode != null
        try{
            // se ce il barcode devo capire se è un barcode di cibo o di un altra categoria

            if(item.category != null)
            // se ce la  categoria (che è una bitmask) devo capire quale categoria ha, e se nelcaso quella categoria ha un provider associato
            (openfooddacts per cibo o un altra api peraltro tipo ardiuino), devo passargli questa istanza specifica, ltrimenti passa null

            IMetadataProvider provider = istanzaPassata (in qualche modo)

            if provider == null
            return 

            IFormFile image = provider.GetImage(); 
            string imageToAd = provider.GetImageName(item.barcode);
            
            // aggiunge limmagine e tiene il riferimento dell immagine in modo da poterlo salvare nell item
            var riferimentoImmagine = _context.ImageController.GetImageId(imageToAdd);

            item.idImage = riferimentoImmagine.id;
            item.imageName = riferimentoImmagine.name;
        } catch (Exception ex)
        {
            // se c'è un errore con il provider, logga l'errore e continua ad aggiungere l'item senza immagine
            Console.WriteLine($"Errore con il provider per il barcode {item.barcode}: {ex.Message}");
        }
        */

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