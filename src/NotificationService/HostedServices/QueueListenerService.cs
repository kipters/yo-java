using Amazon.SQS;
using Amazon.SQS.Model;

namespace NotificationService.HostedServices;

public class QueueListenerService<TMessageProcessor, TQueueUrlProvider> : BackgroundService
    where TMessageProcessor : IMessageProcessor
    where TQueueUrlProvider : IQueueUrlProvider
{
    private readonly TMessageProcessor _messageProcessor;
    private readonly TQueueUrlProvider _queueUrlProvider;
    private readonly IAmazonSQS _sqsClient;

    public QueueListenerService(TMessageProcessor messageProcessor, TQueueUrlProvider queueUrlProvider, IAmazonSQS sqsClient)
    {
        _messageProcessor = messageProcessor;
        _queueUrlProvider = queueUrlProvider;
        _sqsClient = sqsClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueUrl = await _queueUrlProvider.GetQueueUrl();

        while (!stoppingToken.IsCancellationRequested)
        {
            var receiveMessageRequest = new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = 20
            };

            var receiveMessageResponse = await _sqsClient.ReceiveMessageAsync(receiveMessageRequest, stoppingToken);

            if (receiveMessageResponse.Messages.Count == 0)
            {
                continue;
            }

            foreach (var message in receiveMessageResponse.Messages)
            {
                var messageProcessed = await _messageProcessor.ProcessMessageAsync(message, stoppingToken);

                if (!messageProcessed)
                {
                    continue;
                }

                var deleteMessageRequest = new DeleteMessageRequest
                {
                    QueueUrl = queueUrl,
                    ReceiptHandle = message.ReceiptHandle
                };

                await _sqsClient.DeleteMessageAsync(deleteMessageRequest, stoppingToken);
            }
        }
    }
}
