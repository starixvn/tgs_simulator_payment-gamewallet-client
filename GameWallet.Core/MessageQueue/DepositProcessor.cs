using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payment.GameWallet.Client;
using Tgs.Logging;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Valhalla.MessageQueue;

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
            ["mt"] = settings.Method
        };
        logger.LogInformation("Request send: {Dict}", dict);
        var param = AddSignature(dict, settings, datetime);

        var applyInfo = new GameWalletDepositApplyInfo(
            settings,
            new GameWalletDepositResponse(
                FormId: settings.Reference,
                Amount: decimal.Parse(data.Amount),
                RedirectUrl: new Uri(_options.DepositUrl, $"Home/2.0/Deposit/?parameter={param}")
             ));

        if (settings.BeExpirationTime.HasValue)
        {
            await paymentClient.ScheduleAsync(
            "tgs.payment.gamewallet.deposit.schedule.reject",
            settings.Reference,
            DateTimeOffset.UtcNow.DateTime.AddMinutes(settings.BeExpirationTime.Value),
            cancellationToken).ConfigureAwait(false);
        }

        var response = JsonSerializer.Serialize(applyInfo, GameWalletUtility.SerializerOptions);

        _ = dict.Remove("k");
        LogApi(settings.Reference, JsonSerializer.Serialize(dict), JsonSerializer.Serialize(applyInfo.Response, GameWalletUtility.SerializerOptions));

        return new DepositResponse
        {
            Result = response
        };
    }

    private string AddSignature(Dictionary<string, string> dict,  GameWalletDepositSettings settings, DateTimeOffset datetime)
    {
        string[] content = [dict["m"], dict["r"], dict["c2"], dict["a"], dict["c1"], datetime.ToString("yyyyMMddHHmmss"), settings.MerchantSecurityCode, dict["c3"]];
        var key = hasher.ComputeMd5(string.Join("", content));
        dict["k"] = key;
        var param = hasher.EncodeParam(JsonSerializer.Serialize(dict));
        logger.LogInformation("Param send: {Params}", param);
        return param;
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
            ResponseCode = 200,
            ResponseContent = response,
            ResponseHeaders = "",
            ServerName = Environment.MachineName,
            TrackingId = formId
        };

        tgsLog.LogApi(apiCall);
    }
}
