namespace Charles.PaymentProcessor.Api.Extension;

using Charles.PaymentProcessor.Domain.Entities;
using Charles.PaymentProcessor.Domain.Enums;
using Charles.PaymentProcessor.Domain.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;


    public static class ApplicationBuilderExtensions
    {
        public static async Task SeedDatabaseAsync(this WebApplication app, CancellationToken ct = default)
        {
            using var scope = app.Services.CreateScope();
            var merchantRepo = scope.ServiceProvider.GetRequiredService<IRepository<Merchant>>();
            var paymentMethodRepo = scope.ServiceProvider.GetRequiredService<IRepository<PaymentMethod>>();

            // 1️⃣ Seed Merchants
            if (!(await merchantRepo.GetAllAsync(ct)).Any())
            {
                var merchants = new List<Merchant>
                {
                    new Merchant { Name = "Merchant A", WebhookSecret = Guid.NewGuid().ToString() },
                    new Merchant { Name = "Merchant B", WebhookSecret = Guid.NewGuid().ToString() }
                };

                foreach (var merchant in merchants)
                    await merchantRepo.AddAsync(merchant, ct);

                Console.WriteLine($"Seeded {merchants.Count} merchants.");
            }

            // 2️⃣ Seed Payment Methods per merchant
            var allMerchants = await merchantRepo.GetAllAsync(ct);
            foreach (var merchant in allMerchants)
            {
                var existingMethods =  paymentMethodRepo.Query().Where(pm => pm.MerchantId == merchant.Id);
                if (!existingMethods.Any())
                {
                    var paymentMethods = new List<PaymentMethod>
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

                    Console.WriteLine($"Seeded {paymentMethods.Count} payment methods for {merchant.Name}.");
                }
            }
        }
    }
