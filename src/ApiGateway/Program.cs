using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
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
        var config = configuration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithSpan()
            .WriteTo.Console();

            if (lokiEndpoint is not null)
            {
                config.WriteTo.GrafanaLoki(lokiEndpoint, labels: new[]
                {
                    new LokiLabel { Key = "service", Value = context.HostingEnvironment.ApplicationName }
                });
            }
    });

    builder.Services
        .AddReverseProxy()
        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

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
            .AddOtlpExporter()
        );
    
    var app = builder.Build();
    
    app.MapReverseProxy(p => 
    {
        p.Use((context, next) => 
        {
            return next();
        });
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
