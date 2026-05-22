using GestioLan.API.Models;

namespace GestioLan.API.Services.Images;


public interface IImageService
{
    // Restituisce la lista di tutte le immagini con le info base (IdImage, FileName, ItemsCount, LastModified)
    Task<IEnumerable<object>> GetAllImagesInfoAsync();
 
    // Restituisce i byte dell'immagine cercandola per nome file
    // Lancia FileNotFoundException se il file non esiste su disco
    Task<byte[]> GetImageByNameAsync(string itemImageName);
 
    // Restituisce i byte dell'immagine cercandola per ID nel database
    // Lancia KeyNotFoundException se il record non esiste
    // Lancia FileNotFoundException se il file non esiste su disco
    Task<byte[]> GetImageByIdAsync(int idImage);
 
    // Restituisce la lista di immagini con ItemsCount <= qty
    Task<IEnumerable<object>> GetImagesByItemsCountAsync(int qty);
 
    // Carica una nuova immagine su disco e crea il record nel database
    // Restituisce il messaggio di conferma con il percorso
    // Lancia ArgumentException se il file è null o vuoto
    Task<string> CreateImageAsync(string? itemName, IFormFile file);
 
    // Sostituisce il file immagine su disco e aggiorna il record e gli Item collegati
    // Restituisce il messaggio di conferma
    // Lancia ArgumentException se il file è null o vuoto
    // Lancia KeyNotFoundException se il record non esiste
    Task<string> UpdateImageAsync(int id, string? itemName, IFormFile file);
 
    // Rinomina il file immagine su disco e aggiorna il record e gli Item collegati
    // Restituisce il messaggio di conferma
    // Lancia KeyNotFoundException se il record non esiste
    // Lancia FileNotFoundException se il file da rinominare non esiste su disco
    Task<string> RenameImageAsync(int id, string? itemName);
 
    // Elimina il file immagine su disco, rimuove il record e azzera i riferimenti negli Item
    // Restituisce il messaggio di conferma
    // Lancia KeyNotFoundException se il record non esiste
    Task<string> DeleteImageAsync(int id);
}
 
