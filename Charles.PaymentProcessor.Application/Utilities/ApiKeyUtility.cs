namespace Charles.PayementProcessor.Application.Utilities;

using System.Security.Cryptography;
using System.Text;

public static class ApiKeyHelper
{
    public static string GenerateApiKey()
    {
        var keyBytes = new byte[32]; 
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(keyBytes);
        return Convert.ToBase64String(keyBytes);
    }

    public static string HashApiKey(string apiKey, string secretSalt)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretSalt));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToBase64String(hashBytes);
    }

    public static bool VerifyApiKey(string apiKey, string secretSalt, string storedHash)
    {
        var computedHash = HashApiKey(apiKey, secretSalt);
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(computedHash),
            Convert.FromBase64String(storedHash)
        );
    }
}