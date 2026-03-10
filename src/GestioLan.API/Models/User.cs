using System;
using System.Collections.Generic;

namespace GestioLan.API.Models;

public partial class User
{
    public string Username { get; set; } = null!;

    public string? Email { get; set; }

    public string Password { get; set; } = null!;

    public bool IsAdmin { get; set; } = false;

    public DateTime? CreateTime { get; set; }
}
