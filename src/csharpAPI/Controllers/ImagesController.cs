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
    [NonAction]
    public IActionResult GetItemImage(string itemImageName)
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

    // [Authorize] 
    [NonAction]
    public async Task<IActionResult> UploadItemImage(int id, string itemName, IFormFile file)
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
        string itemNameSanitized = itemName.Replace(" ", "_"); // Sostituisce gli spazi con underscore
        string randStr = StringHelper.GenerateRandomString(8); // Genera una stringa casuale per evitare conflitti di nome
        var fileName = $"{randStr}_{itemNameSanitized}.jpg"; // Forziamo .jpg come deciso
        var filePath = Path.Combine(baseFolder, fileName);

        // 4. Salviamo il file fisicamente (sovrascrive se esiste già)
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return Ok(new { message = "Immagine caricata con successo", url = $"/api/Items/image/{fileName}" });
    }
}