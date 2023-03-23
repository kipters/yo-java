using System.Diagnostics;
using System.Diagnostics.Metrics;
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
    private readonly ActivitySource _activitySource;
    private readonly Meter _meter;
    private readonly Histogram<int> _batchSizeHistogram;
    public const string ActivitySourceName = "QueueListenerService";
    public const string MeterName = "QueueListenerService";

    public QueueListenerService(TMessageProcessor messageProcessor, TQueueUrlProvider queueUrlProvider, IAmazonSQS sqsClient)
    {
        _messageProcessor = messageProcessor;
        _queueUrlProvider = queueUrlProvider;
        _sqsClient = sqsClient;
        _activitySource = new ActivitySource(ActivitySourceName);
        _meter = new Meter(MeterName);
        _batchSizeHistogram = _meter.CreateHistogram<int>("Sqs.Messages.BatchSize");
    }

    public override void Dispose()
    {
        _activitySource?.Dispose();
        _meter?.Dispose();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueUrl = await _queueUrlProvider.GetQueueUrl();

        if (!queueUrl.StartsWith("https://sqs.", StringComparison.OrdinalIgnoreCase))
        {
            var queueUrlResponse = await _sqsClient.GetQueueUrlAsync(queueUrl, stoppingToken);
            queueUrl = queueUrlResponse switch
            {
                { QueueUrl: not null } => queueUrlResponse.QueueUrl,
                _ => throw new InvalidOperationException("Queue not found")
            };
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            using var activity = _activitySource.StartActivity("ReceiveMessage", ActivityKind.Internal);
            var receiveMessageRequest = new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = 20,
                MessageAttributeNames = new List<string> { "ParentTraceId", "ParentSpanId" }
            };

            var receiveMessageResponse = await _sqsClient.ReceiveMessageAsync(receiveMessageRequest, stoppingToken);
            _batchSizeHistogram.Record(receiveMessageResponse.Messages.Count);

            if (receiveMessageResponse.Messages.Count == 0)
            {
                continue;
            }

            foreach (var message in receiveMessageResponse.Messages)
            {
                using var messageActivity = BuildActivityFromLinkedTrace(activity, message);
                messageActivity?.Start();

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

    private Activity? BuildActivityFromLinkedTrace(Activity? activity, Message? message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (activity is not null &&
            message.MessageAttributes.TryGetValue("ParentTraceId", out var parentTrace) &&
            parentTrace.DataType == "String" &&
            message.MessageAttributes.TryGetValue("ParentSpanId", out var parentSpan) &&
            parentSpan.DataType == "String")
        {
            var parentTraceId = ActivityTraceId.CreateFromString(parentTrace.StringValue);
            var parentSpanId = ActivitySpanId.CreateFromString(parentSpan.StringValue);
            var parentActivityContext = new ActivityContext(parentTraceId, parentSpanId, ActivityTraceFlags.Recorded);
            var links = new[] { new ActivityLink(parentActivityContext) };
            return _activitySource.CreateActivity("ProcessMessage", ActivityKind.Internal, activity.Context, links: links);
        }

        return _activitySource.CreateActivity("ProcessMessage", ActivityKind.Internal);
    }
}
