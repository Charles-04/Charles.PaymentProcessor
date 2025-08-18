using Charles.PaymentProcessor.Domain.Entities;

namespace Charles.PaymentProcessor.Domain.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(Merchant merchant);
}