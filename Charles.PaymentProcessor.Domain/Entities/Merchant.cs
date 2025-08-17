using System.ComponentModel.DataAnnotations;

namespace Charles.PaymentProcessor.Domain.Entities;

public class Merchant
{
    public Guid Id { get; set; }
    [MaxLength(256)] public string Name { get; set; } = string.Empty;
    public string ApiKeyHash { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PaymentMethod> PaymentMethods { get; set; } = new List<PaymentMethod>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}