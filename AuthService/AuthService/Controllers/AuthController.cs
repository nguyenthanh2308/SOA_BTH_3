using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthService.Models;
using AuthService.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Controllers;

[ApiController]
[Route("")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _cfg;

    // Demo user (đơn giản cho bài thực hành)
    private static readonly UserRecord DemoUser = new()
    {
        UserName = "admin",
        PasswordHash = "21232f297a57a5a743894a0e4a801fc3", // MD5("admin")
        Role = "Admin"
    };

    public AuthController(IConfiguration cfg) => _cfg = cfg;

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.UserName) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { message = "Missing username/password" });

        // req.Password là MD5 sẵn
        var ok = req.UserName == DemoUser.UserName && req.Password == DemoUser.PasswordHash;
        if (!ok) return Unauthorized(new { message = "Invalid credentials" });

        // 🔹 PHÁT TOKEN TẠI ĐÂY
        var jwt = _cfg.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Claims (nhúng thông tin người dùng vào token)
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, req.UserName),
            new Claim(ClaimTypes.Name, req.UserName),
            new Claim(ClaimTypes.Role, DemoUser.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Sinh JWT
        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(jwt["ExpireMinutes"] ?? "60")),
            signingCredentials: creds
        );

        var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new
        {
            token = tokenStr,
            issuedAt = DateTime.UtcNow,
            expiresAt = DateTime.UtcNow.AddMinutes(double.Parse(jwt["ExpireMinutes"] ?? "60")),
            issuer = jwt["Issuer"],
            audience = jwt["Audience"],
            role = DemoUser.Role
        });
    }

    [HttpGet("hello")]
    public IActionResult Hello() => Ok(new { message = "AuthService OK" });
}
