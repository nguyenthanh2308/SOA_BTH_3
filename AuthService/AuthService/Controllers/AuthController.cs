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
    // demo user store: username=admin, password=MD5("admin")
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

        // Tuỳ bài: req.Password đã là MD5 sẵn (bạn đang dùng vậy).
        // Nếu bạn muốn nhận raw password rồi hash tại server, thì thay dòng so sánh thành:
        // var md5 = HashUtil.Md5Hex(req.Password);
        var ok = req.UserName == DemoUser.UserName && req.Password == DemoUser.PasswordHash;
        if (!ok) return Unauthorized(new { message = "Invalid credentials" });

        var jwt = _cfg.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, req.UserName),
            new Claim(ClaimTypes.Name, req.UserName),
            new Claim(ClaimTypes.Role, DemoUser.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(jwt["ExpireMinutes"] ?? "60")),
            signingCredentials: creds
        );

        var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);
        return Ok(new { token = tokenStr });
    }

    [HttpGet("hello")]
    public IActionResult Hello() => Ok(new { message = "AuthService OK" });
}
