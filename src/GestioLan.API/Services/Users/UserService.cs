using GestioLan.API.Models;
using GestioLan.API.Utils.Hash;
using GestioLan.API.Utils.JWT;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace GestioLan.API.Services.Users;

public class UserService : IUserService
{
    private readonly GestioLanContext _context;
    private readonly JWT _jwt;
    private readonly string _usersFolder;

    public UserService(GestioLanContext context, JWT jwt, IConfiguration configuration)
    {
        _context = context;
        _jwt = jwt;
        _usersFolder = configuration["Storage:UsersPath"] ?? "/app/data/uploads/users";
    }

    // Login endpoint
    public async Task<(User User, string Token)> LoginAsync(User loginUserdata)
    {
        // Primo controllo sui dati ricevuti
        if (loginUserdata == null || string.IsNullOrEmpty(loginUserdata.Username) || string.IsNullOrEmpty(loginUserdata.Password))
        {
            throw new ArgumentException("Username and password are required.");
        }

        // Cerca l'utente nel database
        var user = await _context.Users
            .Where(u => u.Username == loginUserdata.Username)
            .FirstOrDefaultAsync();

        // se l'utente non esiste, o se la password non corrisponde, ritorna errore
        if (user == null || !Hash.VerifyPassword(loginUserdata.Password, user.Password))
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        // Ritorna i dati dell'utente senza la password
        user.Password = ""; // Rimuove la password prima di ritornare l'oggetto
        string token = _jwt.GenerateToken(user);

        return (user, token);
    }

    // Crea un nuovo utente
    public async Task RegisterAsync(User user)
    {
        // Controlla se l'username esiste già
        bool giaEsistente = await _context.Users
            .AnyAsync(u => u.Username == user.Username);

        if (giaEsistente)
        {
            throw new InvalidOperationException("Username already exists");
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
    }

    // Eliminazione di un utente (solo admin)
    public async Task DeleteUserAsync(string username)
    {
        var user = await _context.Users.FindAsync(username);
        if (user == null)
        {
            throw new KeyNotFoundException($"User {username} not found.");
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }

    // Aggiornamento dei dati di un utente
    // currentUsername e currentUserIsAdmin provengono dal token JWT nel controller
    public async Task<string> UpdateUserAsync(string targetUsername, User newUser, string currentUsername, bool currentUserIsAdmin)
    {
        string message = "";
        message += $"Current user: {currentUsername}, Target user: {targetUsername}, IsAdmin: {currentUserIsAdmin}\n";

        // Autorizzazione: puoi procedere solo se sei l'interessato O sei un Admin
        if (currentUsername != targetUsername && !currentUserIsAdmin)
        {
            throw new UnauthorizedAccessException("You are not authorized to update this user's data.");
        }

        var user = await _context.Users.FindAsync(targetUsername);
        if (user == null)
        {
            throw new KeyNotFoundException("Utente non trovato.");
        }

        if (user.Email == newUser.Email)
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
            else
            {
                user.Email = newUser.Email;
                message += "Email updated.\n";
            }
        }

        if (Hash.HashPassword(newUser.Password) == user.Password || string.IsNullOrEmpty(newUser.Password))
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

        return message;
    }

    // Recupero dell'immagine profilo come byte array
    public Task<byte[]> GetProfileImageAsync(string username)
    {
        // l'autorizzazione per solo se sei l'interessato O sei un Admin è stata messa nekl controller, quindi qui possiamo procedere direttamente con la logica di recupero dell'immagine
        // Questo cercherà PRIMA nei User Secrets, poi nelle variabili d'ambiente, poi nel JSON
        var filePath = Path.Combine(_usersFolder, $"{username}.jpg");

        // Controlla se il file esiste davvero
        if (!System.IO.File.Exists(filePath))
        {
            throw new FileNotFoundException($"Cercato in: {filePath}");
        }

        // Leggi il file e ritorna i byte
        var imageBytes = System.IO.File.ReadAllBytes(filePath);
        return Task.FromResult(imageBytes);
    }

    // Upload dell'immagine profilo
    public async Task<string> UploadProfileImageAsync(string username, IFormFile file)
    {
        // L'autorizzazione per solo se sei l'interessato O sei un Admin è stata messa nekl controller, quindi qui possiamo procedere direttamente con la logica di upload dell'immagine

        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("Nessun file selezionato.");
        }

        // Assicuriamo che la cartella esista
        if (!Directory.Exists(_usersFolder))
        {
            Directory.CreateDirectory(_usersFolder);
        }

        // Creazione del nome del file e il percorso completo
        var fileName = $"{username}.jpg"; // Forziamo .jpg come deciso
        var filePath = Path.Combine(_usersFolder, fileName);

        // Salviamo il file fisicamente (sovrascrive se esiste già)
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/api/Users/image/{username}";
    }
}