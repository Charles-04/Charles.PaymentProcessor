using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Charles.PaymentProcessor.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Charles.PayementProcessor.Application.Services;

public class SqsEventPublisher : IEventPublisher
{
    private readonly IAmazonSQS _sqs;
    private readonly ILogger<SqsEventPublisher> _logger;
    private readonly string _queueUrl;

    public SqsEventPublisher(IAmazonSQS sqs, IConfiguration cfg, ILogger<SqsEventPublisher> logger)
    {
        _sqs = sqs; _logger = logger;
        _queueUrl = cfg["Queues:PaymentEventsUrl"] ?? throw new InvalidOperationException("Missing Queues:PaymentEventsUrl");
    }

    public async Task PublishAsync<T>(string topic, T @event, CancellationToken ct = default)
    {
        var envelope = new
        {
            topic,
            id = Guid.NewGuid(),
            occurredAtUtc = DateTime.UtcNow,
            data = @event
        };
        var body = JsonSerializer.Serialize(envelope);
        _logger.LogInformation("Publishing {Topic}: {Body}", topic, body);
        await _sqs.SendMessageAsync(new SendMessageRequest { QueueUrl = _queueUrl, MessageBody = body }, ct);
    }
}