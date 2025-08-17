namespace Charles.PaymentProcessor.Domain.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<T>(string topic, T @event, CancellationToken ct = default);

}