using Charles.PaymentProcessor.Domain.Enums;

namespace Charles.PaymentProcessor.Domain.Entities;

public class PaymentMethod
{
    public Guid Id { get; set; }
    public Guid MerchantId { get; set; }
    public Merchant Merchant { get; set; } = default!;
    public PaymentMethodType Type { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Details { get; set; } = "{}"; // jsonb
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}