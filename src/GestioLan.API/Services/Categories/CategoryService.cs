using Microsoft.EntityFrameworkCore;
using GestioLan.API.Models;

namespace GestioLan.API.Services.Categories;

public class CategoryService : ICategoryService
{
    private readonly GestioLanContext _context;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(GestioLanContext context, ILogger<CategoryService> logger)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
    {
        _logger.LogInformation("Retrieving all categories");
        var categories = await _context.Categories.OrderBy(c => c.IdCategory).ToListAsync();
        _logger.LogInformation("Returned {Count} categories", categories.Count);
        return categories;

    }

    public async Task<Category> AddCategoryAsync(string nome)
    {
        // 1. Conta quanti record ci sono
        _logger.LogInformation("Attempting to add category: {Name}", nome);

        var count = await _context.Categories.CountAsync();
        if (count >= 31) 
        {
            _logger.LogWarning("Category limit reached ({Count}/31), cannot add: {Name}", count, nome);
            throw new InvalidOperationException("Maximum limit of 32 categories reached.");
        }

        // Controlla se esiste già una categoria con lo stesso nome
        bool giaEsistente = await _context.Categories.AnyAsync(c => c.NameCategory == nome);
        if (giaEsistente) 
        {
            _logger.LogWarning("Category already exists: {Name}", nome);
            throw new ArgumentException("Category already exists.");
        }


        // 2. trova  l'ultimo id_category usato piu alto
        var maxId = await _context.Categories.MaxAsync(c => (int?)c.IdCategory) ?? 0;

        // vede se esiste un "buco" nelle categorie (es. 1, 2, 8) e se si usare quel id_category invece di creare una nuova potenza di 2
        var existingIds = await _context.Categories.Select(c => c.IdCategory).ToListAsync();
        for (int i = 0; i < 32; i++)
        {
            int potentialId = 1 << i; // Calcola la potenza di 2 (1, 2, 4, 8, 16, 32)
            if (!existingIds.Contains(potentialId))
            {
                maxId = potentialId; // Usa il "buco" trovato
                _logger.LogInformation("Assigned bitmask ID: {IdCategory}", maxId);
                break;
            }
        }
        try 
        {
            var category = new Category { IdCategory = maxId, NameCategory = nome };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Category created successfully: ID {IdCategory}, Name {Name}", category.IdCategory, category.NameCategory);
            return category;
        }

        catch (DbUpdateException ex)
        {
            _logger.LogError(ex.Message, "DB error while adding category: {Name}", nome);
            throw new InvalidOperationException("No valid bitmask bit available.");

        }
    }

    public async Task<string> UpdateCategoryAsync(int id, Category category)
    {
        _logger.LogInformation("Attempting to update category with ID: {Id}", id);

        // coontrolla se sono uguali
        if (id != category.IdCategory)
        {
            _logger.LogWarning("ID mismatch: route ID {Id} does not match body ID {BodyId}", id, category.IdCategory);
            throw new ArgumentException("The URL ID does not match the ID in the request body.");
        }

        // Controlla se il nome della categoria è vuoto
        if (string.IsNullOrEmpty(category.NameCategory))
        {
            _logger.LogWarning("Update rejected: category name is empty for ID {Id}", id);
            throw new ArgumentException("Category name cannot be empty.");

        }

        // Controlla se esiste già un'altra categoria con lo stesso nome
        if (await _context.Categories.AnyAsync(c => c.NameCategory == category.NameCategory && c.IdCategory != id))
        {
            _logger.LogWarning("Update rejected: another category with name '{Name}' already exists", category.NameCategory);
            throw new ArgumentException("A category with this name already exists.");
        }

        // Controlla se stai modificando la stessa risorsa con gli stessi dati
        if (await _context.Categories.AnyAsync(c => c.NameCategory == category.NameCategory && c.IdCategory == id))
        {
            _logger.LogWarning("Update rejected: category ID {Id} already has name '{Name}'", id, category.NameCategory);
            throw new ArgumentException("The category already has the name you are trying to set.");
        }

        // Controlla se la categoria esiste (se esiste, l'id dovrebbe essere valido, altrimenti restituirebbe NotFound)
        var catInDb = await _context.Categories.FindAsync(id);
        if (catInDb == null) 
        {
            _logger.LogWarning("Category with ID {Id} not found", id);
            throw new KeyNotFoundException();
        }

        string oldName = catInDb.NameCategory;// Salva il vecchio nome per il messaggio di risposta
        catInDb.NameCategory = category.NameCategory;

        try 
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Category ID {Id} renamed from '{OldName}' to '{NewName}'", id, oldName, category.NameCategory);
            return $"Category `{id}` renamed from '{oldName}' to '{category.NameCategory}'";
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "DB error while updating category ID {Id}", id);
            throw new Exception("Database validation error (invalid bitmask).", ex);
        }

    }

    public async Task<Category> DeleteCategoryAsync(int id)
    {
        _logger.LogInformation("Attempting to delete category with ID: {Id}", id);

        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            _logger.LogWarning("Category with ID {Id} not found", id);
            throw new KeyNotFoundException();
        }

            // Recupera gli item che hanno questa categoria nella bitmask
        var prodToUpdate = await _context.Items
            .Where(p => (p.IdCategory & category.IdCategory) != 0)
            .ToListAsync();

        _logger.LogInformation("Removing category bitmask from {Count} item(s)", prodToUpdate.Count);
        foreach (var prod in prodToUpdate)
        {
            // Rimuove il bit della categoria
            prod.IdCategory = prod.IdCategory & ~category.IdCategory; // Rimuove la categoria usando un AND con il complemento
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Category ID {Id} ('{Name}') deleted successfully", id, category.NameCategory);
        return category;
    }
}