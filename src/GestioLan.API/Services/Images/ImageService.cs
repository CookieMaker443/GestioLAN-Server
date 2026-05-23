using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using GestioLan.API.Models;
using GestioLan.API.Utils.Helpers; // Per la classe StringHelper

namespace GestioLan.API.Services.Images;

public class ImageService : IImageService
{
    private readonly GestioLanContext _context;
    private readonly string _itemsFolder;

    public ImageService(GestioLanContext context, IConfiguration config)
    {
        _context = context;

        _itemsFolder = config["Storage:ItemsPath"] ?? "/app/data/uploads/items";
    }

    // Restituisce la lista di tutte le immagini con le info base (IdImage, FileName, ItemsCount, LastModified)
    public async Task<IEnumerable<object>> GetAllImagesInfoAsync()
    {
        var images = await _context.Images
            .Select(img => new {
                img.IdImage,
                img.FileName,
                img.ItemsCount,
                img.LastModified
            })
            .ToListAsync();

        return images;
    }

    // Restituisce i byte dell'immagine cercandola per nome file
    // Lancia FileNotFoundException se il file non esiste su disco
    public async Task<byte[]> GetImageByNameAsync(string itemImageName)
    {
        // qui si costruisce il percorso interno al container
        // Questo cercherà PRIMA nei User Secrets, poi nelle variabili d'ambiente, poi nel JSON
        // _itemsFolder
        var filePath = Path.Combine(_itemsFolder, itemImageName);

        // Controlla se il file esiste davvero
        if (!System.IO.File.Exists(filePath))
        {
            throw new FileNotFoundException($"Not Found!");
        }

        // Legge il file e restituisce i byte
        var imageBytes = await System.IO.File.ReadAllBytesAsync(filePath);
        return imageBytes;
    }

    // Restituisce i byte dell'immagine cercandola per ID nel database
    // Lancia KeyNotFoundException se il record non esiste
    // Lancia FileNotFoundException se il file non esiste su disco
    public async Task<byte[]> GetImageByIdAsync(int idImage)
    {
        // cerca l'id nella tabella e recupera il nome dell'immagine
        var image = await _context.Images.FindAsync(idImage);

        // se esiste continua, altrimenti errore notfound
        if (image == null)
        {
            throw new KeyNotFoundException("image search with ID not found");
        }
        string itemImageName = image.FileName;

        var filePath = Path.Combine(_itemsFolder, itemImageName);

        // Controlla se il file esiste davvero
        if (!System.IO.File.Exists(filePath))
        {
            throw new FileNotFoundException($"Not Found!");
        }

        // Legge il file e restituisce i byte
        var imageBytes = await System.IO.File.ReadAllBytesAsync(filePath);
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

        return images;
    }

    // Carica una nuova immagine su disco e crea il record nel database
    // Restituisce il messaggio di conferma con il percorso
    // Lancia ArgumentException se il file è null o vuoto
    public async Task<string> CreateImageAsync(string? itemName, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("Nessun file selezionato.");
        }

        // prendo il percorso di base dalla configurazione (User Secrets o Environment)
        // Assicuriamo che la cartella esista
        if (!Directory.Exists(_itemsFolder))
        {
            Directory.CreateDirectory(_itemsFolder);
        }

        // Creazione del nome del file e il percorso completo
        string fileName = await UploadImage(_itemsFolder, file, itemName ?? "unknown");

        // Creazione del record nel database
        var newImage = new Image
        {
            FileName = fileName
        };
        _context.Images.Add(newImage);
        await _context.SaveChangesAsync();

