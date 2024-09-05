using FluentdForward.OpenTelemetry.MessagePack;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.MessageQueue.Nats.Serializers;
using Payment.GameWallet;
using Payment.GameWallet.MessageQueue;
using System;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseSentry();
builder.Logging
    .AddOpenTelemetry(options =>
    {
        options.IncludeFormattedMessage = true;
        options.ParseStateValues = true;
        _ = options.AddFluentdForwardExporter(fluentOptions =>
        {
            var nodeIp = builder.Configuration.GetValue<string>("NODE_IP")!;
            fluentOptions.Host = nodeIp;
            fluentOptions.Tag = builder.Configuration.GetValue<string>("OTEL_SERVICE_NAME")!;
            fluentOptions.UseMessagePack(LogRecordFormatterResolver.GetResolverInstanceWithExtendInfo(
                "MachineName",
                () => Environment.MachineName));
        });
    });


builder.Services
    .AddControllers();

builder.Services
    .AddGameWallet(
        options => builder.Configuration.GetSection("GameWallet").Bind(options),
        clientBuilder => clientBuilder
            .ConfigureHttpClient((sp, http) =>
            {
                var options = sp.GetRequiredService<IOptions<GameWalletOptions>>().Value;

                http.BaseAddress = options.PaymentApiUrl;
            })
            .AddLoggingHandler<GameWalletOptions>())
    .AddPaymentClient()
    .AddPaymentServiceClient(httpBuilder => httpBuilder
        .ConfigureHttpClient((sp, http) =>
        {
            var options = sp.GetRequiredService<IOptions<GameWalletOptions>>().Value;

            http.BaseAddress = options.PaymentServiceBaseUrl;
        }));

builder.Services
    .AddKeyedSingleton(
        "NatQueue",
        (sp, _) => new NatsConnection(NatsOpts.Default with
        {
            Url = builder.Configuration.GetConnectionString("NatQueue")!,
            SerializerRegistry = TgsNatsSerializerRegistration.Default
        }))
        .AddMessageQueue()
        .AddNatsMessageQueue(config => config
        .ConfigureResolveConnection(sp => sp.GetRequiredKeyedService<NatsConnection>("NatQueue"))
        .AddProcessor(
            ProcessorTypes.DepositProcessorType,
            "tgs.payment.gamewallet.deposit",
            "tgs.payment.gamewallet.deposit")
        .AddProcessor(
            ProcessorTypes.WithdrawProcessorType,
            "tgs.payment.gamewallet.withdraw",
            "tgs.payment.gamewallet.withdraw")
        .AddHandler(
            ProcessorTypes.DepositTimeoutCheckHandlerType,
            $"tgs.payment.gamewallet.deposit.schedule.reject",
            $"tgs.payment.gamewallet.deposit.schedule.reject")
        .AddProcessor(
            ProcessorTypes.GetBalanceProcessorType,
            "tgs.payment.gamewallet.balance",
            "tgs.payment.gamewallet.balance"))
    .AddNatsGlobPatternExchange("*");

builder.Services
    .AddProfileViewer()
    .AddHttpLogging(logging =>
    {
        logging.LoggingFields = HttpLoggingFields.All;
        logging.MediaTypeOptions.AddText("application/x-www-form-urlencoded");
        logging.MediaTypeOptions.AddText("text/html");
        logging.MediaTypeOptions.AddText("multipart/form-data");
    })
    .AddHealthChecks()
    .Services
    .AddOpenTelemetry()
    .WithTracing(traceBuilder => traceBuilder
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddEnvironmentVariableDetector())
        .AddSource("Tgs.*")
        .AddSource("TGS.*")
        .AddSource("Valhalla.*")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddProfileViewExporter()
        .AddOtlpExporter())
    .WithMetrics(meterBuiler => meterBuiler
        .AddRuntimeInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter())
    .Services
    .Configure<AspNetCoreTraceInstrumentationOptions>(options => options.Filter =
        httpContext => !httpContext.Request.Path.HasValue
            || !httpContext.Request.Path.StartsWithSegments("/swagger")
            && !httpContext.Request.Path.StartsWithSegments("/healthz")
            && !httpContext.Request.Path.StartsWithSegments("/metrics")
            && !httpContext.Request.Path.StartsWithSegments("/profiler/")
            && !new[] { "html", "ico", "png", "jpg" }.Any(
                ext => httpContext.Request.Path.Value.EndsWith($".{ext}")));

var app = builder.Build();
app.UsePathBase($"{builder.Configuration.GetValue<string>("PathBase")}");
app.UseHttpLogging();
app.MapControllers();
app.MapHealthChecks("/healthz");
app.MapProfileViewer();

app.Use(async (context, next) =>
{
    await next().ConfigureAwait(false);

    var logger = context.RequestServices.GetService<ILoggerFactory>()
        ?.CreateLogger("Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware");

    if (logger?.IsEnabled(LogLevel.Information) == true)
    {
        using var stream = new StreamReader(context.Request.Body);

        _ = await stream.ReadToEndAsync().ConfigureAwait(false);
    }
});
SentrySdk.CaptureMessage($"Started result Tgs-GameWallet at {DateTime.Now} at {Environment.MachineName}");
app.Run();
