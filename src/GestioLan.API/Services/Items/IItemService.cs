using GestioLan.API.Models;

namespace GestioLan.API.Services.Items;

public interface IItemService
{
    // Ottiene tutti gli oggetti del DB con filtri opzionali
    Task<IEnumerable<Item>> GetItemsAsync(
        bool? has_category,
        int? id_category,
        string? name,
        bool? has_image,
        int? quantity,
        string? type_quantity
    );

    // Ottiene un singolo oggetto del DB tramite il suo ID
    Task<Item> GetItemByIdAsync(int id);

    // Crea un nuovo oggetto nel DB
    Task<Item> CreateItemAsync(Item item);

    // Elimina un oggetto dal DB tramite il suo ID
    Task DeleteItemAsync(int id);

    // Aggiorna un oggetto esistente nel DB
    Task UpdateItemAsync(int id, Item updatedItem);
}