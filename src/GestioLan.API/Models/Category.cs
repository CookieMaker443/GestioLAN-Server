using System;
using System.Collections.Generic;

namespace GestioLan.API.Models;

public partial class Category
{
    public int IdCategory { get; set; }
    public string NameCategory { get; set; } = null!;
    public string? AssociatedProviderName { get; set; }
}
