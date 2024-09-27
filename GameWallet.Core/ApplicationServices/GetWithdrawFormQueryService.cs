using System.Text.Json;
using Tgs.Payment.Clients;
using Payment.GameWallet.Client;
using Tgs.ServiceUtility;

namespace Payment.GameWallet.ApplicationServices;

public class GetWithdrawFormQueryService(IPaymentServiceClient paymentServiceClient) : IGetService<GetWithdrawFormQuery, WithdrawForm?>
{
    private readonly IPaymentServiceClient _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));

    public async ValueTask<WithdrawForm?> GetAsync(GetWithdrawFormQuery query, CancellationToken cancellationToken = default)
    {
        var data = await _paymentServiceClient
            .GetWithdrawFormAsync(query.FormId, cancellationToken)
        .ConfigureAwait(false);

        if (data == null)
        {
            return null;
        }

        var applyInfo = string.IsNullOrEmpty(data.ApplyInfo)
        ? null
        : JsonSerializer.Deserialize<GameWalletWithdrawApplyInfo>(data.ApplyInfo, GameWalletUtility.SerializerOptions);

        return new WithdrawForm(
                data.Id,
                data.UserId,
                data.UserType,
                data.BrandId,
                data.BrandPaymentId,
                data.Amount,
                data.Status,
                data.BankInfoId,
                applyInfo,
                data.PaymentReturn);
    }
}
