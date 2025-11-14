using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Models;

[ApiController]
[Route("customers")]
public class CustomersController : ControllerBase
{
    private readonly OrderDbContext _db;
    public CustomersController(OrderDbContext db) => _db = db;

    // ===== DTOs linh hoạt =====
    public class RegisterDto
    {
        [JsonPropertyName("fullName")] public string? FullName { get; set; }
        // Chấp nhận alias "name" từ UI cũ
        [JsonPropertyName("name")] public string? Name { get; set; }

        public string? Email { get; set; }
        public string? Password { get; set; }
    }
    public class LoginDto
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
    }
    public record CustomerDto(int Id, string FullName, string Email, DateTime CreatedAt);

    // ===== Helpers =====
    private static string Md5Hex(string? input)
    {
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input ?? string.Empty));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    // ===== Register =====
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var fullName = string.IsNullOrWhiteSpace(dto.FullName) ? dto.Name : dto.FullName;
        var email = dto.Email?.Trim().ToLowerInvariant();
        var password = dto.Password;

        if (string.IsNullOrWhiteSpace(fullName))
            return BadRequest(new { message = "Full name is required." });
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Email is required." });
        if (string.IsNullOrWhiteSpace(password))
            return BadRequest(new { message = "Password is required." });

        if (await _db.Customers.AnyAsync(x => x.Email == email))
            return Conflict(new { message = "Email already exists." });

        var c = new Customer
        {
            FullName = fullName!.Trim(),
            Email = email!,
            PasswordMd5 = Md5Hex(password),
            CreatedAt = DateTime.UtcNow
        };

        _db.Customers.Add(c);
        await _db.SaveChangesAsync();

        return Created($"/customers/{c.Id}", new CustomerDto(c.Id, c.FullName, c.Email, c.CreatedAt));
    }

    // ===== Login =====
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var email = dto.Email?.Trim().ToLowerInvariant();
        var pass = dto.Password;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pass))
            return BadRequest(new { message = "Email and password are required." });

        var hash = Md5Hex(pass);
        var c = await _db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == email && x.PasswordMd5 == hash);

        if (c is null)
            return Unauthorized(new { message = "Invalid email or password." });

        return Ok(new CustomerDto(c.Id, c.FullName, c.Email, c.CreatedAt));
    }

    // ... các action khác giữ nguyên (Get, Update, ...)
}
