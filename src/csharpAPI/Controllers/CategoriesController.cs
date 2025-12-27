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

    
}