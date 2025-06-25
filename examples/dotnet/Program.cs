using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Globalization;
using System.Diagnostics;

// Create ActivitySource for manual tracing
var activitySource = new ActivitySource("RollDice.Manual");

var appBuilder = WebApplication.CreateBuilder(args);

// Build a resource configuration action to set service information.
Action<ResourceBuilder> configureResource = r => r.AddService(
    serviceName: appBuilder.Configuration.GetValue("ServiceName", defaultValue: "otel-test")!,
    serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
    serviceInstanceId: Environment.MachineName);

const string DefaultEndpoint = "http://127.0.0.1:4317";

// Configure OpenTelemetry tracing and metrics with auto-start using the
// AddOpenTelemetry() extension method from the OpenTelemetry.Extensions.Hosting package.

Func<string, Action<OtlpExporterOptions>> configureOtlp = otelType => options =>
{
    var otlpEndpoint = appBuilder.Configuration.GetValue("Otlp:Endpoint", defaultValue: DefaultEndpoint);
    var dtToken = appBuilder.Configuration.GetValue("Otlp:ApiToken", defaultValue: string.Empty);
    // var path = otelType == "metric" ? "/v1/metrics" : "/v1/traces";
    options.Endpoint = new Uri(otlpEndpoint + "/v1/" + otelType);
    options.Protocol = OtlpExportProtocol.HttpProtobuf;
    options.Headers = $"Authorization=Api-Token {dtToken}";
};

appBuilder.Services.AddOpenTelemetry()
    .ConfigureResource(configureResource)
    .WithTracing(builder =>
    {
        builder
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddSource("RollDice.Manual"); // Add your custom ActivitySource

        // Use IConfiguration binding for AspNetCore instrumentation options.
        appBuilder.Services.Configure<AspNetCoreTraceInstrumentationOptions>(
            appBuilder.Configuration.GetSection("AspNetCoreInstrumentation"));

        builder.SetSampler(new AlwaysOnSampler());

        // Add console exporter for debugging
        builder.AddConsoleExporter();
        // builder.AddOtlpExporter(otlpOptions =>
        // {
        //     // Use IConfiguration directly for OTLP exporter endpoint option.
        //     otlpOptions.Endpoint = new Uri(
        //         appBuilder.Configuration.GetValue("Otlp:Endpoint", defaultValue: DefaultEndpoint)!);
        // });
        builder.AddOtlpExporter(configureOtlp("traces"));
    })
    .WithMetrics(builder =>
    {
        builder
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation();

        // Add console exporter for debugging
        builder.AddConsoleExporter();

        // builder.AddOtlpExporter(otlpOptions =>
        // {
        //     // Use IConfiguration directly for OTLP exporter endpoint option.
        //     otlpOptions.Endpoint = new Uri(
        //         appBuilder.Configuration.GetValue("Otlp:Endpoint", defaultValue: DefaultEndpoint)!);
        // });
        builder.AddOtlpExporter(configureOtlp("metrics"));
    });

// Clear default logging providers used by WebApplication host.
appBuilder.Logging.ClearProviders();
appBuilder.Logging.AddConsole();

// Configure OpenTelemetry Logging.
appBuilder.Logging.AddOpenTelemetry(options =>
{
    // See appsettings.json "Logging:OpenTelemetry" section for configuration.
    var resourceBuilder = ResourceBuilder.CreateDefault();
    configureResource(resourceBuilder);
    options.SetResourceBuilder(resourceBuilder);
    options.AddConsoleExporter();
    options.AddOtlpExporter(configureOtlp("logs"));
});

var app = appBuilder.Build();

// Print configuration info for debugging
Console.WriteLine("=== OpenTelemetry Configuration ===");
Console.WriteLine($"Service Name: {appBuilder.Configuration.GetValue("ServiceName", defaultValue: "otel-test")}");
Console.WriteLine($"OTLP Endpoint: {appBuilder.Configuration.GetValue("Otlp:Endpoint", defaultValue: DefaultEndpoint)}");
Console.WriteLine($"Dynatrace Token Present: {!string.IsNullOrEmpty(appBuilder.Configuration.GetValue("Otlp:ApiToken", defaultValue: string.Empty))}");
Console.WriteLine("====================================");

static string HandleRollDice(string? player, ILogger<Program> logger)
{
    // Start a manual activity (span) for tracing
    using var activity = Activity.Current?.Source.StartActivity("HandleRollDice") ??
        new Activity("HandleRollDice").Start();

    var result = RollDice();

    if (string.IsNullOrEmpty(player))
    {
        logger.LogInformation("Anonymous player is rolling the dice: {result}", result);
    }
    else
    {
        logger.LogInformation("{player} is rolling the dice: {result}", player, result);
    }

    // Add custom tags and events to the activity
    activity?.SetTag("player", player ?? "anonymous");
    activity?.SetTag("result", result);
    activity?.AddEvent(new ActivityEvent("RolledDice"));

    return result.ToString(CultureInfo.InvariantCulture);
}

static int RollDice() => Random.Shared.Next(1, 7);

app.MapGet("/rolldice/{player?}", HandleRollDice);

app.Run();
