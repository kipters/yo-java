namespace NotificationService.HostedServices;

public interface IQueueUrlProvider
{
    ValueTask<string> GetQueueUrl();
}
