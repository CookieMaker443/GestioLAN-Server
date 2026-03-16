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
        IQueryable<Item> query = _context.Items;

        if (has_category.HasValue)
        {
            if (has_category.Value)
                query = query.Where(item => item.IdCategory != null);
            else
                query = query.Where(item => item.IdCategory == null);
        }

        if (id_category.HasValue)
        {
            // controllare il WHERE per la bitmask
            query = query.Where(item => 
                        (item.IdCategory & id_category.Value) == id_category.Value && 
                        item.IdCategory != null);
        }

        if (!string.IsNullOrEmpty(name))
        {
            query = query.Where(item => item.ItemName.Contains(name));
        }

        if (has_image.HasValue)
        {
            if (has_image.Value)
                query = query.Where(item => item.IdImage != null);
            else
                query = query.Where(item => item.IdImage == null);
        }

        if (id_category.HasValue)
        {
            // controllare il WHERE per la bitmask
            query = query.Where(item => 
                        (item.IdCategory & id_category.Value) == id_category.Value && 
                        item.IdCategory != null);
        }

        if (!string.IsNullOrEmpty(name))
        {
            query = query.Where(item => item.ItemName.Contains(name));
        }

        if (quantity.HasValue)
            query = query.Where(item => item.Quantity == quantity.Value);

        if (!string.IsNullOrEmpty(type_quantity))
            query = query.Where(item => item.TypeQuantity == type_quantity);

        return await query.ToListAsync();
    }

    // Ottiene un singolo oggetto del DB tramite il suo ID    
    [Authorize]
    [HttpGet("GetItems/{id}")]
    public async Task<ActionResult<Item>> GetItem(int id)
    {
        var item = await _context.Items.FindAsync(id);

        if (item == null)
        {
            return NotFound(); // <-- Ritorna un codice 404
        }
        return item;
    }

    [Authorize]
    [HttpPost("CreateItem")]
    public async Task<ActionResult<IEnumerable<Item>>> PostItem([FromBody] Item item)
    {
        if (item.IdImage == 0 || item.IdImage == null)
        {
            item.IdImage = null;
            item.ImageName = null;
        }

        // se  l'immagine dell item non è null, aggiorna il counter di quell'immagine
        // se ha un'immagine, verifica che esista e aggiorna il contatore
        if (item.IdImage != null)
        {
            var image = await _context.Images.FindAsync(item.IdImage);
            if (image == null)
            {
                // immagine specificata non esiste, pulisce i campi e avvisa
                item.IdImage = null;
                item.ImageName = null;
                Console.WriteLine($"Immagine con id {item.IdImage} non trovata, item aggiunto senza immagine");
            }
            else
            {
                image.ItemsCount++;
                item.ImageName = image.FileName;
            }
        }
        else
        {
            item.ImageName = null;
        }
        //aggiunge il nuovo oggetto al DB, con l'immmagine se è stata specificata
        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetItem), new { id = item.IdItem }, item);
    }


    [Authorize]
    [HttpDelete("DeleteItem/{id}")]
    public async Task<ActionResult<IEnumerable<Item>>> DeleteItem(int id)
    {
        var item = await _context.Items.FindAsync(id);
        if (item == null)
        {
            return NotFound();
        }

        var image = await _context.Images.FindAsync(item.IdImage);
        if (image != null)
        {
            image.ItemsCount--;
            // image.LastModified = DateTime.Now;
        }

        _context.Items.Remove(item);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [Authorize]
    [HttpPut("ModifyItem/{id}")]
    public async Task<IActionResult> PutItem(
        int id, Item updatedItem)
    {

        if (id != updatedItem.IdItem)
        {
            return BadRequest("Id mismatch");
        }

        var item = await _context.Items.FindAsync(id);
        if (item == null)
        {
            return NotFound();
        }

        item.ItemName = updatedItem.ItemName;
        item.Description = updatedItem.Description;

        item.IdCategory = updatedItem.IdCategory;
        item.Quantity = updatedItem.Quantity;
        item.TypeQuantity = updatedItem.TypeQuantity;

        // se il vecchio e nuovo item sono senza immagine, "pulisce" i campi immagine e non fa nulla
        if ((item.IdImage != updatedItem.IdImage) && updatedItem.IdImage != null)
        {    
            var newImage = await _context.Images.FindAsync(updatedItem.IdImage);
            if (newImage != null)
            {
                // abbassa contatore vecchia immagine
                if (item.IdImage != null)
                {
                    var oldImage = await _context.Images.FindAsync(item.IdImage);
                    if (oldImage != null)
                        oldImage.ItemsCount--;
                }

                // aumenta contatore nuova immagine
                newImage.ItemsCount++;
                item.ImageName = newImage.FileName;
                item.IdImage = updatedItem.IdImage;
            } 
            else
            {
                // immagine specificata non esiste, pulisce i campi e avvisa
                if (item.IdImage != null)
                {
                    var oldImage = await _context.Images.FindAsync(item.IdImage);
                    if (oldImage != null)
                        oldImage.ItemsCount--;
                }
                item.IdImage = null;
                item.ImageName = null;
                Console.WriteLine($"Immagine con id {updatedItem.IdImage} non trovata, item aggiornato senza immagine");
            }  
        }
        else if (updatedItem.IdImage == null)
        // se il nuovo item è senza immagine, abbassa il contatore della vecchia immagine (se esiste) e pulisce i campi immagine senza fare query inutili
        {
            if (item.IdImage != null)
            {
                var oldImage = await _context.Images.FindAsync(item.IdImage);
                if (oldImage != null)
                    oldImage.ItemsCount--;
            }
            item.IdImage = null;
            item.ImageName = null;
        }

        await _context.SaveChangesAsync();

        return Ok("Item updated successfully");
    }
}