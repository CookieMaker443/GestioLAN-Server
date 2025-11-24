using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using csharpAPI.Models;

namespace csharpAPI.Controllers;
namespace csharpAPI.MoreCode;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly GestioLanContext _context;

    public UsersController(GestioLanContext context)
    {
        _context = context;
    }

    // Ottiene la lista degli utenti del DB
    [HttpGet("{Username},{password}")]
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
        LoginData loginData 
    ){
        if (loginData == null || string.IsNullOrEmpty(loginData.Username) || string.IsNullOrEmpty(loginData.Password))
        {
            return BadRequest("Username and password are required.");
        }

        var user = await _context.Users
            .Where(u => u.Username == loginData.Username && u.Password == loginData.Password)
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return Unauthorized("Invalid username or password."); 
        }

        user.Password = ""; // Rimuove la password prima di ritornare l'oggetto
        return Ok(user);
    }

    
    [HttpPost]
    public async Task<ActionResult<IEnumerable<User>>> PostUser(
        string username, string password, string email
    ){
        // Controlla se l'username esiste già
        var userList = await _context.Users
            .Where(u => u.Username == username)
            .ToListAsync();

        if(userList.Count > 0){
            return BadRequest("Username already exists");
        }

        // aggiunge il nuovo utente
        User newUser = new User
        {
            Username = username,
            Password = password,
            Email = email,
            CreateTime = DateTime.Now
        };
        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        return await _context.Users.ToListAsync();
    }

    [HttpDelete]
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
