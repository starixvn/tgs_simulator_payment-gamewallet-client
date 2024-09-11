using System.Text.Json;
using Microsoft.Extensions.Options;
using Payment.GameWallet.Client;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Valhalla.MessageQueue;

namespace Payment.GameWallet.MessageQueue;

internal class WithdrawProcessor(
    GameWalletClient gameWalletClient,
    IPaymentClient paymentClient,
    TimeProvider timeProvider,
    GameWalletHasher hasher,
    IOptions<GameWalletOptions> optionsAccessor) : IMessageProcessor<WithdrawRequest, WithdrawResponse>
{
    private readonly GameWalletOptions _options = optionsAccessor.Value;

    public async ValueTask<WithdrawResponse> HandleAsync(
        string subject,
        WithdrawRequest data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        var preApplyInfo = JsonSerializer.Deserialize<GameWalletWithdrawApplyInfo>(data.ApplyInfo, GameWalletUtility.SerializerOptions)!;
        var settings = preApplyInfo.Settings;
        var amount = decimal.Parse(data.Amount);
        var now = timeProvider.GetUtcNow();

        var dict = new Dictionary<string, string>
        {
            ["Merchant"] = settings.MerchantCode,
            ["Currency"] = settings.CurrencyCode,
            ["Bank"] = settings.BankCode,
            ["Customer"] = settings.Customer,
            ["Reference"] = settings.Reference.ToString(),
            ["Amount"] = amount.ToString("0.00"),
            ["Note"] = "Withdraw for player",
            ["AccountNumber"] = settings.AccountNumber,
            ["AccountName"] = settings.AccountName,
            ["Datetime"] = now.ToString("yyyy-MM-dd HH:mm:sstt"),
            ["CallbackUri"] = new Uri(_options.CallbackBaseUrl, "withdraw/callback").ToString()
        };

        var keysHash = new string[] { dict["Merchant"], dict["Reference"], dict["Customer"], dict["Amount"],
            dict["Currency"], now.ToString("yyyyMMddHHmmss"), settings.MerchantSecurityCode };
        var key = hasher.ComputeMd5(string.Join("", keysHash));
        dict["Key"] = key;

        var withdrawResponse = await gameWalletClient.WithdrawAsync(
            dict,
            cancellationToken).ConfigureAwait(false);

        var applyInfo = new GameWalletWithdrawApplyInfo(
            settings,
            new GameWalletWithdrawResponse(Guid.Parse(withdrawResponse["reference"]!.GetValue<string>()),
                                        withdrawResponse["transactionId"]!.GetValue<string>(),
                                        decimal.Parse(withdrawResponse["actualAmount"]!.GetValue<string>())));

        await paymentClient.SaveWithdrawFormThirdPartyOrderNoAsync(
            applyInfo.Response.FormId,
            applyInfo.Response.ThirdPartyOrderNo,
            cancellationToken);

        return new WithdrawResponse
        {
            Result = JsonSerializer.Serialize(applyInfo, GameWalletUtility.SerializerOptions)
        };
    }
}
