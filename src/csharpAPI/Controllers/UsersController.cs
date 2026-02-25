using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using csharpAPI.Models;
using csharpAPI.Utils.Hash;
using csharpAPI.Utils.JWT; // Per la classe JWT
using Microsoft.AspNetCore.Authorization; // Per l'attributo [Authorize]

namespace csharpAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly GestioLanContext _context;
    private readonly JWT _jwt;

    public UsersController(GestioLanContext context, JWT jwt)
    {
        _context = context;
        _jwt = jwt;
    }

    // GET users di debug
    //[Authorize] // Protegge questo endpoint, richiede un token JWT valido per accedervi
    [HttpGet("AllUsers")]
    public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
    {
        return await _context.Users.ToListAsync();
    }

    // Ottiene la lista degli utenti del DB con filtro per username e password con GET
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers(
        [FromQuery] string? username,
        [FromQuery] string? password
    )
    {
        IQueryable<User> query = _context.Users;

        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
        {
            query = query.Where(user => user.Username == username && user.Password == password);
            // return corretto, utente loggato
            return await query.ToListAsync();
        }

        // utente inesistente, login fallito
        return BadRequest("Invalid username or password");
        //return await _context.Users.ToListAsync();
    }


    // Login endpoint 
    // L'utente con un POST invia le credenziali per il login in un JSON, e in 
    // caso di successo riceve i dati dell'utente (senza password) e un token JWT
    [HttpPost("Login")]
    public async Task<ActionResult<IEnumerable<User>>> LoginUser(
        User loginUserdata
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
        string token = _jwt.GenerateToken(user.Username);
        
        return Ok(new { 
            User = user, 
            Token = token 
            }
        );
    }

    // Crea un nuovo utente
    // #TODO: se la tabella user è vuota, fa fare una registrazione, altrimento un admin puo aggiungere utenti (con JWT)
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
            CreateTime = DateTime.Now
        };
        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();
        return Ok();
    }

    // Elimina un utente dato l'username
    // #TODO: sono un user "admin" puo eliminare un altro user
    //[Authorize]
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


    // #TODO: quando si aggiunge il JWT, proteggere questo endpoint per permettere solo all'utente di aggiornare i propri dati
    // #TODO: quando si aggiunge il flag "admin" agli utenti, permettere agli admin di aggiornare i dati di qualsiasi utente, sempre
    // #TODO: Quandi si aggiungono leimmagini, aggiornare qui
    // verificando che il token JWT corrisponda all'username da aggiornare
    // Aggiorna i dati di un utente dato l'username
    // [Authorize]
    [HttpPut("{username}")]
    public async Task<IActionResult> PutUser(
        string username, [FromBody] User newUser)
    {
        // Controlla se l'utente esiste, e lo seleziona
        var user = await _context.Users.FindAsync(username);

        if (user == null)
        {
            return NotFound();
        }

        if(user.Username == newUser.Username || string.IsNullOrEmpty(newUser.Username))
        {
            return Ok("Username is equal, no update needed.");
        }
        else
        {
            user.Username = newUser.Username;
        }

        if(user.Email == newUser.Email || string.IsNullOrEmpty(newUser.Email))
        {
            return Ok("Email is equal, no update needed.");
        }
        else
        {
            user.Email = newUser.Email;
        }

        if(Hash.HashPassword(newUser.Password) == user.Password || string.IsNullOrEmpty(newUser.Password))
        {
            return Ok("Password is equal, no update needed.");
        }
        else
        {
            user.Password = Hash.HashPassword(newUser.Password);
        }

        _context.Entry(user).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
