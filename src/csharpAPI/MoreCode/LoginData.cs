using System.ComponentModel.DataAnnotations;

namespace csharpAPI.Models;

public class LoginData
{
    [Required] // Indica che questo campo è obbligatorio nella richiesta
    public string Username { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}