
using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Charles.PaymentProcessor.Domain.Entities;
using Charles.PaymentProcessor.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Charles.PaymentProcessor.Worker.Services;

public class PaymentEventConsumer : BackgroundService
{
    private readonly IAmazonSQS _sqs;
    private readonly string _queueUrl;
    private readonly ILogger<PaymentEventConsumer> _logger;
    private readonly IRepository<Payment> _paymentRepository;

    public PaymentEventConsumer(
        IAmazonSQS sqs,
        string queueUrl,
        ILogger<PaymentEventConsumer> logger,
        IRepository<Payment> paymentRepository)
    {
        _sqs = sqs;
        _queueUrl = queueUrl;
        _logger = logger;
        _paymentRepository = paymentRepository;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PaymentEventConsumer started. Listening for messages on {QueueUrl}", _queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 5,
                    WaitTimeSeconds = 10
                }, stoppingToken);

                foreach (var message in response.Messages)
                {
                    _logger.LogInformation("[SQS] Received message: {Body}", message.Body);

                    try
                    {
                        var payment = JsonSerializer.Deserialize<Payment>(message.Body);

                        if (payment != null)
                        {
                            await _paymentRepository.AddAsync(payment);
                            _logger.LogInformation("[DB] Payment saved successfully: {PaymentId}", payment.Id);
                        }
                        else
                        {
                            _logger.LogWarning("[SQS] Could not deserialize message: {Body}", message.Body);
                        }

                        await _sqs.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, stoppingToken);
                        _logger.LogInformation("[SQS] Deleted message with ReceiptHandle: {ReceiptHandle}",
                            message.ReceiptHandle);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[SQS] Error while handling message: {Body}", message.Body);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing messages from {QueueUrl}", _queueUrl);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); 
            }
        }

        _logger.LogInformation("PaymentEventConsumer is stopping.");
    }
}