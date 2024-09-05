using System.Text.Json;
using Microsoft.Extensions.Options;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Payment.GameWallet.Client;
using Valhalla.MessageQueue;

namespace Payment.GameWallet.MessageQueue;

internal class DepositProcessor(
    GameWalletClient gameWalletClient,
    IPaymentClient paymentClient,
    IOptions<GameWalletOptions> optionsAccessor)
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

        var order = new DepositOrder(
            decimal.Parse(data.Amount),
            new Uri(_options.CallbackBaseUrl,"deposit/callback"),
            settings.FormId,
            settings.PlayerId,
            settings.ItemDescription);

        var depositResponse = await gameWalletClient.DepositAsync(
            settings,
            order,
            cancellationToken).ConfigureAwait(false);

        var applyInfo = new GameWalletDepositApplyInfo(
            settings,
            new GameWalletDepositResponse(
                depositResponse.Amount,
                depositResponse.FormId,
                depositResponse.RedirectUrl,
                depositResponse.Transaction,
                depositResponse.Token,
                depositResponse.Currency));

        await paymentClient.ScheduleAsync(
            "tgs.payment.bigpayz.deposit.schedule.reject",
            settings.FormId,
            DateTime.UtcNow.AddMinutes(60D),
            cancellationToken).ConfigureAwait(false);

        return new DepositResponse
        {
            Result = JsonSerializer.Serialize(applyInfo, GameWalletUtility.SerializerOptions)
        };
    }
}
