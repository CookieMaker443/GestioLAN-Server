
using Microsoft.AspNetCore.Mvc;
using GestioLan.API.Models;
using GestioLan.API.Services.Items;
using Microsoft.AspNetCore.Authorization; // Per l'attributo [Authorize]
 
namespace GestioLan.API.Controllers;
 
[ApiController]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
    private readonly IItemService _itemService;
 
    public ItemsController(IItemService itemService)
    {
        _itemService = itemService;
    }


    // Ottiene tutti gli oggetti del DB
    [Authorize]
    [HttpGet("GetItems")]
    public async Task<ActionResult<IEnumerable<Item>>> GetItems(
        [FromQuery] bool? has_category,
        [FromQuery] int? id_category,
        [FromQuery] string? name,
        [FromQuery] bool? has_image,
        [FromQuery] int? quantity,
        [FromQuery] string? type_quantity
        )
    {
        var items = await _itemService.GetItemsAsync(has_category, id_category, name, has_image, quantity, type_quantity);
        return Ok(items);
    }


    // Ottiene un singolo oggetto del DB tramite il suo ID    
    [Authorize]
    [HttpGet("GetItems/{id}")]
    public async Task<ActionResult<Item>> GetItem(int id)
    {
        try
        {
            var item = await _itemService.GetItemByIdAsync(id);
            return Ok(item);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(); // <-- Ritorna un codice 404
        }
    }


    [Authorize]
    [HttpPost("CreateItem")]
    public async Task<ActionResult<IEnumerable<Item>>> PostItem([FromBody] Item item)
    {
        var createdItem = await _itemService.CreateItemAsync(item);
        return CreatedAtAction(nameof(GetItem), new { id = createdItem.IdItem }, createdItem);
    }



    [Authorize]
    [HttpDelete("DeleteItem/{id}")]
    public async Task<ActionResult<IEnumerable<Item>>> DeleteItem(int id)
    {
        try
        {
            await _itemService.DeleteItemAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }


    [Authorize]
    [HttpPut("ModifyItem/{id}")]
    public async Task<IActionResult> PutItem(
        int id, Item updatedItem)
    {
        try
        {
            await _itemService.UpdateItemAsync(id, updatedItem);
            return Ok("Item updated successfully");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}