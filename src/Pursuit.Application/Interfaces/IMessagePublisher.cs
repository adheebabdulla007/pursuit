using Pursuit.Application.Messages;

namespace Pursuit.Application.Interfaces;

public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default);
}