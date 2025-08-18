using System.ComponentModel.DataAnnotations;

namespace Charles.PaymentProcessor.Domain.DTOs;

public record InitPaymentRequest([Range(0.01, double.MaxValue, ErrorMessage = "Value must be greater than 0")]decimal Amount, [Length(2,3)]string Currency, [EmailAddress]string? CustomerEmail, Dictionary<string, object>? Metadata);
