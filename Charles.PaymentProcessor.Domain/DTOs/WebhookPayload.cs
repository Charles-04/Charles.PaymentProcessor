using System.Text.Json.Serialization;

namespace Charles.PaymentProcessor.Domain.DTOs;

public record WebhookPayload
{
    public string Reference { get; init; } = string.Empty;
    public Guid? PaymentMethodId { get; init; }
    public bool Success { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public DateTime ProcessedAtUtc { get; init; } = DateTime.UtcNow;
}

public record WebhookRequest([property: JsonPropertyName("payload")] WebhookPayload Payload);
