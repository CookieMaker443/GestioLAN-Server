using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestioLan.API.Models;
using GestioLan.API.Utils.Helpers; // Per la classe StringHelper
using Microsoft.AspNetCore.Authorization; // Per l'attributo [Authorize]


namespace GestioLan.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImagesController : ControllerBase
{
    private readonly GestioLanContext _context;
    private readonly string _itemsFolder;
    
    public ImagesController(GestioLanContext context, IConfiguration config)
    {
        _context = context;

        _itemsFolder = config["Storage:ItemsPath"] ?? "/app/data/uploads/items";
    }

    [Authorize] // Protegge questo endpoint, richiede un token JWT valido per accedervi
    [HttpGet("AllImagesInfo")]
    public async Task<IActionResult> GetAllImagesInfo()
    {
        var images = await _context.Images
                .Select(img => new { 
                    img.IdImage, 
                    img.FileName, 
                    img.ItemsCount,
                    img.LastModified})
                .ToListAsync();
        return Ok(images);
    }

    // NOTA: una chiamata per immagine di item, il client sarà responsabile del caching
    [Authorize]
    [HttpGet("ImageName/{itemImageName}")]
    public async Task<IActionResult> GetImageByName(string itemImageName)
    {
        // qui si costruisci il percorso interno al container
        // Questo cercherà PRIMA nei User Secrets, poi nelle variabili d'ambiente, poi nel JSON
        // _itemsFolder
        var filePath = Path.Combine(_itemsFolder, itemImageName);

        // 2. Controlla se il file esiste davvero
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound($"Not Found!");
        }

