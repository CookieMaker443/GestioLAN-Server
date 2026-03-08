using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using csharpAPI.Models;
using csharpAPI.Utils.Helpers; // Per la classe StringHelper
using Microsoft.AspNetCore.Authorization; // Per l'attributo [Authorize]


namespace csharpAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImagesController : ControllerBase
{
    private readonly GestioLanContext _context;
    private readonly IConfiguration _config;
    
    public ImagesController(GestioLanContext context, IConfiguration configuration)
    {
        _context = context;
        _config = configuration;
    }

    // NOTA: una chiamata per immagine di item, il client sarà responsabile del caching
    // [Authorize]
    [HttpGet("item/{itemImageName}")]
    public IActionResult GetIImage(string itemImageName)
    {
        // qui si costruisci il percorso interno al container
        // Questo cercherà PRIMA nei User Secrets, poi nelle variabili d'ambiente, poi nel JSON
        var baseFolder = _config["UPLOAD_PATH_ITEMS"] ?? "/app/data/uploads/items";
        var filePath = Path.Combine(baseFolder, itemImageName);

        // 2. Controlla se il file esiste davvero
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound($"Not Found!");
        }

        // 3. Leggi il file e sputa fuori i byte
        var imageBytes = System.IO.File.ReadAllBytes(filePath);
        return File(imageBytes, "image/jpeg"); // Il browser/client vedrà un file immagine
    }

    //[Authenticate]
    [HttpGet("item/{idImage}")]
    public async Task<IActionResult> GetIImage(int idImage)
    {
        // cerca l'id nella tabella e recupera il nome dell'immagine
        var image = await _context.Images.FindAsync(idImage);

        // se esiste continua, altrimenti errore notfound
        if(image == null)
        {
            return NotFound("image search with ID not found");
        }
        string itemImageName = image.FileName;

        // qui si costruisci il percorso interno al container
        // Questo cercherà PRIMA nei User Secrets, poi nelle variabili d'ambiente, poi nel JSON
        var baseFolder = _config["UPLOAD_PATH_ITEMS"] ?? "/app/data/uploads/items";
        var filePath = Path.Combine(baseFolder, itemImageName);

        // 2. Controlla se il file esiste davvero
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound($"Not Found!");
        }

        // 3. Leggi il file e sputa fuori i byte
        var imageBytes = System.IO.File.ReadAllBytes(filePath);
        return File(imageBytes, "image/jpeg"); // Il browser/client vedrà un file immagine
    }

    // [Authorize] 
    [HttpPost("item/{id}")]
    public async Task<IActionResult> CreateIImage(int id, string? itemName, IFormFile file)
    {

        if (file == null || file.Length == 0){
            return BadRequest("Nessun file selezionato.");
        }
        // Legge il percorso di base dalla configurazione (User Secrets o Environment)
        var baseFolder = _config["UPLOAD_PATH_ITEMS"] ?? "/app/data/uploads/items";

        // Assicuriamo che la cartella esista
        if (!Directory.Exists(baseFolder))
        {
            Directory.CreateDirectory(baseFolder);
        }

        // Creazione del nome del file e il percorso completo
        if (string.isNullOrEmpty(itemName))
        {
            UploadImage(baseFolder, file);
        }
        UploadImage(itemName, baseFolder, file);

        return Ok(new { message = "Immagine caricata con successo", url = $"/api/Items/image/{fileName}" });
    }

    // Modifica un immagine
    // [Authorize] 
    [HttpPut("item/{id}")]
    public async Task<IActionResult> UpdateIImage(int id, string? itemName, IFormFile file)
    {

        // Controlli generici:
        // Vede se l'utente ha inserito un immagine
        if (file == null || file.Length == 0){
            return BadRequest("Nessun file selezionato.");
        }
        // Legge il percorso di base dalla configurazione (User Secrets o Environment)
        var baseFolder = _config["UPLOAD_PATH_ITEMS"] ?? "/app/data/uploads/items";

        // Assicuriamo che la cartella esista
        if (!Directory.Exists(baseFolder))
        {
            Directory.CreateDirectory(baseFolder);
        }

        // vede se il record esiste
        var imageRecord = await _context.Images.FindAsync(id);
        if(imageRecord == null)
        {
            return BadRequest("you cant update a record that does not exist!");
        }

        string itemImageName = imageRecord.FileName;
        var filePath = Path.Combine(baseFolder, itemImageName);

        // cerca la vecchia immagine, controlla se il file esiste davvero, e nel caso lo elimina
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
            Console.WriteLine($"eliminato: {filePath}");
        }

        // carica la nuova immagine e aggiorna il record;
        if (string.isNullOrEmpty(itemName))
        {
            UploadImage(baseFolder, file);
        }
        UploadImage(itemName, baseFolder, file);
        imageRecord.Filename = newFilePath;

        // OPZIONALE: cercare ogni item con idImage = a questo e aggiornare il nome immagine

        return Ok(new { message = "Immagine caricata con successo", url = $"/api/Items/image/{fileName}" });
    }

    private async Task<IActionResult> UploadImage(string itemName, string baseFolder, IFormFile file)
    {
        string itemNameSanitized = itemName.Replace(" ", "_"); // Sostituisce gli spazi con underscore
        string randStr = StringHelper.GenerateRandomString(8); // Genera una stringa casuale per evitare conflitti di nome
        var fileName = $"{randStr}_{itemNameSanitized}.jpg"; // Forziamo .jpg come deciso
        var newFilePath = Path.Combine(baseFolder, fileName);

        //Salviamo il file fisicamente (sovrascrive se esiste già)
        using (var stream = new FileStream(newFilePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
            Console.WriteLine($"caricato: {newFilePath}");
        }
    }

    private async Task<IActionResult> UploadImage(string baseFolder, IFormFile file)
    {

        string itemName = "unknown"; // genera un nome generico
        string randStr = StringHelper.GenerateRandomString(8); // Genera una stringa casuale per evitare conflitti di nome
        var fileName = $"{randStr}_{itemName}.jpg"; // Forziamo .jpg come deciso
        var newFilePath = Path.Combine(baseFolder, fileName);

        //Salviamo il file fisicamente (sovrascrive se esiste già)
        using (var stream = new FileStream(newFilePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
            Console.WriteLine($"caricato: {newFilePath}");
        }
    }
}