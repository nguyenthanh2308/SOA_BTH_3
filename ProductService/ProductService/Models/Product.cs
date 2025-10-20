using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductService.Models;

[Table("products")]                 // khớp tên bảng MySQL
public class Product
{
    [Key, Column("id")]
    public int Id { get; set; }

    [Required, StringLength(255), Column("name")]
    public string Name { get; set; } = "";

    [Column("description")]
    public string? Description { get; set; }

    [Column("price", TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
