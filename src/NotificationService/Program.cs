using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Sinks.Grafana.Loki;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

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
                        { OriginalString: { } os } when os.StartsWith(lokiEndpoint) => false,
                        _ => true
                    };
                }
            })
            .AddSource("Microsoft.Orleans.Application")
            .AddOtlpExporter()
        );

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    
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