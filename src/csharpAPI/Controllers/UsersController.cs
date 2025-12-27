using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using csharpAPI.Models;
using csharpAPI.Utils.Hash;

namespace csharpAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly GestioLanContext _context;

    public UsersController(GestioLanContext context)
    {
        _context = context;
    }

    // GET users di debug
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
    ){
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
    ){

        // Primo controllo sui dati ricevuti
        if (loginUserdata == null || string.IsNullOrEmpty(loginUserdata.Username) || string.IsNullOrEmpty(loginUserdata.Password))
        {
            return BadRequest("Username and password are required.");
        }

        // Cerca l'utente nel database
        var user = await _context.Users
            .Where(u => u.Username == loginUserdata.Username && u.Password == loginUserdata.Password)
            .FirstOrDefaultAsync();

        // se l'utente non esiste, ritorna errore
        if (user == null)
        {
            return Unauthorized("Invalid username or password."); 
        }

        // #TODO: Genera e ritorna un token JWT qui
        // Ritorna i dati dell'utente senza la password
        user.Password = ""; // Rimuove la password prima di ritornare l'oggetto
        return Ok(new User { 
            Username = user.Username, 
            Email = user.Email, 
            CreateTime = user.CreateTime 
        });
    }

    // Crea un nuovo utente
    [HttpPost("Register")]
    public async Task<ActionResult<IEnumerable<User>>> PostUser(
        string username, string password, string email
    ){
        // Controlla se l'username esiste già
        /*
        var userList = await _context.Users
            .Where(u => u.Username == username)
            .ToListAsync();

        if(userList.Count > 0){
            return BadRequest("Username already exists");
        }*/

        // Controllo più efficiente se l'username esiste già
        bool giaEsistente = await _context.Users
            .AnyAsync(u => u.Username == username);
        
        if (giaEsistente)
        {
            return BadRequest("Username already exists");;
        }

        // #TODO: genera l'hash della password prima di salvarla nel DB
        // aggiunge il nuovo utente
        User newUser = new User
        {
            Username = username,
            Password = Hash.HashPassword(password),
            //Password = password,
            Email = email,
            CreateTime = DateTime.Now
        };
        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        return await _context.Users.ToListAsync();
    }

    // Elimina un utente dato l'username
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
    // verificando che il token JWT corrisponda all'username da aggiornare
    // Aggiorna i dati di un utente dato l'username
    [HttpPut ("{username}")]
    public async Task<IActionResult> PutUser(string username, string password, string email)
    {
        var user = await _context.Users.FindAsync(username);
        if (user == null)
        {
            return NotFound();
        }


        user.Password = password;
        user.Email = email;

        _context.Entry(user).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
