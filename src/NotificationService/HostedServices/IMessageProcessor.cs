using Amazon.SQS.Model;

namespace NotificationService.HostedServices;

public interface IMessageProcessor
{
    ValueTask<bool> ProcessMessageAsync(Message message, CancellationToken stoppingToken);
}
