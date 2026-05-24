using GestioLan.API.Models;
using Microsoft.AspNetCore.Http;

namespace GestioLan.API.Services.Users;

public interface IUserService
{
    // Login: ritorna l'utente (senza password) e il token JWT
    Task<(User User, string Token)> LoginAsync(User loginUserdata);

    // Registrazione di un nuovo utente
    Task RegisterAsync(User user);

    // Eliminazione di un utente (solo admin)
    Task DeleteUserAsync(string username);

    // Aggiornamento dei dati di un utente
    // currentUsername e currentUserIsAdmin provengono dal token JWT nel controller
    Task<string> UpdateUserAsync(string targetUsername, User newUser, string currentUsername, bool currentUserIsAdmin);

    // Recupero dell'immagine profilo come byte array
    Task<byte[]> GetProfileImageAsync(string username);

    // Upload dell'immagine profilo
    Task<string> UploadProfileImageAsync(string username, IFormFile file);
}