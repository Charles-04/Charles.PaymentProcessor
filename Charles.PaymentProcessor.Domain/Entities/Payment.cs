using Charles.PaymentProcessor.Domain.Enums;

namespace Charles.PaymentProcessor.Domain.Entities;

public class Payment
{
    
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty; 
    public Guid MerchantId { get; set; }
    public Merchant Merchant { get; set; } = default!;
    public Guid? PaymentMethodId { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "NGN";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? CustomerEmail { get; set; }
    public string Metadata { get; set; } = "{}"; // jsonblob
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}