        return $"Immagine caricata con successo in: {_itemsFolder}/{fileName}";
    }

    // Sostituisce il file immagine su disco e aggiorna il record e gli Item collegati
    // Restituisce il messaggio di conferma
    // Lancia ArgumentException se il file è null o vuoto
    // Lancia KeyNotFoundException se il record non esiste
    public async Task<string> UpdateImageAsync(int id, string? itemName, IFormFile file)
    {
        // Controlli generici:
        // Vede se l'utente ha inserito un immagine
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("Nessun file selezionato.");
        }

        // mi riassicuro che la cartella esista
        if (!Directory.Exists(_itemsFolder))
        {
            Directory.CreateDirectory(_itemsFolder);
        }

        // vede se il record esiste
        var imageRecord = await _context.Images.FindAsync(id);
        if (imageRecord == null)
        {
            throw new KeyNotFoundException("you cant update a record that does not exist!");
        }

        string itemImageName = imageRecord.FileName;
        var filePath = Path.Combine(_itemsFolder, itemImageName);

        // cerca la vecchia immagine, controlla se il file esiste davvero, e nel caso lo elimina
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
            Console.WriteLine($"eliminato: {filePath}");
        }

        // carica la nuova immagine e aggiorna il record
        string newFileName = await UploadImage(_itemsFolder, file, itemName ?? "unknown");

        // salva il record con il nuovo nome dell'immagine
        imageRecord.FileName = newFileName;
        // _context.Images.Update(imageRecord);
        // ridondante perche ef lo traccia visto che è async, ed è tracciato dal "ChangeTracker" di ef

        // Cercare ogni item con idImage = a questo e aggiornare il nome immagine
        var itemsWithThisImage = await _context.Items.Where(item => item.IdImage == id).ToListAsync();
        foreach (var item in itemsWithThisImage)
        {
            // item.IdImage = id; // in realtà non cambia nulla
            item.ImageName = newFileName; // aggiorna il nome dell'immagine negli item che usano questa immagine
            // _context.Items.Update(item); // sempre ridondante perche la ricerca è asyncrona
        }
        await _context.SaveChangesAsync();

        return "Immagine caricata con successo";
    }

    // Rinomina il file immagine su disco e aggiorna il record e gli Item collegati
    // Restituisce il messaggio di conferma
    // Lancia KeyNotFoundException se il record non esiste
    // Lancia FileNotFoundException se il file da rinominare non esiste su disco
    public async Task<string> RenameImageAsync(int id, string? itemName)
    {
        // Legge il percorso di base dalla configurazione (User Secrets o Environment)
        var imageRecord = await _context.Images.FindAsync(id);
        if (imageRecord == null)
        {
            throw new KeyNotFoundException("you cant update a record that does not exist!");
        }

        string oldFileName = imageRecord.FileName;
        var oldFilePath = Path.Combine(_itemsFolder, oldFileName);

        string newFileName = GenerateUniqueFilename(itemName ?? "unknown"); // genera un nuovo nome basato sull'itemName o "unknown" se itemName è null o vuoto
        var newFilePath = Path.Combine(_itemsFolder, newFileName);

        if (System.IO.File.Exists(oldFilePath))
        {
            System.IO.File.Move(oldFilePath, newFilePath);
            Console.WriteLine($"rinominato: {oldFilePath} in {newFilePath}");
            imageRecord.FileName = newFileName;
            //_context.Images.Update(imageRecord);
        }
        else
        {
            Console.WriteLine($"file da rinominare non trovato: {oldFilePath}");
            throw new FileNotFoundException($"file da rinominare non trovato: {oldFilePath}");
        }

        // OPZIONALE: cercare ogni item con idImage = a questo e aggiornare il nome immagine
        var itemsWithThisImage = await _context.Items.Where(item => item.IdImage == id).ToListAsync();
        foreach (var item in itemsWithThisImage)
        {
            // item.IdImage = id; // in realtà non cambia nulla
            item.ImageName = newFileName; // aggiorna il nome dell'immagine negli item che usano questa immagine
            // _context.Items.Update(item); // ridondanza perche asyncrona
        }
        await _context.SaveChangesAsync();

        return "Immagine rinominata con successo";
    }

    // Elimina il file immagine su disco, rimuove il record e azzera i riferimenti negli Item
    // Restituisce il messaggio di conferma
    // Lancia KeyNotFoundException se il record non esiste
    public async Task<string> DeleteImageAsync(int id)
    {
        // Controlla se il record esiste
        var imageRecord = await _context.Images.FindAsync(id);
        if (imageRecord == null)
        {
            throw new KeyNotFoundException("you cant delete a record that does not exist!");
        }

        string itemImageName = imageRecord.FileName;
        var filePath = Path.Combine(_itemsFolder, itemImageName);
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
            Console.WriteLine($"eliminato: {filePath}");
        }
        else
        {
            Console.WriteLine($"file da eliminare non trovato: {filePath}");
        }

        // elimina ogni riferimento a questa immagine negli item (id_image = null)
        var itemsWithThisImage = await _context.Items.Where(item => item.IdImage == id).ToListAsync();
        foreach (var item in itemsWithThisImage)
        {
            item.IdImage = null;
            item.ImageName = null;
            //_context.Items.Update(item);
        }

        // elimina il record
        _context.Images.Remove(imageRecord);
        await _context.SaveChangesAsync();

        return "Immagine eliminata con successo";
    }

    // Salva un'immagine da uno Stream (usato dai plugin del MetadataService, non dal controller)
    // Restituisce l'IdImage del record creato nel DB
    public async Task<int> SaveImageFromStreamAsync(Stream imageStream, string suggestedExtension, string? itemName )
    {
        if (!Directory.Exists(_itemsFolder))
            Directory.CreateDirectory(_itemsFolder);

        // Riusa GenerateUniqueFilename per coerenza — se itemName è null usa "unknown"
        // Poi sostituisce l'estensione con quella suggerita dal plugin (es. ".png", ".webp")
        string baseName = GenerateUniqueFilename(itemName ?? "unknown");           // es. "aB3xKq1z_coca_cola.jpg"
        string fileName = Path.ChangeExtension(baseName, suggestedExtension);      // es. "aB3xKq1z_coca_cola.png"

        var filePath = Path.Combine(_itemsFolder, fileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await imageStream.CopyToAsync(fileStream);
            Console.WriteLine($"[ImageService] Immagine plugin salvata: {filePath}");
        }

        var newImage = new Image { FileName = fileName };
        _context.Images.Add(newImage);
        await _context.SaveChangesAsync();

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
            Console.WriteLine($"caricato: {baseFolder}/{fileName}");
        }

        return fileName;
    }

    private string GenerateUniqueFilename(string itemName = "unknown")
    {
        string itemNameSanitized = itemName.Replace(" ", "_"); // Sostituisce gli spazi con underscore
        string randStr = StringHelper.GenerateRandomString(8); // Genera una stringa casuale per evitare conflitti di nome
        var fileName = $"{randStr}_{itemNameSanitized}.jpg"; // Forziamo .jpg come deciso
        return fileName;
    }
}