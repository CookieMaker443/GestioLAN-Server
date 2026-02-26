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

    /*
    // Ottiene la lista degli utenti del DB con filtro per username e password con GET
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers(
        [FromBody] string? username, [FromBody] string? password
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
    }*/


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
            IsAdmin = user.IsAdmin,
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

    //[Authorize(Policy = "AdminOnly")] // Solo gli admin possono accedere a questo endpoint
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
    // verificando che il token JWT corrisponda all'username da aggiornarez<
    // Aggiorna i dati di un utente dato l'username
    // [Authorize]
    [HttpPut("{sourceUsername}")]
    public async Task<IActionResult> PutUserTest(
        string sourceUsername, string? targetUsername, [FromBody] User newUser)
    {
        // Controlla se l'utente esiste, e lo seleziona
        var user = await _context.Users.FindAsync(sourceUsername);
        string message = "";

        if (user == null)
        {
            return NotFound();
        }

        // Se sono uguali, l'user modifica se stesso
        if(sourceUsername == targetUsername || string.IsNullOrEmpty(targetUsername))
        {
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

            if(user.IsAdmin == true)
            {
                if(user.IsAdmin == newUser.IsAdmin)
                {
                    message += "IsAdmin is equal, no update needed.\n";
                }
                else
                {
                    user.IsAdmin = newUser.IsAdmin;
                    message += "IsAdmin updated.\n";
                }
            }
            else
            {
                message += "IsAdmin is false, no update allowed.\n";
            }

            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(message);
        }
        else
        {
            if(sourceUsername != targetUsername && !string.IsNullOrEmpty(targetUsername) && user.IsAdmin == true)
            {
                // trova l'utente da modificare, e lo seleziona
                var targerUser = await _context.Users.FindAsync(targetUsername);
                if (targerUser == null)
                {
                    return NotFound("Target user not found.");
                }
                
                if(targerUser.Email == newUser.Email)
                {
                    message += "Target user's email is equal, no update needed.\n";
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(newUser.Email))
                    {
                        targerUser.Email = null; // Se l'email è vuota, la setta a null
                        message += "Target user's email set to null.\n";
                    }
                    else{
                        targerUser.Email = newUser.Email;
                        message += "Target user's email updated.\n";
                    }
                }

                if(Hash.HashPassword(newUser.Password) == targerUser.Password || string.IsNullOrEmpty(newUser.Password))
                {
                    message += "Target user's password is equal, no update needed.\n";
                }
                else
                {
                    targerUser.Password = Hash.HashPassword(newUser.Password);
                    message += "Target user's password updated.\n";
                }

                if(targerUser.IsAdmin == newUser.IsAdmin)
                {
                    message += "Target user's IsAdmin is equal, no update needed.\n";
                }
                else
                {
                    targerUser.IsAdmin = newUser.IsAdmin;
                    message += "Target user's IsAdmin updated.\n";
                }

                _context.Entry(targerUser).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(message);
            }
            else
            {
                message += "Target username cannot be updated, you must be an Admin.\n";
                _context.Entry(user).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return Ok(message);
            }
        }
    }

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
}
