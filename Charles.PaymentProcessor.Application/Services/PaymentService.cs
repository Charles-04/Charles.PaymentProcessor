using Charles.PaymentProcessor.Domain.DTOs;
using Charles.PaymentProcessor.Domain.Entities;
using Charles.PaymentProcessor.Domain.Enums;
using Charles.PaymentProcessor.Domain.Interfaces;

namespace Charles.PayementProcessor.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IRepository<Payment> _paymentRepo;
    private readonly IRepository<Merchant> _merchantRepo;
    private readonly IEventPublisher _publisher;
    private readonly ISignatureVerifier _sig;

    public PaymentService(
        IRepository<Payment> paymentRepo,
        IRepository<Merchant> merchantRepo,
        IEventPublisher publisher,
        ISignatureVerifier sig)
    {
        _paymentRepo = paymentRepo;
        _merchantRepo = merchantRepo;
        _publisher = publisher;
        _sig = sig;
    }

    public async Task<string> InitializeAsync(
        Guid merchantId,
        decimal amount,
        string currency,
        string? email,
        string? metadataJson,
        CancellationToken ct)
    {
        var reference = $"pay_{Guid.NewGuid():N}";
        var payment = new Payment
        {
            Reference = reference,
            MerchantId = merchantId,
            Amount = amount,
            Currency = currency,
            Status = PaymentStatus.Pending,
            CustomerEmail = email,
            Metadata = metadataJson ?? "{}"
        };

        await _paymentRepo.AddAsync(payment, ct);

        await _publisher.PublishAsync(
            "payment-initiated",
            new PaymentInitiatedEvent(
                reference,
                payment.Id,
                merchantId,
                amount,
                currency,
                DateTime.UtcNow),
            ct);

        return reference;
    }

    public async Task HandleGatewayWebhookAsync(
        WebhookPayload payload,
        string signature,
        Guid merchantId,
        CancellationToken ct)
    {
        var merchant = await _merchantRepo.GetByIdAsync(merchantId, ct)
            ?? throw new KeyNotFoundException("Merchant not found");

        if (!_sig.Verify(merchant.WebhookSecret, payload, signature))
            throw new UnauthorizedAccessException("Invalid webhook signature");

        var payment = await _paymentRepo.GetSingleByAsync( x => x.MerchantId == merchantId && x.Reference == payload.Reference, ct)
            ?? throw new KeyNotFoundException("Payment not found");

        var newStatus = payload.Success ? PaymentStatus.Succeeded : PaymentStatus.Failed;
        if (payment.Status == newStatus)
        {
            return;
        }        payment.PaymentMethodId = payload.PaymentMethodId;
        payment.UpdatedAt = DateTime.UtcNow;

        await _paymentRepo.UpdateAsync(payment, ct);

        await _publisher.PublishAsync(
            "payment-completed",
            new PaymentCompletedEvent(
                payment.Reference,
                payment.Id,
                payment.MerchantId,
                payment.Status,
                DateTime.UtcNow),
            ct);
    }
}