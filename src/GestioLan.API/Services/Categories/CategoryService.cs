using Microsoft.EntityFrameworkCore;
using GestioLan.API.Models;

namespace GestioLan.API.Services.Categories;

public class CategoryService : ICategoryService
{
    private readonly GestioLanContext _context;

    public CategoryService(GestioLanContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
    {
        return await _context.Categories.OrderBy(c => c.IdCategory).ToListAsync();
    }

    public async Task<Category> AddCategoryAsync(string nome)
    {
        // 1. Conta quanti record ci sono
        var count = await _context.Categories.CountAsync();
        if (count >= 31) 
        {
            throw new InvalidOperationException("Limite massimo di 32 categorie raggiunto.");
        }

        // Controlla se esiste già una categoria con lo stesso nome
        bool giaEsistente = await _context.Categories.AnyAsync(c => c.NameCategory == nome);
        if (giaEsistente) 
        {
            throw new ArgumentException("Categoria già esistente.");
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
                break;
            }
        }
        try 
        {
            var category = new Category { IdCategory = maxId, NameCategory = nome };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return category;
        }

        catch (DbUpdateException)
        {
            throw new InvalidOperationException("Nessun bit valido disponibile per la bitmask.");
        }
    }

    public async Task<string> UpdateCategoryAsync(int id, Category category)
    {
        // coontrolla se sono uguali
        if (id != category.IdCategory)
        {
            throw new ArgumentException("L'ID nella URL non corrisponde all'ID nel corpo della richiesta.");
        }

        // Controlla se il nome della categoria è vuoto
        if (string.IsNullOrEmpty(category.NameCategory))
        {
            throw new ArgumentException("Il nome della categoria non può essere vuoto.");
        }

        // Controlla se esiste già un'altra categoria con lo stesso nome
        if (await _context.Categories.AnyAsync(c => c.NameCategory == category.NameCategory && c.IdCategory != id))
        {
            throw new ArgumentException("Esiste già una categoria con questo nome.");
        }

        // Controlla se stai modificando la stessa risorsa con gli stessi dati
        if (await _context.Categories.AnyAsync(c => c.NameCategory == category.NameCategory && c.IdCategory == id))
        {
            throw new ArgumentException("La categoria è già quella che stai cercando di modificare.");
        }

        // Controlla se la categoria esiste (se esiste, l'id dovrebbe essere valido, altrimenti restituirebbe NotFound)
        var catInDb = await _context.Categories.FindAsync(id);
        if (catInDb == null) 
        {
            throw new KeyNotFoundException();
        }

        string oldName = catInDb.NameCategory;// Salva il vecchio nome per il messaggio di risposta
        catInDb.NameCategory = category.NameCategory;

        try 
        {
            await _context.SaveChangesAsync();
            return $"Categoria `{id}` modificata da '{oldName}' a '{category.NameCategory}'";
        }
        catch (DbUpdateException ex)
        {
            throw new Exception("Errore di validazione del Database (Bitmask non valida).", ex);
        }
    }

        public async Task<Category> DeleteCategoryAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                throw new KeyNotFoundException();
            }

            // Recupera gli item che hanno questa categoria nella bitmask
            var prodToUpdate = await _context.Items
                .Where(p => (p.IdCategory & category.IdCategory) != 0)
                .ToListAsync();

            foreach (var prod in prodToUpdate)
            {
                // Rimuove il bit della categoria
                prod.IdCategory = prod.IdCategory & ~category.IdCategory; // Rimuove la categoria usando un AND con il complemento
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return category;
        }
}