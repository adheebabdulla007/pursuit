using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pursuit.Application.Messages;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Pursuit.Infrastructure.Messaging;

public sealed class ApplicationSubmittedConsumer : BackgroundService
{
    private const string ExchangeName = "pursuit.direct";
    private const string QueueName = "application.submitted";
    private const string RoutingKey = "application.submitted";

    private readonly IConnection _connection;
    private readonly ILogger<ApplicationSubmittedConsumer> _logger;
    private IChannel? _channel;

    public ApplicationSubmittedConsumer(IConnection connection, ILogger<ApplicationSubmittedConsumer> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueBindAsync(
            queue: QueueName,
            exchange: ExchangeName,
            routingKey: RoutingKey,
            cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (sender, args) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(args.Body.Span);
                var message = JsonSerializer.Deserialize<ApplicationSubmittedMessage>(body);

                if (message is null)
                {
                    _logger.LogWarning("Received null or ungeneralizable message. Acknowledging to discard.");
                    await _channel.BasicAckAsync(args.DeliveryTag, multiple: false);
                    return;
                }

                _logger.LogInformation(
                    "EMAIL TO EMPLOYER [{EmployerEmail}]: New application received for '{JobTitle}' from {ApplicantName}.",
                    message.EmployerEmail, message.JobTitle, message.ApplicantName);

                _logger.LogInformation(
                    "EMAIL TO APPLICANT [{ApplicantEmail}]: Your application for '{JobTitle}' at {CompanyName} has been received.",
                    message.ApplicantEmail, message.JobTitle, message.CompanyName);

                await _channel.BasicAckAsync(args.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process ApplicationSubmittedMessage. Re-queuing.");
                await _channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // stoppingToken cancelled — application is shutting down, exit cleanly
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        if (_channel is not null)
        {
            await _channel.CloseAsync();
            await _channel.DisposeAsync();
        }
    }
}