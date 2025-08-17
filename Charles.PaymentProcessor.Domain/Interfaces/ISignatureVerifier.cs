using Charles.PaymentProcessor.Domain.DTOs;

namespace Charles.PaymentProcessor.Domain.Interfaces;

public interface ISignatureVerifier
{
    bool Verify(string secret, WebhookPayload payload, string signatureHeader);

}