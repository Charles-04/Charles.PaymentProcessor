using System.Text.Json;
using Charles.PayementProcessor.Application.Services;
using FluentAssertions;

namespace Charles.PaymentProcessor.Test;
using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

    public class SqsEventPublisherTests
    {
        private readonly Mock<IAmazonSQS> _sqsMock;
        private readonly Mock<ILogger<SqsEventPublisher>> _loggerMock;
        private readonly IConfiguration _config;
        private readonly SqsEventPublisher _publisher;
        private readonly CancellationToken _ct = CancellationToken.None;


        public SqsEventPublisherTests()
        {
            _sqsMock = new Mock<IAmazonSQS>();
            _loggerMock = new Mock<ILogger<SqsEventPublisher>>();

            var inMemorySettings = new Dictionary<string, string>
            {
                {"Queues:PaymentEventsUrl", "https://sqs.mock-queue-url"}
            };
            _config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

            _publisher = new SqsEventPublisher(_sqsMock.Object, _config, _loggerMock.Object);
        }

        [Fact]
        public async Task PublishAsync_SendsMessageToQueue()
        {
            // Arrange
            var testEvent = new { Name = "TestUser", Amount = 100 };
            _sqsMock.Setup(s => s.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new SendMessageResponse { MessageId = "12345" });

            // Act
            await _publisher.PublishAsync("TestTopic", testEvent);

            // Assert
            _sqsMock.Verify(s => s.SendMessageAsync(
                It.Is<SendMessageRequest>(r =>
                    r.QueueUrl == "https://sqs.mock-queue-url" &&
                    r.MessageBody.Contains("TestTopic") &&
                    r.MessageBody.Contains("TestUser")
                ),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public void Constructor_ThrowsIfMissingQueueUrl()
        {
            // Arrange
            var emptyConfig = new ConfigurationBuilder().Build();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                new SqsEventPublisher(_sqsMock.Object, emptyConfig, _loggerMock.Object));
        }
        
        [Fact]
        public async Task PublishAsync_SendsMessage_ToSqs_AndLogsInformation()
        {
            // Arrange
            var publisher = new SqsEventPublisher(_sqsMock.Object, _config, _loggerMock.Object);
            var testEvent = new { Foo = "bar" };

            SendMessageRequest? capturedRequest = null;

            _sqsMock
                .Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
                .Callback<SendMessageRequest, CancellationToken>((req, _) => capturedRequest = req)
                .ReturnsAsync(new SendMessageResponse());

            // Act
            await publisher.PublishAsync("TestTopic", testEvent);

            // Assert
            capturedRequest.Should().NotBeNull();
            capturedRequest!.QueueUrl.Should().Be("https://sqs.mock-queue-url");

            var bodyJson = JsonSerializer.Deserialize<Dictionary<string, object>>(capturedRequest.MessageBody)!;

            var data = bodyJson!["data"];
            var topic = bodyJson!["topic"];
            bodyJson.Should().ContainKey("data").WhoseValue.Should().Be(data);
            bodyJson.Should().ContainKey("topic").WhoseValue.Should().Be(topic);

            // Verify logger call
            _loggerMock.Verify(
                x => x.Log<It.IsAnyType>(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) =>
                        v.ToString()!.Contains("Publishing TestTopic") &&
                        v.ToString()!.Contains("\"Foo\":\"bar\"")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                Times.Once);
        }    
    }
