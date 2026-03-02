using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using csharpAPI.Models;
using Microsoft.AspNetCore.Authorization; // Per l'attributo [Authorize]


namespace csharpAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
    private readonly GestioLanContext _context;
    private readonly IConfiguration _config;

    public ItemsController(GestioLanContext context, IConfiguration configuration)
    {
        _context = context;
        _config = configuration;
    }

    // Ottiene tutti gli oggetti del DB
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Item>>> GetItems(
        [FromQuery] int[] ids_category,
        [FromQuery] string? name,
        [FromQuery] int? quantity,
        [FromQuery] string? type_quantity
        )
    {
        IQueryable<Item> query = _context.Items;

        if (ids_category.Any())
        {
            query = query.Where(item => ids_category.Contains(item.IdCategory.Value));
        }


        if (!string.IsNullOrEmpty(name))
        {
            query = query.Where(item => item.ItemName.Contains(name));
        }

        if (quantity.HasValue && !string.IsNullOrEmpty(type_quantity))
        {
            query = query.Where(item => item.Quantity == quantity.Value)
                         .Where(item => item.TypeQuantity == type_quantity);
        }

        return await query.ToListAsync();
    }

    // Ottiene un singolo oggetto del DB tramite il suo ID    
    [HttpGet("{id}")]
    public async Task<ActionResult<Item>> GetItem(int id)
    {
        var item = await _context.Items.FindAsync(id);

        if (item == null)
        {
            return NotFound(); // <-- Ritorna un codice 404
        }
        return item;
    }

    // Crea un nuovo oggetto nel DB
    [HttpPost]
    public async Task<ActionResult<IEnumerable<Item>>> PostItem(
        string name, string? description, string? image, int id_category, int quantity, string type_quantity)
    {

        Item newItem = new Item
        {
            ItemName = name,
            Description = description,
            Image = image,
            IdCategory = id_category,
            Quantity = quantity,
            TypeQuantity = type_quantity
        };

        _context.Items.Add(newItem);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetItems), new { id = newItem.IdItem }, newItem);
    }


    [Authorize]
    [HttpDelete("{id}")]
    public async Task<ActionResult<IEnumerable<Item>>> DeleteItem(int id)
    {
        var item = await _context.Items.FindAsync(id);
        if (item == null)
        {
            return NotFound();
        }

        _context.Items.Remove(item);
        await _context.SaveChangesAsync();

        return NoContent();
    }


    [HttpPut("{id}")]
    public async Task<IActionResult> PutItem(
        int id, string name, string description, string image,
        int id_category, int quantity, string type_quantity, Item updatedItem)
    {
        if (id != updatedItem.IdItem)
        {
            return BadRequest("Id mismatch");
        }

        updatedItem.ItemName = name;
        updatedItem.Description = description;
        updatedItem.Image = image;
        updatedItem.IdCategory = id_category;
        updatedItem.Quantity = quantity;
        updatedItem.TypeQuantity = type_quantity;

        _context.Entry(updatedItem).State = EntityState.Modified;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // NOTA: una chiamata per immagine di item, il client sarà responsabile del caching
    // [Authorize]
    [HttpGet("image/{username}")]
    public IActionResult GetItemImage(string username)
    {
        //var currentUsername = User.Identity?.Name;
        var currentUsername = username; // TEST

        // Autorizzazione: puoi procedere solo se sei l'interessato O sei un Admin
        if (currentUsername != username)
        {
            return Forbid("You are not authorized to update this user's data.");
        }
        // qui si costruisci il percorso interno al container

        // percorso per il server: /app/data/uploads/users/{username}.jpg
        //var filePath = Path.Combine("/", "app", "data", "uploads", "users", $"{username}.jpg");
        
        // Quest crea il percorso per lo sviluppo locale: home/cookie/Docker/services/MariaDb11.6/volumes/images/users
        //var filePath = Path.Combine("/", "home", "cookie", "Docker", "services", "MariaDb11.6", "volumes", "images", "users", $"{username}.jpg");

        // Questo cerca nelle variabili d'ambiente, se non trova niente usa il percorso di default (quello usato nel docker-compose)
        //var baseFolder = Environment.GetEnvironmentVariable("UPLOAD_PATH_USERS") ?? "/app/data/uploads/users";
        
        // Questo cercherà PRIMA nei User Secrets, poi nelle variabili d'ambiente, poi nel JSON
        var baseFolder = _config["UPLOAD_PATH_ITEMS"] ?? "/app/data/uploads/items";
        var filePath = Path.Combine(baseFolder, $"{username}.jpg");

        // 2. Controlla se il file esiste davvero
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound($"Cercato in: {filePath}");
        }

        // 3. Leggi il file e sputa fuori i byte
        var imageBytes = System.IO.File.ReadAllBytes(filePath);
        return File(imageBytes, "image/jpeg"); // Il browser/client vedrà un file immagine
    }

    // [Authorize] 
    [HttpPost("image/{username}")]
    public async Task<IActionResult> UploadItemImage(string username, int id, IFormFile file)
    {
        //var currentUsername = User.Identity?.Name;
        var currentUsername = username; // TEST

        // Autorizzazione: puoi procedere solo se sei l'interessato O sei un Admin
        if (currentUsername != username)
        {
            return Forbid("You are not authorized to update this items's data.");
        }

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
        var fileName = $"{id}.jpg"; // Forziamo .jpg come deciso
        var filePath = Path.Combine(baseFolder, fileName);

        // 4. Salviamo il file fisicamente (sovrascrive se esiste già)
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return Ok(new { message = "Immagine caricata con successo", url = $"/api/Items/image/{username}" });
    }
}