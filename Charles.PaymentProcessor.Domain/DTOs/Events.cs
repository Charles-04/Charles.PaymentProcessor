using Charles.PaymentProcessor.Domain.Enums;

namespace Charles.PaymentProcessor.Domain.DTOs;

public record PaymentInitiatedEvent(string Reference, Guid PaymentId, Guid MerchantId, decimal Amount, string Currency, DateTime OccurredAtUtc);
public record PaymentCompletedEvent(string Reference, Guid PaymentId, Guid MerchantId, PaymentStatus Status, DateTime OccurredAtUtc);