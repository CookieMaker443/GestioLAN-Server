using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestioLan.API.Models;
using GestioLan.API.Utils.Hash;
using GestioLan.API.Utils.JWT; // Per la classe JWT
using Microsoft.AspNetCore.Authorization; // Per l'attributo [Authorize]

namespace GestioLan.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly GestioLanContext _context;
    private readonly JWT _jwt;
    private readonly IConfiguration _config;

    public UsersController(GestioLanContext context, JWT jwt, IConfiguration configuration)
    {
        _context = context;
        _jwt = jwt;
        _config = configuration;    // serve per accedere pooi agli user secret e alle variabili d ambiente, se necessario
    }

    // Login endpoint 
    [HttpPost("Login")]
    public async Task<ActionResult<IEnumerable<User>>> LoginUser(
        [FromBody] User loginUserdata
    )
    {
        // Primo controllo sui dati ricevuti
        if (loginUserdata == null || string.IsNullOrEmpty(loginUserdata.Username) || string.IsNullOrEmpty(loginUserdata.Password))
        {
            return BadRequest("Username and password are required.");
        }

        // Cerca l'utente nel database
        var user = await _context.Users
            .Where(u => u.Username == loginUserdata.Username)
            .FirstOrDefaultAsync();

        // se l'utente non esiste, o se la password non corrisponde, ritorna errore
        if (user == null || !Hash.VerifyPassword(loginUserdata.Password, user.Password))
        {
            return Unauthorized("Invalid username or password.");
        }

        // Ritorna i dati dell'utente senza la password
        user.Password = ""; // Rimuove la password prima di ritornare l'oggetto
        string token = _jwt.GenerateToken(user);
        
        return Ok(new { 
            User = user, 
            Token = token 
            }
        );
    }

    // Crea un nuovo utente
    [HttpPost("Register")]
    public async Task<ActionResult<IEnumerable<User>>> PostUser(
        [FromBody] User user
    )
    {
        // Controlla se l'username esiste già
        bool giaEsistente = await _context.Users
            .AnyAsync(u => u.Username == user.Username);

        if (giaEsistente)
        {
            return BadRequest("Username already exists"); ;
        }

        // aggiunge il nuovo utente
        string email = string.IsNullOrEmpty(user.Email) ? null : user.Email; // Se l'email è vuota, la setta a null
        User newUser = new User
        {
            Username = user.Username,
            Password = Hash.HashPassword(user.Password),
            Email = email,
            IsAdmin = user.IsAdmin ?? false, // Se IsAdmin è null, lo setta a false
            CreateTime = DateTime.Now
        };

        if (await _context.Users.CountAsync() == 0)
        {
            newUser.IsAdmin = true; // Se è il primo utente, lo setta come admin
        }

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();
        return Ok();
    }

    [Authorize(Policy = "AdminOnly")] // Solo gli admin possono accedere a questo endpoint
    [HttpDelete("DeleteUser")]
    public async Task<IActionResult> DeleteUser(string username)
    {
        var user = await _context.Users.FindAsync(username);
        if (user == null)
        {
            return NotFound();
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // #TODO: Quando si aggiorna il proprio username, bisogna vedere ed eventualmente rinominare anche l immagine profilo
    [Authorize]
    [HttpPut("{targetUsername}")]
    public async Task<IActionResult> PutUser(
        string targetUsername, [FromBody] User newUser)
    {
        var currentUsername = User.Identity?.Name;
        var currentUserIsAdmin = User.FindFirst("isAdmin")?.Value == "true";
        string message = "";
        message += $"Current user: {currentUsername}, Target user: {targetUsername}, IsAdmin: {currentUserIsAdmin}\n";

        // Autorizzazione: puoi procedere solo se sei l'interessato O sei un Admin
        if (currentUsername != targetUsername && !currentUserIsAdmin)
        {
            return Forbid("You are not authorized to update this user's data.");
        }

        var user = await _context.Users.FindAsync(targetUsername);
        if (user == null) return NotFound("Utente non trovato.");

        if(user.Email == newUser.Email)
            {
                message += "Email is equal, no update needed.\n";
            }
            else
            {
                if (string.IsNullOrWhiteSpace(newUser.Email))
                {
                    user.Email = null; // Se l'email è vuota, la setta a null
                    message += "Email set to null.\n";
                }
                else{
                    user.Email = newUser.Email;
                    message += "Email updated.\n";
                }
            }

        if(Hash.HashPassword(newUser.Password) == user.Password || string.IsNullOrEmpty(newUser.Password))
        {
            message += "Password is equal, no update needed.\n";
        }
        else
        {
            user.Password = Hash.HashPassword(newUser.Password);
            message += "Password updated.\n";
        }

        if (currentUserIsAdmin)
        {
            if (user.IsAdmin != newUser.IsAdmin)
            {
                user.IsAdmin = newUser.IsAdmin;
                message += "IsAdmin updated.\n";
            }
            else
            {
                message += "IsAdmin is equal, no update needed.\n";
            }
        }
        else
        {
            message += "You are not an Admin, you cannot change the IsAdmin flag.\n";
        }

        await _context.SaveChangesAsync();

        return Ok(message);
    }

    [Authorize]
    [HttpGet("image/{username}")]
    public IActionResult GetProfileImage(string username)
    {
        //var currentUsername = User.Identity?.Name;
        //var currentUserIsAdmin = User.FindFirst("isAdmin")?.Value == "true";
        var currentUsername = username; // TEST
        var currentUserIsAdmin = true; // TEST

        // Autorizzazione: puoi procedere solo se sei l'interessato O sei un Admin
        if (currentUsername != username && !currentUserIsAdmin)
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
        var baseFolder = _config["UPLOAD_PATH_USERS"] ?? "/app/data/uploads/users";
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

    [Authorize] 
    [HttpPost("image/{username}")]
    public async Task<IActionResult> UploadProfileImage(string username, IFormFile file)
    {
        //var currentUsername = User.Identity?.Name;
        //var currentUserIsAdmin = User.FindFirst("isAdmin")?.Value == "true";
        var currentUsername = username; // TEST
        var currentUserIsAdmin = true; // TEST

        // Autorizzazione: puoi procedere solo se sei l'interessato O sei un Admin
        if (currentUsername != username && !currentUserIsAdmin)
        {
            return Forbid("You are not authorized to update this user's data.");
        }

        if (file == null || file.Length == 0){
            return BadRequest("Nessun file selezionato.");
        }
        // Legge il percorso di base dalla configurazione (User Secrets o Environment)
        var baseFolder = _config["UPLOAD_PATH_USERS"] ?? "/app/data/uploads/users";

        // Assicuriamo che la cartella esista
        if (!Directory.Exists(baseFolder))
        {
            Directory.CreateDirectory(baseFolder);
        }

        // Creazione del nome del file e il percorso completo
        var fileName = $"{username}.jpg"; // Forziamo .jpg come deciso
        var filePath = Path.Combine(baseFolder, fileName);

        // 4. Salviamo il file fisicamente (sovrascrive se esiste già)
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return Ok(new { message = "Immagine caricata con successo", url = $"/api/Users/image/{username}" });
    }
}
