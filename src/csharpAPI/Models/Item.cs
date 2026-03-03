using System;
using System.Collections.Generic;

namespace csharpAPI.Models;

public partial class Item
{
    public int IdItem { get; set; }

    public string ItemName { get; set; } = null!;

    public string? Description { get; set; }

    public int? IdImage { get; set; }

    public int? IdCategory { get; set; }

    public int Quantity { get; set; }

    public string? TypeQuantity { get; set; }
}
