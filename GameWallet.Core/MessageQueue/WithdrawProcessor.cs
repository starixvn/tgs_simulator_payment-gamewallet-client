using System.Text.Json;
using Microsoft.Extensions.Options;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Payment.GameWallet.Client;
using Valhalla.MessageQueue;

namespace Payment.GameWallet.MessageQueue;

internal class WithdrawProcessor(
    GameWalletClient bigPayzClient,
    IPaymentClient paymentClient,
    IOptions<GameWalletOptions> optionsAccessor) : IMessageProcessor<WithdrawRequest, WithdrawResponse>
{
    private readonly GameWalletOptions _options = optionsAccessor.Value;

    public async ValueTask<WithdrawResponse> HandleAsync(
        string subject,
        WithdrawRequest data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        var settings = JsonSerializer.Deserialize<GameWalletWithdrawSettings>(data.ApplyInfo, GameWalletUtility.SerializerOptions)!;

        var order = new WithdrawOrder(
            settings.FormId,
            decimal.Parse(data.Amount),
            new Uri(_options.CallbackBaseUrl,"withdraw/callback"),
            settings.ReceiverName,
            settings.BankName,
            settings.BankAccount,
            settings.BankCode,
            settings.PlayerIp,
            settings.CurrencyCode);

        var withdrawResponse = await bigPayzClient.WithdrawAsync(
            settings.MerchantCode,
            settings.ApiKey,
            order,
            cancellationToken).ConfigureAwait(false);

        var applyInfo = new GameWalletWithdrawApplyInfo(
            settings,
            new GameWalletWithdrawResponse(withdrawResponse.FormId, withdrawResponse.ThirdPartyOrderNo, withdrawResponse.MerchantBalance));

        await paymentClient.SaveWithdrawFormThirdPartyOrderNoAsync(
            settings.FormId,
            withdrawResponse.ThirdPartyOrderNo,
            cancellationToken);

        return new WithdrawResponse
        {
            Result = JsonSerializer.Serialize(applyInfo, GameWalletUtility.SerializerOptions)
        };
    }
}
