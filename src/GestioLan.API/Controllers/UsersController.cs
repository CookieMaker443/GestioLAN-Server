using Microsoft.AspNetCore.Mvc;
using GestioLan.API.Models;
using GestioLan.API.Services.Users;
using Microsoft.AspNetCore.Authorization;
 
namespace GestioLan.API.Controllers;
 
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
 
    public UsersController(IUserService userService)
    {
        _userService = userService;
    }


    // Login endpoint 
    [HttpPost("Login")]
    public async Task<ActionResult<IEnumerable<User>>> LoginUser(
        [FromBody] User loginUserdata
    )
    {
        try
        {
            var (user, token) = await _userService.LoginAsync(loginUserdata);
            return Ok(new { User = user, Token = token });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }


    // Crea un nuovo utente
    [HttpPost("Register")]
    public async Task<ActionResult<IEnumerable<User>>> PostUser(
        [FromBody] User user
    )
    {
        try
        {
            await _userService.RegisterAsync(user);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }


    [Authorize(Policy = "AdminOnly")] // Solo gli admin possono accedere a questo endpoint
    [HttpDelete("DeleteUser")]
    public async Task<IActionResult> DeleteUser(string username)
    {
        try
        {
            await _userService.DeleteUserAsync(username);
            return Ok($"User {username} deleted successfully.");
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }


    // #TODO: Quando si aggiorna il proprio username, bisogna vedere ed eventualmente rinominare anche l immagine profilo
    [Authorize]
    [HttpPut("{targetUsername}")]
    public async Task<IActionResult> PutUser(
        string targetUsername, [FromBody] User newUser)
    {
        var currentUsername = User.Identity?.Name;
        var currentUserIsAdmin = User.FindFirst("isAdmin")?.Value == "true";
 
        try
        {
            var message = await _userService.UpdateUserAsync(targetUsername, newUser, currentUsername, currentUserIsAdmin);
            return Ok(message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }


    [Authorize]
    [HttpGet("image/{username}")]
    public async Task<IActionResult> GetProfileImage(string username)
    {
        var currentUsername = User.Identity?.Name;
        var currentUserIsAdmin = User.FindFirst("isAdmin")?.Value == "true";

 
        // Autorizzazione: puoi procedere solo se sei l'interessato O sei un Admin
        if (currentUsername != username && !currentUserIsAdmin)
        {
            return Forbid("You are not authorized to update this user's data.");
        }
 
        try
        {
            var imageBytes = await _userService.GetProfileImageAsync(username);
            return File(imageBytes, "image/jpeg"); // Il browser/client vedrà un file immagine
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }


    [Authorize] 
    [HttpPost("image/{username}")]
    public async Task<IActionResult> UploadProfileImage(string username, IFormFile file)
    {
        var currentUsername = User.Identity?.Name;
        var currentUserIsAdmin = User.FindFirst("isAdmin")?.Value == "true";
 
        // Autorizzazione: puoi procedere solo se sei l'interessato O sei un Admin
        if (currentUsername != username && !currentUserIsAdmin)
        {
            return Forbid("You are not authorized to update this user's data.");
        }
 
        try
        {
            var url = await _userService.UploadProfileImageAsync(username, file);
            return Ok(new { message = "Immagine caricata con successo", url });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

}
