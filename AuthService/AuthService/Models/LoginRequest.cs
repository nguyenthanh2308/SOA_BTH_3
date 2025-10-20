namespace AuthService.Models;
public class LoginRequest
{
    public string UserName { get; set; } = "";
    public string Password { get; set; } = ""; // theo bài của bạn: gửi MD5("admin")
}
