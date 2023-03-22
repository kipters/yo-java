using Amazon.Runtime;
using Amazon.SQS;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    static ServiceCollectionExtensions()
    {
        LocalstackHost = Environment.GetEnvironmentVariable("LOCALSTACK_HOST");
    }

    public static string? LocalstackHost { get; }

    public static IServiceCollection AddAmazonSqs(this IServiceCollection services)
    {
#pragma warning disable CA2000
        var client = LocalstackHost switch
        {
            null => new AmazonSQSClient(),
            { } host => new AmazonSQSClient(new BasicAWSCredentials("foo", "bar"), new AmazonSQSConfig
            {
                ServiceURL = $"http://{host}:4566",
                UseHttp = true,

            })
        };

        services.AddSingleton<IAmazonSQS>(client);
        return services;
#pragma warning restore CA2000
    }
}
