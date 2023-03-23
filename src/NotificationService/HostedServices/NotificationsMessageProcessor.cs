using System.Text.Json;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using NotificationService.ConfigModel;
using NotificationService.NotificationChannels;
using Orleans.BroadcastChannel;

namespace NotificationService.HostedServices;

public class NotificationsMessageProcessor : IMessageProcessor, IQueueUrlProvider
{
    private readonly SqsConfig _config;
    private readonly ILogger<NotificationsMessageProcessor> _logger;
    private readonly IBroadcastChannelProvider _broadcastChannelProvider;

    public NotificationsMessageProcessor(IOptions<SqsConfig> config, ILogger<NotificationsMessageProcessor> logger, IClusterClient clusterClient)
    {
        ArgumentNullException.ThrowIfNull(config);
        _config = config.Value;
        _logger = logger;
        _broadcastChannelProvider = clusterClient.GetBroadcastChannelProvider("notifications");
    }

    public ValueTask<string> GetQueueUrl() => new(_config.QueueUrl!);
    public async ValueTask<bool> ProcessMessageAsync(Message message, CancellationToken stoppingToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(message?.Body))
            {
                _logger.LogWarning("Received empty message");
                return false;
            }

            var content = JsonSerializer.Deserialize<NotificationModel>(message.Body);

            if (content is null)
            {
                _logger.LogWarning("Received message with invalid content");
                return false;
            }

            var channelId = ChannelId.Create("notifications", content.Target);
            var stream = _broadcastChannelProvider.GetChannelWriter<NotificationModel>(channelId);
            await stream.Publish(content);
            return true;
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error while processing message");
            return false;
        }
    }
}
