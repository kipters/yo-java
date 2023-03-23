using NotificationService.ConfigModel;
using NotificationService.HostedServices;
using NotificationService.NotificationChannels;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Orleans.Configuration;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Sinks.Grafana.Loki;

#pragma warning disable CA1852
#pragma warning disable CA1305
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();
#pragma warning restore CA1305

try
{
    var lokiEndpoint = Environment.GetEnvironmentVariable("LOKI_ENDPOINT");
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog((context, services, configuration) =>
    {
#pragma warning disable CA1305
        var config = configuration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithSpan()
            .WriteTo.Console();
#pragma warning restore CA1305

            if (lokiEndpoint is not null)
            {
                config.WriteTo.GrafanaLoki(lokiEndpoint, labels: new[]
                {
                    new LokiLabel { Key = "service", Value = context.HostingEnvironment.ApplicationName }
                });
            }
    });

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(r => r
            .AddTelemetrySdk()
            .AddService(builder.Environment.ApplicationName)
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = builder.Environment.EnvironmentName
            })
        )
        .WithMetrics(m => m
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter("Microsoft.Orleans", "Orleans")
            .AddMeter("QueueListenerService")
            .AddOtlpExporter()
        )
        .WithTracing(t => t
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation(h =>
            {
                if (lokiEndpoint is not null)
                {
                    h.FilterHttpRequestMessage = (req) => req.RequestUri switch
                    {
                        null => true,
                        { OriginalString: { } os } when os.StartsWith(lokiEndpoint, StringComparison.OrdinalIgnoreCase) => false,
                        _ => true
                    };
                }
            })
            .AddSource("Microsoft.Orleans.Application", "Microsoft.Orleans.Runtime")
            .AddSource("QueueListenerService")
            .AddOtlpExporter()
        );

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddAmazonSqs();
    builder.Services.AddOptions<SqsConfig>()
        .Bind(builder.Configuration.GetSection("Sqs"))
        .ValidateDataAnnotations();
    builder.Services.AddSingleton<NotificationsMessageProcessor>();
    builder.Services.AddSingleton<IVapidService, VapidService>();
    builder.Services.AddHostedService<QueueListenerService<NotificationsMessageProcessor, NotificationsMessageProcessor>>();

    builder.Host.UseOrleans((context, siloBuilder) =>
    {
        siloBuilder
            .UseLocalhostClustering()
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = "dev";
                options.ServiceId = "NotificationService";
            })
            .AddBroadcastChannel("notifications", o => o.FireAndForgetDelivery = false)
            .AddMemoryGrainStorageAsDefault();
    });

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.MapGet("/env", (HttpContext context) => new
    {
        Headers = context.Request.Headers
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
