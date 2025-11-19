using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using csharpAPI.Models;

namespace csharpAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
    private readonly GestioLanContext _context;

    public ItemsController(GestioLanContext context){
        _context = context;
    }

    // Ottiene tutti gli oggetti del DB
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Item>>> GetItems(
        [FromQuery] int? id_category,
        [FromQuery] string? name,
        [FromQuery] int? quantity,
        [FromQuery] string? type_quantity
        ){
        IQueryable<Item> query = _context.Items;

        if(id_category.HasValue){
            query = query.Where(item => item.IdCategory == id_category.Value);
        }

        if(!string.IsNullOrEmpty(name)){
            query = query.Where(item => item.ItemName.Contains(name));
        }

        if(quantity.HasValue && !string.IsNullOrEmpty(type_quantity)){
            query = query.Where(item => item.Quantity == quantity.Value)
                         .Where(item => item.TypeQuantity == type_quantity);
        }

        return await query.ToListAsync();
    }

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

    [HttpPost]
    public async Task<ActionResult<IEnumerable<Item>>> PostItem(
        string name, string description, string image, int id_category, int quantity, string type_quantity)
    {

        Item newItem = new Item
        {
            ItemName = name,
            Description = description,
            Image = image,
            IdCategory = id_category,
            Quantity = quantity,
            TypeQuantity = type_quantity
        };

        _context.Items.Add(newItem);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetItems), new { id = newItem.IdItem }, newItem);
    }
}