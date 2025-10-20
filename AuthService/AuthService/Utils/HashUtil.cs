using System.Security.Cryptography;
using System.Text;

namespace AuthService.Utils;
public static class HashUtil
{
    public static string Md5Hex(string input)
    {
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant(); // ex: "21232f297a57a5a743894a0e4a801fc3" cho "admin"
    }
}
