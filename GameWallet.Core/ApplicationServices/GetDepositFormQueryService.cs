using System.Text.Json;
using Tgs.Payment.Clients;
using Payment.GameWallet.Client;
using Tgs.ServiceUtility;

namespace Payment.GameWallet.ApplicationServices;

internal class GetDepositFormQueryService : IGetService<GetDepositFormQuery, DepositForm?>
{
    private readonly IPaymentServiceClient _paymentServiceClient;

    public GetDepositFormQueryService(IPaymentServiceClient paymentServiceClient)
    {
        _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
    }

    public async ValueTask<DepositForm?> GetAsync(GetDepositFormQuery query, CancellationToken cancellationToken = default)
    {
        var data = await _paymentServiceClient
            .GetDepositFormAsync(query.FormId, cancellationToken)
        .ConfigureAwait(false);

        if (data == null)
        {
            return null;
        }

        var applyInfo = string.IsNullOrEmpty(data.ApplyInfo)
                ? null
                : JsonSerializer.Deserialize<GameWalletDepositApplyInfo>(data.ApplyInfo, GameWalletUtility.SerializerOptions);

        return new DepositForm(
                data.Id,
                data.PlayerCode,
                data.BrandId,
                data.BrandPaymentId,
                data.Amount,
                data.Status,
                applyInfo,
                data.PaymentReturn);
    }
}
