using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using csharpAPI.Models;

namespace csharpAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly GestioLanContext _context;

    public CategoriesController(GestioLanContext context)
    {
        _context = context;
    }

    // GET category di debug
    //[Authorize] // Protegge questo endpoint, richiede un token JWT valido per accedervi
    [HttpGet("AllCategories")]
    public async Task<ActionResult<IEnumerable<Category>>> GetAllCategories()
    {
        return await _context.Categories.OrderBy(c => c.IdCategory).ToListAsync();
    }

    // #TODO: Controllare se il nome della categoria è già presente, se si restituire un errore (visto che è PK)
    // inserisci una categoria
    [HttpPost("AddCategory")]
    public async Task<ActionResult<Category>> AddCategory([FromBody] string nome)
    {
        // 1. Conta quanti record ci sono
        var count = await _context.Categories.CountAsync();
        if (count >= 31) return BadRequest("Limite massimo di 32 categorie raggiunto.");

        // Controlla se esiste già una categoria con lo stesso nome
        bool giaEsistente = await _context.Categories.AnyAsync(c => c.NameCategory == nome);
        if (giaEsistente) return BadRequest("Categoria già esistente.");

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

            return Ok(category);
        }
        catch (DbUpdateException)
        {
            return BadRequest("Errore di validazione del Database (Bitmask non valida).");
        }
    }

    // Aggiorna categoria
    [HttpPut("UpdateCategory/{id}")]
    public async Task<ActionResult<Category>> UpdateCategory(int id, Category category)
    {
        // coontrolla se sono uguali
        if (id != category.IdCategory)
        {
            return BadRequest("L'ID nella URL non corrisponde all'ID nel corpo della richiesta.");
        }

        // Controlla se il nome della categoria è vuoto
        if (string.IsNullOrEmpty(category.NameCategory))
        {
            return BadRequest("Il nome della categoria non può essere vuoto.");
        }

        // Controlla se esiste già un'altra categoria con lo stesso nome
        if(await _context.Categories.AnyAsync(c => c.NameCategory == category.NameCategory && c.IdCategory != id))
        {
            return BadRequest("Esiste già una categoria con questo nome.");
        }

        // Controlla se stai modificando la stessa risorsa con gli stessi dati
        if(await _context.Categories.AnyAsync(c => c.NameCategory == category.NameCategory && c.IdCategory == id))
        {
            return BadRequest("La categoria è già quella che stai cercando di modificare.");
        }

        // Controlla se la categoria esiste (se esiste, l'id dovrebbe essere valido, altrimenti restituirebbe NotFound)
        var catInDb = await _context.Categories.FindAsync(id);
        if (catInDb == null) { return NotFound(); }

        string oldName = catInDb.NameCategory; // Salva il vecchio nome per il messaggio di risposta
        catInDb.NameCategory = category.NameCategory;
        try 
        {
            await _context.SaveChangesAsync();

            return Ok($"Categoria `{id}` modificata da '{oldName}' a '{category.NameCategory}'");
        }
        catch (DbUpdateException)
        {
            return BadRequest("Errore di validazione del Database (Bitmask non valida).");
        }
    }

    // rimuovi categoria
    [HttpDelete("DeleteCategory/{id}")]
    public async Task<ActionResult<Category>> DeleteCategory(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return category;
    }
}