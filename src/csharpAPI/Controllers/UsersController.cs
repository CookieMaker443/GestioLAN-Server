using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using csharpAPI.Models;

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

    // Ottiene la lista degli utenti del DB
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

}