        // 3. Leggi il file e sputa fuori i byte
        var imageBytes = System.IO.File.ReadAllBytes(filePath);
        return File(imageBytes, "image/jpeg"); // Il browser/client vedrà un file immagine
    }

    [Authorize]
    [HttpGet("IdImage/{idImage}")]
    public async Task<IActionResult> GetImageById(int idImage)
    {
        // cerca l'id nella tabella e recupera il nome dell'immagine
        var image = await _context.Images.FindAsync(idImage);

        // se esiste continua, altrimenti errore notfound
        if(image == null)
        {
            return NotFound("image search with ID not found");
        }
        string itemImageName = image.FileName;

        var filePath = Path.Combine(_itemsFolder, itemImageName);

        // 2. Controlla se il file esiste davvero
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound($"Not Found!");
        }

        // 3. Leggi il file e sputa fuori i byte
        var imageBytes = System.IO.File.ReadAllBytes(filePath);
        return File(imageBytes, "image/jpeg"); // Il browser/client vedrà un file immagine
    }

    [Authorize]
    [HttpGet("ItemsCount/{qty}")]
    public async Task<IActionResult> GetImagesByItemsCount(int qty)
    {
        var images = await _context.Images
            .Select(img => new { 
                img.IdImage, 
                img.FileName, 
                img.ItemsCount,
                img.LastModified})
            .Where(img => img.ItemsCount <= qty)
            .ToListAsync();
        return Ok(images);
    }

    [Authorize] 
    [HttpPost("CreateImage")]
    public async Task<IActionResult> CreateIImage(string? itemName, IFormFile file)
    {

        if (file == null || file.Length == 0){
            return BadRequest("Nessun file selezionato.");
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

        return Ok(new { message = $"Immagine caricata con successo in: {_itemsFolder}/{fileName}" });
    }

    // Modifica un immagine
    [Authorize] 
    [HttpPut("UpdateImage/{id}")]
    public async Task<IActionResult> UpdateImage(int id, string? itemName, IFormFile file)
    {

        // Controlli generici:
        // Vede se l'utente ha inserito un immagine
        if (file == null || file.Length == 0){
            return BadRequest("Nessun file selezionato.");
        }

        // mi riassicuro che la cartella esista
        if (!Directory.Exists(_itemsFolder))
        {
            Directory.CreateDirectory(_itemsFolder);
        }

        // vede se il record esiste
        var imageRecord = await _context.Images.FindAsync(id);
        if(imageRecord == null)
        {
            return BadRequest("you cant update a record that does not exist!");
        }

        string itemImageName = imageRecord.FileName;
        var filePath = Path.Combine(_itemsFolder, itemImageName);

        // cerca la vecchia immagine, controlla se il file esiste davvero, e nel caso lo elimina
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
            Console.WriteLine($"eliminato: {filePath}");
        }

        // carica la nuova immagine e aggiorna il record;
        string newFileName = await UploadImage(_itemsFolder, file, itemName ?? "unknown");
        
        // salva il record con il nuovo nome dell'immagine
        imageRecord.FileName = newFileName;
        // _context.Images.Update(imageRecord); 
        // ridondante perche ef lo traccia visto che è async, ed è tracciato dal "ChangeTracker" di ef

        // Cercare ogni item con idImage = a questo e aggiornare il nome immagine
        var itemsWithThisImage = await _context.Items.Where(item => item.IdImage == id).ToListAsync();
        foreach(var item in itemsWithThisImage)        {
            // item.IdImage = id; // in realtà non cambia nulla
            item.ImageName = newFileName; // aggiorna il nome dell'immagine negli item che usano questa immagine
            // _context.Items.Update(item); // sempre ridondadte perche la ricerca è asyncrona 
        }
        await _context.SaveChangesAsync();

        return Ok(new { message = "Immagine caricata con successo"});
    }

    [Authorize]
    [HttpPut("RenameImage/{id}")]
    public async Task<IActionResult> RenameImage(int id, string? itemName)
    {
        // Controlli generici:
        // Legge il percorso di base dalla configurazione (User Secrets o Environment)
        var imageRecord = await _context.Images.FindAsync(id);
        if(imageRecord == null)        {
            return BadRequest("you cant update a record that does not exist!");
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
        } else {
            Console.WriteLine($"file da rinominare non trovato: {oldFilePath}");
            return NotFound($"file da rinominare non trovato: {oldFilePath}");
        }
        // OPZIONALE: cercare ogni item con idImage = a questo e aggiornare il nome immagine
        var itemsWithThisImage = await _context.Items.Where(item => item.IdImage == id).ToListAsync();
        foreach(var item in itemsWithThisImage)        {
            // item.IdImage = id; // in realtà non cambia nulla
            item.ImageName = newFileName; // aggiorna il nome dell'immagine negli item che usano questa immagine
            // _context.Items.Update(item); // ridondanza perche asyncrona
        }
        await _context.SaveChangesAsync();
        return Ok(new { message = "Immagine rinominata con successo" });
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("DeleteImage/{id}")]
    public async Task<IActionResult> DeleteImage(int id)
    {
        // Controlla se il record esiste
        var imageRecord = await _context.Images.FindAsync(id);
        if(imageRecord == null)        {
            return BadRequest("you cant delete a record that does not exist!");
        }    

        string itemImageName = imageRecord.FileName;
        var filePath = Path.Combine(_itemsFolder, itemImageName);
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
            Console.WriteLine($"eliminato: {filePath}");
        } else {
            Console.WriteLine($"file da eliminare non trovato: {filePath}");
        }
        
        // elimina ogni riferimento a questa immagine negli item (id_image = null)
        var itemsWithThisImage = await _context.Items.Where(item => item.IdImage == id).ToListAsync();
        foreach(var item in itemsWithThisImage)        {
            item.IdImage = null;
            item.ImageName = null;
            //_context.Items.Update(item);
        }
        // elimina il record
        _context.Images.Remove(imageRecord);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Immagine eliminata con successo" });
    }

    private async Task<string> UploadImage(string baseFolder, IFormFile file, string itemName = "unknown")
    {
        var fileName = GenerateUniqueFilename(itemName); // Genera un nome unico basato sull'itemName
        var newFilePath = Path.Combine(baseFolder, fileName);

        //Salviamo il file fisicamente (sovrascrive se esiste già)
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