using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using NotificationService.ConfigModel;

namespace NotificationService.HostedServices;

public class NotificationsMessageProcessor : IMessageProcessor, IQueueUrlProvider
{
    private readonly SqsConfig _config;

    public NotificationsMessageProcessor(IOptions<SqsConfig> config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _config = config.Value;
    }

    public ValueTask<string> GetQueueUrl() => new(_config.QueueUrl.ToString());
    public ValueTask<bool> ProcessMessageAsync(Message message, CancellationToken stoppingToken)
    {
        return new(true);
    }
}
