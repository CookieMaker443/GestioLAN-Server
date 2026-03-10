using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestioLan.API.Models;

[Table("images")]
public class Image
{

    public int IdImage { get; set; }

    public string FileName { get; set; } = null!;

    public int ItemsCount { get; set; } = 0;

    public DateTime? LastModified { get; set; }
}