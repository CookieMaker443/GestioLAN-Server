using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestioLan.API.Models;
using Microsoft.AspNetCore.Authorization; // Per l'attributo [Authorize]
using GestioLan.API.Services.Categories;

namespace GestioLan.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    // Iniettiamo l'interfaccia del servizio, non più il DbContext direttamente
    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    // GET category di debug
    [Authorize] // Protegge questo endpoint, richiede un token JWT valido per accedervi
    [HttpGet("AllCategories")]
    public async Task<ActionResult<IEnumerable<Category>>> GetAllCategories()
    {
        var categories = await _categoryService.GetAllCategoriesAsync();
        return Ok(categories);
    }

    // inserisci una categoria
    [Authorize(Policy = "AdminOnly")] // Solo gli admin possono accedere a questo endpoint
    [HttpPost("AddCategory")]
    public async Task<ActionResult<Category>> AddCategory([FromBody] string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            return BadRequest("Category name cannot be empty.");
        }

        try
        {
            var category = await _categoryService.AddCategoryAsync(nome);
            return Ok(category);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // Aggiorna categoria
    [Authorize(Policy = "AdminOnly")] // Solo gli admin possono accedere a questo endpoint
    [HttpPut("UpdateCategory/{id}")]
    public async Task<ActionResult<Category>> UpdateCategory(int id, Category category)
    {
        try
        {
            var resultMessage = await _categoryService.UpdateCategoryAsync(id, category);
            return Ok(resultMessage);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Category with ID {id} not found.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // rimuovi categoria
    [Authorize(Policy = "AdminOnly")] // Solo gli admin possono accedere a questo endpoint
    [HttpDelete("DeleteCategory/{id}")]
    public async Task<ActionResult<Category>> DeleteCategory(int id)
    {
        try
        {
            var deletedCategory = await _categoryService.DeleteCategoryAsync(id);
            return Ok(deletedCategory);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Categoria con ID {id} non trovata.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}