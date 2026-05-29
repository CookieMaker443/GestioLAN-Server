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

    private readonly ILogger<UserService> _logger;
    private readonly string _usersFolder;

    public UserService(GestioLanContext context, JWT jwt, 
        IConfiguration configuration, ILogger<UserService> logger)
    {
        _context = context;
        _jwt = jwt;
        _logger = logger;
        _usersFolder = configuration["Storage:UsersPath"] ?? "/app/data/uploads/users";
    }

    // Login endpoint
    public async Task<(User User, string Token)> LoginAsync(User loginUserdata)
    {
        // Primo controllo sui dati ricevuti
        if (loginUserdata == null || string.IsNullOrEmpty(loginUserdata.Username) || string.IsNullOrEmpty(loginUserdata.Password))
        {
            _logger.LogWarning("Login attempt with null or incomplete data");
            throw new ArgumentException("Username and password are required.");
        }

        // Cerca l'utente nel database
        _logger.LogInformation("Login attempt for user: {Username}", loginUserdata.Username);
        var user = await _context.Users
            .Where(u => u.Username == loginUserdata.Username)
            .FirstOrDefaultAsync();

        // se l'utente non esiste, o se la password non corrisponde, ritorna errore
        if (user == null || !Hash.VerifyPassword(loginUserdata.Password, user.Password))
        {
            _logger.LogWarning("Invalid credentials for user: {Username}", loginUserdata.Username);
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        // Ritorna i dati dell'utente senza la password
        user.Password = ""; // Rimuove la password prima di ritornare l'oggetto
        string token = _jwt.GenerateToken(user);
        _logger.LogInformation("Login successful, token generated for: {Username}", user.Username);

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
            _logger.LogWarning("Registration rejected: username already exists: {Username}", user.Username);
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
            _logger.LogInformation("First user detected, granting admin role to: {Username}", newUser.Username);
            newUser.IsAdmin = true; // Se è il primo utente, lo setta come admin
        }

        
        _context.Users.Add(newUser);
        _logger.LogInformation("User registered: {Username}, IsAdmin: {IsAdmin}", newUser.Username, newUser.IsAdmin);
        await _context.SaveChangesAsync();
    }

    // Eliminazione di un utente (solo admin)
    public async Task DeleteUserAsync(string username)
    {
        _logger.LogInformation("Attempting to delete user: {Username}", username);

        var user = await _context.Users.FindAsync(username);
        if (user == null)
        {
            _logger.LogWarning("User not found: {Username}", username);
            throw new KeyNotFoundException($"User {username} not found.");
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User deleted successfully: {Username}", username);
    }

    // Aggiornamento dei dati di un utente
    // currentUsername e currentUserIsAdmin provengono dal token JWT nel controller
    public async Task<string> UpdateUserAsync(string targetUsername, User newUser, string currentUsername, bool currentUserIsAdmin)
    {
        _logger.LogInformation("Update requested by: {CurrentUsername} for target: {TargetUsername}, IsAdmin: {IsAdmin}",
            currentUsername, targetUsername, currentUserIsAdmin);

        string message = "";
        message += $"Current user: {currentUsername}, Target user: {targetUsername}, IsAdmin: {currentUserIsAdmin}\n";

        // Autorizzazione: puoi procedere solo se sei l'interessato O sei un Admin
        if (currentUsername != targetUsername && !currentUserIsAdmin)
        {
            _logger.LogWarning("Unauthorized update attempt: {CurrentUsername} tried to update {TargetUsername}", currentUsername, targetUsername);
            throw new UnauthorizedAccessException("You are not authorized to update this user's data.");
        }

        var user = await _context.Users.FindAsync(targetUsername);
        if (user == null)
        {
            _logger.LogWarning("Target user not found: {TargetUsername}", targetUsername);
            throw new KeyNotFoundException("User not found.");
        }

        if (user.Email == newUser.Email)
        {_logger.LogInformation("Email unchanged for user: {Username}", targetUsername);
            message += "Email is equal, no update needed.\n";
        }
        else
        {
            if (string.IsNullOrWhiteSpace(newUser.Email))
            {
                user.Email = null; // Se l'email è vuota, la setta a null
                _logger.LogInformation("Email set to null for user: {Username}", targetUsername);
                message += "Email set to null.\n";
            }
            else
            {
                user.Email = newUser.Email;
                _logger.LogInformation("Email updated for user: {Username}", targetUsername);
                message += "Email updated.\n";
            }
        }

        if (Hash.HashPassword(newUser.Password) == user.Password || string.IsNullOrEmpty(newUser.Password))
        {
            _logger.LogInformation("Password unchanged for user: {Username}", targetUsername);
            message += "Password is equal, no update needed.\n";
        }
        else
        {
            user.Password = Hash.HashPassword(newUser.Password);
            _logger.LogInformation("Password updated for user: {Username}", targetUsername);
            message += "Password updated.\n";
        }

        if (currentUserIsAdmin)
        {
            if (user.IsAdmin != newUser.IsAdmin)
            {
                user.IsAdmin = newUser.IsAdmin;
                _logger.LogInformation("IsAdmin flag updated to {IsAdmin} for user: {Username}", newUser.IsAdmin, targetUsername);
                message += "IsAdmin updated.\n";
            }
            else
            {
                _logger.LogInformation("IsAdmin flag unchanged for user: {Username}", targetUsername);
                message += "IsAdmin is equal, no update needed.\n";
            }
        }
        else
        {
            _logger.LogInformation("Non-admin user {CurrentUsername} cannot change IsAdmin flag", currentUsername);
            message += "You are not an Admin, you cannot change the IsAdmin flag.\n";
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("User {TargetUsername} updated successfully", targetUsername);

        return message;
    }

    // Recupero dell'immagine profilo come byte array
    public Task<byte[]> GetProfileImageAsync(string username)
    {
        _logger.LogInformation("Profile image requested for user: {Username}", username);
        // l'autorizzazione per solo se sei l'interessato O sei un Admin è stata messa nekl controller, quindi qui possiamo procedere direttamente con la logica di recupero dell'immagine
        // Questo cercherà PRIMA nei User Secrets, poi nelle variabili d'ambiente, poi nel JSON
        var filePath = Path.Combine(_usersFolder, $"{username}.jpg");

        // Controlla se il file esiste davvero
        if (!System.IO.File.Exists(filePath))
        {
            _logger.LogWarning("Profile image not found for user: {Username}, path: {FilePath}", username, filePath);
            throw new FileNotFoundException($"Searched in: {filePath}");
        }

        // Leggi il file e ritorna i byte
        var imageBytes = System.IO.File.ReadAllBytes(filePath);
        _logger.LogInformation("Profile image served for user: {Username}", username);
        return Task.FromResult(imageBytes);
    }

    // Upload dell'immagine profilo
    public async Task<string> UploadProfileImageAsync(string username, IFormFile file)
    {
        // L'autorizzazione per solo se sei l'interessato O sei un Admin è stata messa nekl controller, quindi qui possiamo procedere direttamente con la logica di upload dell'immagine
        _logger.LogInformation("Profile image upload requested for user: {Username}", username);

        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("Profile image upload rejected: no file provided for user: {Username}", username);
            throw new ArgumentException("No file selected.");
        }

        // Assicuriamo che la cartella esista
        if (!Directory.Exists(_usersFolder))
        {
            Directory.CreateDirectory(_usersFolder);
            _logger.LogInformation("Created missing target directory: {UsersFolder}", _usersFolder);
        }

        // Creazione del nome del file e il percorso completo
        var fileName = $"{username}.jpg"; // Forziamo .jpg come deciso
        var filePath = Path.Combine(_usersFolder, fileName);

        // Salviamo il file fisicamente (sovrascrive se esiste già)
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        _logger.LogInformation("Profile image uploaded successfully for user: {Username}, path: {FilePath}", username, filePath);
        return username;
    }
}