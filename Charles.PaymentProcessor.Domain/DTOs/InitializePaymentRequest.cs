namespace Charles.PaymentProcessor.Domain.DTOs;

public record InitPaymentRequest(decimal Amount, string Currency, string? CustomerEmail, Dictionary<string, object>? Metadata);
