using Charles.PayementProcessor.Application.Utilities;
using Charles.PaymentProcessor.Domain.Entities;
using Charles.PaymentProcessor.Domain.Enums;
using Charles.PaymentProcessor.Domain.Interfaces;
namespace Charles.PaymentProcessor.Api.Extension;

public static class ApplicationBuilderExtensions
{
    public static async Task SeedDatabaseAsync(this WebApplication app, CancellationToken ct = default)
    {
        using var scope = app.Services.CreateScope();
        var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var merchantRepo = scope.ServiceProvider.GetRequiredService<IRepository<Merchant>>();
        var paymentMethodRepo = scope.ServiceProvider.GetRequiredService<IRepository<PaymentMethod>>();

        var salt = cfg["ApiKeys:Salt"] ?? throw new InvalidOperationException("Missing ApiKeys:Salt in configuration.");

        var merchants = (await merchantRepo.GetAllAsync(ct)).ToList();

        if (!merchants.Any())
        {
            merchants = new List<Merchant>
            {
                new Merchant { Name = "Merchant A", WebhookSecret = Guid.NewGuid().ToString() },
                new Merchant { Name = "Merchant B", WebhookSecret = Guid.NewGuid().ToString() }
            };

            foreach (var m in merchants)
            {
                var apiKey = ApiKeyHelper.GenerateApiKey();
                m.ApiKeyHash = ApiKeyHelper.HashApiKey(apiKey, salt);

                await merchantRepo.AddAsync(m, ct);

                Console.WriteLine($"[SEED] {m.Name} API Key: {apiKey}");
            }

            Console.WriteLine($"[SEED] Seeded {merchants.Count} merchants with API keys.");
        }
        else
        {
            foreach (var m in merchants.Where(x => string.IsNullOrWhiteSpace(x.ApiKeyHash)))
            {
                var apiKey = ApiKeyHelper.GenerateApiKey();
                m.ApiKeyHash = ApiKeyHelper.HashApiKey(apiKey, salt);
                await merchantRepo.UpdateAsync(m, ct);

                Console.WriteLine($"[SEED] Backfilled API Key for {m.Name}: {apiKey}");
            }
        }

        foreach (var merchant in merchants)
        {
            var hasMethods = paymentMethodRepo.Query().Any(pm => pm.MerchantId == merchant.Id);
            if (hasMethods) continue;

            var paymentMethods = new[]
            {
                new PaymentMethod
                {
                    MerchantId = merchant.Id,
                    Type = PaymentMethodType.Card,
                    DisplayName = "Credit/Debit Card",
                    Details = "{}"
                },
                new PaymentMethod
                {
                    MerchantId = merchant.Id,
                    Type = PaymentMethodType.BankTransfer,
                    DisplayName = "Bank Transfer",
                    Details = "{}"
                },
                new PaymentMethod
                {
                    MerchantId = merchant.Id,
                    Type = PaymentMethodType.MobileMoney,
                    DisplayName = "Wallet",
                    Details = "{}"
                }
            };

            foreach (var pm in paymentMethods)
                await paymentMethodRepo.AddAsync(pm, ct);

            Console.WriteLine($"[SEED] Seeded {paymentMethods.Length} payment methods for {merchant.Name}.");
        }
    }
}