using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductService.Models
{
    [Table("products")]
    public class Product
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required, MaxLength(255)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        [Column("description")]
        public string? Description { get; set; }

        [Required]
        [Column("price", TypeName = "decimal(12,2)")]
        public decimal Price { get; set; }

        [Required]
        [Column("quantity")]
        public int Quantity { get; set; }

        [Column("created_at", TypeName = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 👇 để nullable để khớp với mapping IsRequired(false)
        [Column("updated_at", TypeName = "datetime")]
        public DateTime? UpdatedAt { get; set; }
    }
}
