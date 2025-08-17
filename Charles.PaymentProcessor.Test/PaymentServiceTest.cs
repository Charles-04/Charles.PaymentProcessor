using System.Linq.Expressions;
using Charles.PaymentProcessor.Domain.DTOs;
using Charles.PaymentProcessor.Domain.Entities;
using Charles.PaymentProcessor.Domain.Enums;
using Charles.PaymentProcessor.Domain.Interfaces;
using Charles.PayementProcessor.Application.Services;
using NSubstitute;

namespace Charles.PaymentProcessor.Test;

    public class PaymentServiceTests
    {
        private readonly IRepository<Payment> _paymentRepo;
        private readonly IRepository<Merchant> _merchantRepo;
        private readonly IEventPublisher _publisher;
        private readonly ISignatureVerifier _sig;
        private readonly PaymentService _sut; 
        private readonly CancellationToken _ct = CancellationToken.None;

        private const string TestReference = "pay_1234567890";
        private const string TestSecret = "secret";

        public PaymentServiceTests()
        {
            _paymentRepo = Substitute.For<IRepository<Payment>>();
            _merchantRepo = Substitute.For<IRepository<Merchant>>();
            _publisher = Substitute.For<IEventPublisher>();
            _sig = Substitute.For<ISignatureVerifier>();
            _sut = new PaymentService(_paymentRepo, _merchantRepo, _publisher, _sig);
        }

        [Fact]
        public async Task InitializeAsync_ValidInput_CreatesPaymentAndPublishesEvent()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var amount = 100.50m;
            var currency = "USD";
            var email = "test@example.com";
            var metadataJson = "{\"key\":\"value\"}";

            // Act
            var reference = await _sut.InitializeAsync(merchantId, amount, currency, email, metadataJson, _ct);

            // Assert
            Assert.NotNull(reference);
            Assert.StartsWith("pay_", reference);

            await _paymentRepo.Received(1).AddAsync(
                Arg.Is<Payment>(p =>
                    p.MerchantId == merchantId &&
                    p.Amount == amount &&
                    p.Currency == currency &&
                    p.Status == PaymentStatus.Pending &&
                    p.CustomerEmail == email &&
                    p.Metadata == metadataJson),
                _ct);

            await _publisher.Received(1).PublishAsync(
                "payment-initiated",
                Arg.Is<PaymentInitiatedEvent>(e =>
                    e.Reference == reference &&
                    e.MerchantId == merchantId &&
                    e.Amount == amount &&
                    e.Currency == currency),
                _ct);
        }

        [Fact]
        public async Task HandleGatewayWebhookAsync_ValidPayloadAndSignature_UpdatesPaymentAndPublishesEvent()
        {
            // Arrange
            var merchant = CreateMerchant();
            var payment = CreatePayment(merchant.Id, TestReference);
            var payload = CreatePayload(TestReference, true);

            _merchantRepo.GetByIdAsync(merchant.Id, _ct).Returns(merchant);
            _sig.Verify(merchant.WebhookSecret, payload, "valid_signature").Returns(true);
            _paymentRepo
                .GetSingleByAsync(Arg.Any<Expression<Func<Payment, bool>>>(), _ct)
                .Returns(payment);

            // Act
            await _sut.HandleGatewayWebhookAsync(payload, "valid_signature", merchant.Id, _ct);

            // Assert
            Assert.Equal(PaymentStatus.Succeeded, payment.Status);
            Assert.Equal(payload.PaymentMethodId, payment.PaymentMethodId);
            Assert.NotEqual(default, payment.UpdatedAt);

            await _paymentRepo.Received(1).UpdateAsync(
                Arg.Is<Payment>(p =>
                    p.Id == payment.Id &&
                    p.Status == PaymentStatus.Succeeded &&
                    p.PaymentMethodId == payload.PaymentMethodId),
                _ct);

            await _publisher.Received(1).PublishAsync(
                "payment-completed",
                Arg.Is<PaymentCompletedEvent>(e =>
                    e.Reference == TestReference &&
                    e.PaymentId == payment.Id &&
                    e.MerchantId == merchant.Id &&
                    e.Status == PaymentStatus.Succeeded),
                _ct);
        }

        [Fact]
        public async Task HandleGatewayWebhookAsync_MerchantNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var payload = CreatePayload(TestReference, true);
            _merchantRepo.GetByIdAsync(merchantId, _ct).Returns((Merchant)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _sut.HandleGatewayWebhookAsync(payload, "signature", merchantId, _ct));
        }

        [Fact]
        public async Task HandleGatewayWebhookAsync_InvalidSignature_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var merchant = CreateMerchant();
            var payload = CreatePayload(TestReference, true);

            _merchantRepo.GetByIdAsync(merchant.Id, _ct).Returns(merchant);
            _sig.Verify(merchant.WebhookSecret, payload, "invalid_signature").Returns(false);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _sut.HandleGatewayWebhookAsync(payload, "invalid_signature", merchant.Id, _ct));
        }

        [Fact]
        public async Task HandleGatewayWebhookAsync_PaymentNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var merchant = CreateMerchant();
            var payload = CreatePayload(TestReference, true);

            _merchantRepo.GetByIdAsync(merchant.Id, _ct).Returns(merchant);
            _sig.Verify(merchant.WebhookSecret, payload, "valid_signature").Returns(true);
            _paymentRepo
                .GetSingleByAsync(Arg.Any<Expression<Func<Payment, bool>>>(), _ct)
                .Returns((Payment)null);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _sut.HandleGatewayWebhookAsync(payload, "valid_signature", merchant.Id, _ct));
        }

        #region Helpers

        private static Merchant CreateMerchant() =>
            new() { Id = Guid.NewGuid(), WebhookSecret = TestSecret };

        private static Payment CreatePayment(Guid merchantId, string reference) =>
            new()
            {
                Id = Guid.NewGuid(),
                MerchantId = merchantId,
                Reference = reference,
                Status = PaymentStatus.Pending
            };

        private static WebhookPayload CreatePayload(string reference, bool success) =>
            new()
            {
                Reference = reference,
                Success = success,
                PaymentMethodId = Guid.NewGuid()
            };

        #endregion
    }
