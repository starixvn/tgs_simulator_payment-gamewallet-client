using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payment.GameWallet.Client;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Valhalla.MessageQueue;
using Tgs.Logging;
using System.Net.Http;

namespace Payment.GameWallet.MessageQueue;

internal class DepositProcessor(
    GameWalletHasher hasher,
    IPaymentClient paymentClient,
    TimeProvider timeProvider,
    ILogger<DepositProcessor> logger,
    IOptions<GameWalletOptions> optionsAccessor,
    ITgsLog tgsLog)
    : IMessageProcessor<DepositRequest, DepositResponse>
{
    private readonly GameWalletOptions _options = optionsAccessor.Value;

    public async ValueTask<DepositResponse> HandleAsync(
        string subject,
        DepositRequest data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        var settings = JsonSerializer.Deserialize<GameWalletDepositSettings>(
            data.ApplyInfo,
            GameWalletUtility.SerializerOptions)!;

        var datetime = timeProvider.GetUtcNow();

        var dict = new Dictionary<string, string>
        {
            ["m"] = settings.MerchantCode,
            ["c1"] = settings.CurrencyCode,
            ["b"] = settings.BankCode,
            ["c2"] = settings.Customer,
            ["r"] = settings.Reference.ToString(),
            ["a"] = decimal.Parse(data.Amount).ToString("0.00"),
            ["n"] = "Deposit For Player",
            ["su"] = settings.SuccessUrl,
            ["fu"] = settings.FailUrl,
            ["d"] = datetime.ToString("yyyy-MM-dd HH:mm:sstt"),
            ["c4"] = new Uri(_options.CallbackBaseUrl, "deposit/callback").ToString(),
            ["l"] = settings.Language,
            ["c3"] = settings.ClientIP,
            ["mt"] = settings.Method,
        };
        logger.LogInformation("Request send: {dict}", dict);
        string[] content = [dict["m"], dict["r"], dict["c2"], dict["a"], dict["c1"], datetime.ToString("yyyyMMddHHmmss"), settings.MerchantSecurityCode, dict["c3"]];
        var key = hasher.ComputeMd5(string.Join("", content));
        dict["k"] = key;
        var param = hasher.EncodeParam(JsonSerializer.Serialize(dict));
        logger.LogInformation("Param send: {params}", param);

        var applyInfo = new GameWalletDepositApplyInfo(
            settings,
            new GameWalletDepositResponse(
                FormId: settings.Reference,
                Amount: decimal.Parse(data.Amount),
                RedirectUrl: new Uri(_options.DepositUrl, $"Home/2.0/Deposit/?parameter={param}")
             ));

        if ( settings.BeExpirationTime >= 0) 
        {
            await paymentClient.ScheduleAsync(
            "tgs.payment.gamewallet.deposit.schedule.reject",
            settings.Reference,
            DateTime.UtcNow.AddMinutes(settings.BeExpirationTime),
            cancellationToken).ConfigureAwait(false);
        }
     
        var response = JsonSerializer.Serialize(applyInfo, GameWalletUtility.SerializerOptions);
        LogApi(settings.Reference, JsonSerializer.Serialize(dict), response);

        return new DepositResponse
        {
            Result = response
        };
    }

    private void LogApi(Guid formId, string request, string response)
    {
        var ip = Environment.MachineName;

        var apiCall = new ApiLogEvent
        {
            ExecutionTimeMs = 0,
            Ip = ip,
            Method = "GET",
            Path = "/Home/2.0/Deposit",
            Prefix = _options.DepositUrl.ToString(),
            RecordedOn = DateTimeOffset.Now,
            RequestContent = request,
            RequestHeaders = "",
            ResponseCode = 400,
            ResponseContent = response,
            ResponseHeaders = "",
            ServerName = Environment.MachineName,
            TrackingId = formId
        };
        
        tgsLog.LogApi(apiCall);
    }
}
