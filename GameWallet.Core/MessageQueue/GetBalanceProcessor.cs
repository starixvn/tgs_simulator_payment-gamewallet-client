using System.Text.Json;
using Tgs.Payment.Protos;
using Payment.GameWallet.Client;
using Valhalla.MessageQueue;

namespace Payment.GameWallet.MessageQueue;

internal class GetBalanceProcessor(
    GameWalletClient bigPayzClient)
    : IMessageProcessor<PaymentBalanceRequest, PaymentBalanceResponse>
{
    public async ValueTask<PaymentBalanceResponse> HandleAsync(
        string subject,
        PaymentBalanceRequest data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        var settings = JsonSerializer.Deserialize<BrandPaymentMerchantInfo>(
            data.BrandPaymentSettings,
            GameWalletUtility.SerializerOptions)!;

        var result = await bigPayzClient.QueryBalanceAsync(
            settings.MerchantCode,
            settings.CurrencyCode,
            settings.ApiKey,
            cancellationToken).ConfigureAwait(false);

        return new PaymentBalanceResponse
        {
            Balance = result
        };
    }
}
