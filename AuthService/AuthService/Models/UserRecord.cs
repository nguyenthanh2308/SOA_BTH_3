namespace AuthService.Models;
public class UserRecord
{
    public string UserName { get; set; } = "";
    public string PasswordHash { get; set; } = ""; // MD5 dạng hex hoặc base16
    public string Role { get; set; } = "User";
}
