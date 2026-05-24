using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GestioLan.API.Services.Metadata;

namespace GestioLan.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MetadataController : ControllerBase
{
    private readonly IMetadataService _metadataService;

    public MetadataController(IMetadataService metadataService)
    {
        _metadataService = metadataService;
    }

    // Restituisce i nomi di tutti i plugin caricati in memoria
    [Authorize]
    [HttpGet("Providers")]
    public IActionResult GetLoadedProviders()
    {
        var providers = _metadataService.GetLoadedProviders();
        return Ok(providers);
    }

    // Associa un provider a una categoria
    [Authorize(Policy = "AdminOnly")]
    [HttpPut("AssociateProvider/{idCategory}")]
    public async Task<IActionResult> AssociateProvider(int idCategory, [FromQuery] string providerName)
    {
        try
        {
            await _metadataService.AssociateProviderToCategoryAsync(idCategory, providerName);
            return Ok(new { message = $"Provider '{providerName}' associato alla categoria {idCategory}" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}