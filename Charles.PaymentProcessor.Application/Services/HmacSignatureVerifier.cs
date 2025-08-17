using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Charles.PaymentProcessor.Domain.DTOs;
using Charles.PaymentProcessor.Domain.Interfaces;

namespace Charles.PayementProcessor.Application.Services;

public class HmacSignatureVerifier : ISignatureVerifier
{
    public bool Verify(string secret, WebhookPayload payload, string signatureHeader)
    {
        var json = JsonSerializer.Serialize(payload);
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(json));
        var expected = Convert.ToHexString(hash).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signatureHeader.Trim().ToLowerInvariant()));
    }
}