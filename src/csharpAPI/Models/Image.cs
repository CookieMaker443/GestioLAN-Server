using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace csharpAPI.Models;

[Table("images")]
public class Image
{
    [Key]
    [Column("id_image")]
    public int IdImage { get; set; }

    [Required]
    [Column("file_name")]
    public string FileName { get; set; } = null!;

    [Column("items_count")]
    public int ItemsCount { get; set; } = 0;
}