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
        return await _context.Categories.ToListAsync();
    }

    // #TODO: Controllare se il nome della categoria è già presente, se si restituire un errore (visto che è PK)
    // #TODO: Controllare se esiste un "buco" nelle categorie (es. 1, 2, 8) e se si usare quel id_category invece di creare una nuova potenza di 2
    // inserisci una categoria
    [HttpPost("AddCategory")]
    public async Task<ActionResult<Category>> AddCategory([FromBody] string nome)
    {
        // 1. Conta quanti record ci sono
        var count = await _context.Categories.CountAsync();
        if (count >= 32) return BadRequest("Limite massimo di 32 categorie raggiunto.");

        // 2. trova  l'ultimo id_category usato piu alto
        var maxId = await _context.Categories.MaxAsync(c => (int?)c.IdCategory) ?? 0;

        // 3. Calcola il prossimo id_category come potenza di 2
        var nextId = maxId == 0 ? 1 : (uint)maxId<<1; // Sposta a sinistra di 1 bit per ottenere la prossima potenza di 2

        var category = new Category { IdCategory = (int)nextId, NameCategory = nome };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return Ok(category);
    }

    // Aggiorna categoria
    [HttpPut("UpdateCategory/{id}")]
    public async Task<ActionResult<Category>> UpdateCategory(int id, Category category)
    {
        /*
        if (id != category.Id)
        {
            return BadRequest();
        }

        _context.Entry(category).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!CategoryExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }
        */
        return NoContent();

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