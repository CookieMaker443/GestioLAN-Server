using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestioLan.API.Models;
using GestioLan.API.Utils.Helpers; // Per la classe StringHelper
using Microsoft.AspNetCore.Authorization; // Per l'attributo [Authorize]


namespace GestioLan.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
    private readonly GestioLanContext _context;
    private readonly IConfiguration _config;

    public ItemsController(GestioLanContext context, IConfiguration configuration)
    {
        _context = context;
        _config = configuration;
    }

    // Ottiene tutti gli oggetti del DB
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Item>>> GetItems(
        [FromQuery] int id_category,
        [FromQuery] string? name,
        [FromQuery] int? quantity,
        [FromQuery] string? type_quantity
        )
    {
        IQueryable<Item> query = _context.Items;

        /*
        if (ids_category.Any())
        {
            query = query.Where(item => ids_category.Contains(item.IdCategory.Value));
        }*/

        if (id_category != 0)
        {
            // controllare il WHERE per la bitmask
        }

        if (!string.IsNullOrEmpty(name))
        {
            query = query.Where(item => item.ItemName.Contains(name));
        }

        if (quantity.HasValue && !string.IsNullOrEmpty(type_quantity))
        {
            query = query.Where(item => item.Quantity == quantity.Value)
                         .Where(item => item.TypeQuantity == type_quantity);
        }

        return await query.ToListAsync();
    }

    // Ottiene un singolo oggetto del DB tramite il suo ID    
    [HttpGet("{id}")]
    public async Task<ActionResult<Item>> GetItem(int id)
    {
        var item = await _context.Items.FindAsync(id);

        if (item == null)
        {
            return NotFound(); // <-- Ritorna un codice 404
        }
        return item;
    }

    // Crea un nuovo oggetto nel DB
    [HttpPost]
    public async Task<ActionResult<IEnumerable<Item>>> PostItem(
        string name, string? description, int? id_image, int[] ids_category, int quantity, string type_quantity)
    {
        if(id_image == 0){
            id_image = null;
        }

        int id_category = 0;
        foreach (int id in ids_category)
        {
            id_category |= (1 << id); // Imposta il bit corrispondente all'id della categoria
        }

        Item newItem = new Item
        {
            ItemName = name,
            Description = description,
            IdImage = id_image,
            IdCategory = id_category,
            Quantity = quantity,
            TypeQuantity = type_quantity
        };

        _context.Items.Add(newItem);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetItems), new { id = newItem.IdItem }, newItem);
    }


    [Authorize]
    [HttpDelete("{id}")]
    public async Task<ActionResult<IEnumerable<Item>>> DeleteItem(int id)
    {
        var item = await _context.Items.FindAsync(id);
        if (item == null)
        {
            return NotFound();
        }

        _context.Items.Remove(item);
        await _context.SaveChangesAsync();

        return NoContent();
    }


    [HttpPut("{id}")]
    public async Task<IActionResult> PutItem(
        int id, string name, string description, int? id_image,
        int id_category, int quantity, string type_quantity, Item updatedItem)
    {
        if (id != updatedItem.IdItem)
        {
            return BadRequest("Id mismatch");
        }

        if(id_image == 0){
            id_image = null;
        }

        updatedItem.ItemName = name;
        updatedItem.Description = description;
        updatedItem.IdImage = id_image;
        updatedItem.IdCategory = id_category;
        updatedItem.Quantity = quantity;
        updatedItem.TypeQuantity = type_quantity;

        _context.Entry(updatedItem).State = EntityState.Modified;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("test/{id}")]
    public async Task<IActionResult> PutItem(
        int id, Item updatedItem)
    {
        
        if (id != updatedItem.IdItem)
        {
            return BadRequest("Id mismatch");
        }

        var item = await _context.Items.FindAsync(id);
        if (item == null)        {
            return NotFound();
        }

        item.ItemName = updatedItem.ItemName;
        item.Description = updatedItem.Description;
        item.IdImage = updatedItem.IdImage;
        item.IdCategory = updatedItem.IdCategory;
        item.Quantity = updatedItem.Quantity;
        item.TypeQuantity = updatedItem.TypeQuantity;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}