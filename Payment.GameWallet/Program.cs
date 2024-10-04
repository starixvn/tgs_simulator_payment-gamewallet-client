using System.Net;
using System.Net.Mime;
using FluentdForward.OpenTelemetry.MessagePack;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Payment.GameWallet;
using Payment.GameWallet.DependencyInjection;
using Payment.GameWallet.MessageQueue;
using Tgs.MessageQueue.Nats.Serializers;

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
builder.Services.AddMemoryCache();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", b =>
        b.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader());
});
builder.Services
    .AddControllers();

builder.Services
    .AddGameWallet(
        options => builder.Configuration.GetSection("GameWallet").Bind(options),
        clientBuilder =>
        {
            _ = clientBuilder.ConfigureHttpClient((sp, http) =>
            {
                var options = sp.GetRequiredService<IOptions<GameWalletOptions>>().Value;

                http.BaseAddress = options.PaymentApiUrl;
            })
            .AddLoggingHandler<GameWalletOptions>();

            var proxy = builder.Configuration.GetValue<string>("Proxy");

            if (proxy != null)
            {
                _ = clientBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    Proxy = new WebProxy(proxy)
                });
            }
        })
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
        (_, _) => new NatsConnection(NatsOpts.Default with
        {
            Url = builder.Configuration.GetConnectionString("NatQueue")!,
            SerializerRegistry = TgsNatsSerializerRegistration.Default
        }))
        .AddTgsLog()
        .AddSingleton<TgsLogHandler>()
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
        logging.MediaTypeOptions.AddText(MediaTypeNames.Application.FormUrlEncoded);
        logging.MediaTypeOptions.AddText(MediaTypeNames.Text.Html);
        logging.MediaTypeOptions.AddText(MediaTypeNames.Multipart.FormData);
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
    .WithMetrics(m => m
        .AddRuntimeInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter())
    .Services
    .Configure<AspNetCoreTraceInstrumentationOptions>(options => options.Filter =
        httpContext =>
        {
            var path = httpContext.Request.Path;
            if (!path.HasValue)
            {
                return true;
            }

            List<string> excludePaths = ["/swagger", "/healthz", "/metrics", "/profiler/"];

            if (excludePaths.Exists(p => path.StartsWithSegments(p)))
            {
                return false;
            }

            List<string> excludeExtensions = ["html", "ico", "png", "jpg"];

            return !excludeExtensions.Exists(ext => path.Value.EndsWith($".{ext}"));
        });

var app = builder.Build();
app.UseCors("AllowAll");
app.UsePathBase($"{builder.Configuration.GetValue<string>("PathBase")}");
app.UseWhen(context => !(context.Request.Path.StartsWithSegments("/healthz") && context.Response.StatusCode == ((int)HttpStatusCode.OK)), a => a.UseHttpLogging());
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

SentrySdk.CaptureMessage($"Started result Tgs-GameWallet at {DateTimeOffset.Now.DateTime} at {Environment.MachineName}");
await app.RunAsync();
