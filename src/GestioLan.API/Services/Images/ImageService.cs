using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using GestioLan.API.Models;
using GestioLan.API.Utils.Helpers; // Per la classe StringHelper e CurrentUserService
using Serilog.Context;

namespace GestioLan.API.Services.Images;

public class ImageService : IImageService
{
    private readonly GestioLanContext _context;
    private readonly ILogger<ImageService> _logger;

    private readonly string _itemsFolder;

    public ImageService(GestioLanContext context, IConfiguration config, 
        ILogger<ImageService> logger)
    {
        _context = context;
        _logger = logger;   // serve per loggare

        _itemsFolder = config["Storage:ItemsPath"] ?? "/app/data/uploads/items";
    }


    // Restituisce la lista di tutte le immagini con le info base (IdImage, FileName, ItemsCount, LastModified)
    public async Task<IEnumerable<object>> GetAllImagesInfoAsync()
    {
        _logger.LogInformation("Retrieving list of all images");
        var images = await _context.Images
            .Select(img => new {
                img.IdImage,
                img.FileName,
                img.ItemsCount,
                img.LastModified
            })
            .ToListAsync();

        _logger.LogInformation("Returned {Count} images", images.Count);
        return images;
    }

    // Restituisce i byte dell'immagine cercandola per nome file
    // Lancia FileNotFoundException se il file non esiste su disco
    public async Task<byte[]> GetImageByNameAsync(string itemImageName)
    {
        // qui si costruisce il percorso interno al container
        // Questo cercherà PRIMA nei User Secrets, poi nelle variabili d'ambiente, poi nel JSON
        // _itemsFolder
        _logger.LogInformation("Image requested by name: {ItemImageName}", itemImageName);
        var filePath = Path.Combine(_itemsFolder, itemImageName);

        // Controlla se il file esiste davvero
        if (!System.IO.File.Exists(filePath))
        {
            _logger.LogWarning("Image file not found on disk: {ItemImageName}", itemImageName);
            throw new FileNotFoundException($"Not Found!");
        }

        // Legge il file e restituisce i byte
        var imageBytes = await System.IO.File.ReadAllBytesAsync(filePath);
        _logger.LogInformation("Image {ItemImageName} served successfully", itemImageName);
        return imageBytes;        
    }

    // Restituisce i byte dell'immagine cercandola per ID nel database
    // Lancia KeyNotFoundException se il record non esiste
    // Lancia FileNotFoundException se il file non esiste su disco
    public async Task<byte[]> GetImageByIdAsync(int idImage)
    {
        // cerca l'id nella tabella e recupera il nome dell'immagine
        _logger.LogInformation("Image requested by ID: {IdImage}", idImage);
        var image = await _context.Images.FindAsync(idImage);

        // se esiste continua, altrimenti errore notfound
        if (image == null)
        {
            _logger.LogWarning("Image record with ID {IdImage} not found in DB", idImage);
            throw new KeyNotFoundException("image search with ID not found");
        }
        string itemImageName = image.FileName;

        var filePath = Path.Combine(_itemsFolder, itemImageName);

        // Controlla se il file esiste davvero
        if (!System.IO.File.Exists(filePath))
        {
            _logger.LogWarning("Image file not found on disk for ID {IdImage}: {FileName}", idImage, itemImageName);
            throw new FileNotFoundException($"Not Found!");
        }

        // Legge il file e restituisce i byte
        var imageBytes = await System.IO.File.ReadAllBytesAsync(filePath);
        _logger.LogInformation("Image with ID {IdImage} served successfully", idImage);
        return imageBytes;
    }

    // Restituisce la lista di immagini con ItemsCount <= qty
    public async Task<IEnumerable<object>> GetImagesByItemsCountAsync(int qty)
    {
        var images = await _context.Images
            .Select(img => new {
                img.IdImage,
                img.FileName,
                img.ItemsCount,
                img.LastModified
            })
            .Where(img => img.ItemsCount <= qty)
            .ToListAsync();

        _logger.LogInformation("Returned {Count} images with ItemsCount <= {Qty}", images.Count, qty);

        return images;
    }

    // Carica una nuova immagine su disco e crea il record nel database
    // Restituisce il messaggio di conferma con il percorso
    // Lancia ArgumentException se il file è null o vuoto
    public async Task<string> CreateImageAsync(string? itemName, IFormFile file)
    {
        _logger.LogInformation("Creating image for item: {ItemName}", itemName ?? "unknown");

        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("File is null or empty");
            throw new ArgumentException("No file selected");
        }

        // prendo il percorso di base dalla configurazione (User Secrets o Environment)
        // Assicuriamo che la cartella esista
        if (!Directory.Exists(_itemsFolder))
        {
            Directory.CreateDirectory(_itemsFolder);
            _logger.LogInformation("Created missing target directory: {ItemsFolder}", _itemsFolder);
        }

