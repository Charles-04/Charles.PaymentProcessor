using Charles.PaymentProcessor.Domain.DTOs;

namespace Charles.PaymentProcessor.Domain.Interfaces;

public interface IPaymentService
{
    
    Task<string> InitializeAsync(Guid merchantId, decimal amount, string currency, string? email, string? metadataJson, CancellationToken ct);
    Task HandleGatewayWebhookAsync(WebhookPayload payload, string signature, Guid merchantId, CancellationToken ct);
}