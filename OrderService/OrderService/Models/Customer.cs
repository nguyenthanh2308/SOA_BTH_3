using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace OrderService.Models
{
    [Table("customers")]
    [Index(nameof(Email), IsUnique = true)] // đảm bảo email là duy nhất ở DB
    public class Customer
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required, MaxLength(255)]
        [Column("full_name")]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        // Lưu HASH MD5 (32 ký tự hex) của mật khẩu
        [Required, MaxLength(32)]
        [Column("password_md5")]
        public string PasswordMd5 { get; set; } = string.Empty;

        [Column("created_at", TypeName = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 1 khách hàng có nhiều đơn
        [JsonIgnore] // tránh vòng lặp khi trả JSON
        public List<Order> Orders { get; set; } = new();
    }
}