        // Creazione del nome del file e il percorso completo
        string fileName = await UploadImage(_itemsFolder, file, itemName ?? "unknown");
        _logger.LogInformation("Image saved to disk: {FileName}", fileName);

        // Creazione del record nel database
        var newImage = new Image
        {
            FileName = fileName
        };
        _context.Images.Add(newImage);
        await _context.SaveChangesAsync();

        _logger.LogInformation("DB record created with ID: {IdImage}", newImage.IdImage);
        return $"DONE: {fileName}";
    }

    // Sostituisce il file immagine su disco e aggiorna il record e gli Item collegati
    // Restituisce il messaggio di conferma
    // Lancia ArgumentException se il file è null o vuoto
    // Lancia KeyNotFoundException se il record non esiste
    public async Task<string> UpdateImageAsync(int id, string? itemName, IFormFile file)
    {
        // Controlli generici:
        _logger.LogInformation("Updating image with ID: {Id}", id);

        // Vede se l'utente ha inserito un immagine
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("File is null or empty for update of ID: {Id}", id);
            throw new ArgumentException("No file selected.");
        }

        // mi riassicuro che la cartella esista
        if (!Directory.Exists(_itemsFolder))
        {
            Directory.CreateDirectory(_itemsFolder);
            _logger.LogInformation("Created missing target directory: {directory}", _itemsFolder);
        }

        // vede se il record esiste
        var imageRecord = await _context.Images.FindAsync(id);
        if (imageRecord == null)
        {
            _logger.LogWarning("Image record with ID {Id} not found in DB", id);
            throw new KeyNotFoundException($"Cannot update image. Record with ID {id} does not exist.");
        }

        string itemImageName = imageRecord.FileName;
        var filePath = Path.Combine(_itemsFolder, itemImageName);

        // cerca la vecchia immagine, controlla se il file esiste davvero, e nel caso lo elimina
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
            Console.WriteLine("Deleted old image file: {filePath}", filePath);
        }
        else
        {
            _logger.LogWarning("Old image file not found on disk at: {filePath}", filePath);
        }

        // carica la nuova immagine e aggiorna il record
        string newFileName = await UploadImage(_itemsFolder, file, itemName ?? "unknown");
        _logger.LogInformation("New image saved to disk: {NewFileName}", newFileName);

        // salva il record con il nuovo nome dell'immagine
        imageRecord.FileName = newFileName;
        // _context.Images.Update(imageRecord);
        // ridondante perche ef lo traccia visto che è async, ed è tracciato dal "ChangeTracker" di ef

        // Cercare ogni item con idImage = a questo e aggiornare il nome immagine
        var itemsWithThisImage = await _context.Items.Where(item => item.IdImage == id).ToListAsync();
        _logger.LogInformation("Updating ImageName on {Count} item(s) linked to image ID: {Id}", itemsWithThisImage.Count, id);
        foreach (var item in itemsWithThisImage)
        {
            // item.IdImage = id; // in realtà non cambia nulla
            item.ImageName = newFileName; // aggiorna il nome dell'immagine negli item che usano questa immagine
            // _context.Items.Update(item); // sempre ridondante perche la ricerca è asyncrona
        }
        await _context.SaveChangesAsync();

        _logger.LogInformation("Image with ID {Id} updated successfully", id);
        return "DONE";
    }

    // Rinomina il file immagine su disco e aggiorna il record e gli Item collegati
    // Restituisce il messaggio di conferma
    // Lancia KeyNotFoundException se il record non esiste
    // Lancia FileNotFoundException se il file da rinominare non esiste su disco
    public async Task<string> RenameImageAsync(int id, string? itemName)
    {
        // Legge il percorso di base dalla configurazione (User Secrets o Environment)
        _logger.LogInformation("Renaming image with ID: {Id} to item name: {ItemName}", id, itemName ?? "unknown");

        var imageRecord = await _context.Images.FindAsync(id);
        if (imageRecord == null)
        {
            _logger.LogWarning("Image record with ID {Id} not found in DB", id);
            throw new KeyNotFoundException("you cant update a record that does not exist!");
        }

        string oldFileName = imageRecord.FileName;
        var oldFilePath = Path.Combine(_itemsFolder, oldFileName);

        string newFileName = GenerateUniqueFilename(itemName ?? "unknown"); // genera un nuovo nome basato sull'itemName o "unknown" se itemName è null o vuoto
        var newFilePath = Path.Combine(_itemsFolder, newFileName);

        if (System.IO.File.Exists(oldFilePath))
        {
            System.IO.File.Move(oldFilePath, newFilePath);
            _logger.LogWarning("Image record with ID {Id} not found in DB", id);
            imageRecord.FileName = newFileName;
            //_context.Images.Update(imageRecord);
        }
        else
        {
            _logger.LogWarning("File to rename not found on disk: {OldFilePath}", oldFilePath);
            throw new FileNotFoundException($"file da rinominare non trovato: {oldFilePath}");
        }

        // OPZIONALE: cercare ogni item con idImage = a questo e aggiornare il nome immagine
        var itemsWithThisImage = await _context.Items.Where(item => item.IdImage == id).ToListAsync();
        _logger.LogInformation("Updating ImageName on {Count} item(s) linked to image ID: {Id}", itemsWithThisImage.Count, id);

        foreach (var item in itemsWithThisImage)
        {
            // item.IdImage = id; // in realtà non cambia nulla
            item.ImageName = newFileName; // aggiorna il nome dell'immagine negli item che usano questa immagine
            // _context.Items.Update(item); // ridondanza perche asyncrona
        }
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Image with ID {Id} renamed successfully", id);
        return "DONE";
    }

    // Elimina il file immagine su disco, rimuove il record e azzera i riferimenti negli Item
    // Restituisce il messaggio di conferma
    // Lancia KeyNotFoundException se il record non esiste
    public async Task<string> DeleteImageAsync(int id)
    {
        // Controlla se il record esiste
        _logger.LogInformation("Deleting image with ID: {Id}", id);

        var imageRecord = await _context.Images.FindAsync(id);
        if (imageRecord == null)
        {
            _logger.LogWarning("Image record with ID {Id} not found in DB", id);
            throw new KeyNotFoundException("you cant delete a record that does not exist!");
        }

        string itemImageName = imageRecord.FileName;
        var filePath = Path.Combine(_itemsFolder, itemImageName);
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
            _logger.LogInformation("Deleted image file: {FilePath}", filePath);

        }
        else
        {
            _logger.LogWarning("Image file not found on disk, skipping file deletion: {FilePath}", filePath);
        }

        // elimina ogni riferimento a questa immagine negli item (id_image = null)
        var itemsWithThisImage = await _context.Items.Where(item => item.IdImage == id).ToListAsync();
        _logger.LogInformation("Clearing image reference on {Count} item(s) linked to image ID: {Id}", itemsWithThisImage.Count, id);

        foreach (var item in itemsWithThisImage)
        {
            item.IdImage = null;
            item.ImageName = null;
            //_context.Items.Update(item);
        }

        // elimina il record
        _context.Images.Remove(imageRecord);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Image with ID {Id} deleted successfully", id);
        return "DONE";
    }

    // Salva un'immagine da uno Stream (usato dai plugin del MetadataService, non dal controller)
    // Restituisce l'IdImage del record creato nel DB
    public async Task<int> SaveImageFromStreamAsync(Stream imageStream, string suggestedExtension, string? itemName )
    {
        _logger.LogInformation("Saving image from stream for item: {ItemName}, extension: {Extension}", itemName ?? "unknown", suggestedExtension ?? ".jpg");

        if (!Directory.Exists(_itemsFolder))
        {
            Directory.CreateDirectory(_itemsFolder);
            _logger.LogInformation("Created missing target directory: {ItemsFolder}", _itemsFolder);
        }


        // Riusa GenerateUniqueFilename per coerenza — se itemName è null usa "unknown"
        // Poi sostituisce l'estensione con quella suggerita dal plugin (es. ".png", ".webp")
        string fileName = GenerateUniqueFilename(itemName ?? "unknown", suggestedExtension ?? ".jpg");           // es. "aB3xKq1z_coca_cola.jpg"

        var filePath = Path.Combine(_itemsFolder, fileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await imageStream.CopyToAsync(fileStream);
            _logger.LogInformation("Plugin image saved to disk: {FilePath}", filePath);
        }

        var newImage = new Image { FileName = fileName };
        _context.Images.Add(newImage);
        await _context.SaveChangesAsync();

        _logger.LogInformation("DB record created with ID: {IdImage} for plugin image: {FileName}", newImage.IdImage, fileName);
        return newImage.IdImage;
    }

    // --- Metodi privati di supporto ---

    private async Task<string> UploadImage(string baseFolder, IFormFile file, string itemName = "unknown")
    {
        var fileName = GenerateUniqueFilename(itemName); // Genera un nome unico basato sull'itemName
        var newFilePath = Path.Combine(baseFolder, fileName);

        // Salviamo il file fisicamente (sovrascrive se esiste già)
        using (var stream = new FileStream(newFilePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
           _logger.LogInformation("File uploaded to: {BaseFolder}/{FileName}", baseFolder, fileName);
        }

        return fileName;
    }

    private string GenerateUniqueFilename(string itemName = "unknown", string extension = ".jpg")
    {
        string itemNameSanitized = itemName.Replace(" ", "_"); // Sostituisce gli spazi con underscore
        string randStr = StringHelper.GenerateRandomString(8); // Genera una stringa casuale per evitare conflitti di nome
        var fileName = $"{randStr}_{itemNameSanitized}{extension}"; // Forziamo .jpg come deciso
        _logger.LogDebug("Generated unique filename: {FileName}", fileName);
        return fileName;
    }
}