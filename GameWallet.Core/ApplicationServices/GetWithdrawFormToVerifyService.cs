using System.Text.Json;
using Payment.GameWallet.Client;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;

namespace Payment.GameWallet.ApplicationServices;
internal class GetWithdrawFormToVerifyService(IPaymentServiceClient paymentServiceClient) : IGetService<GetWithdrawFormToVerifyQuery, WithdrawFormBeforeVerify?>
{
    public async ValueTask<WithdrawFormBeforeVerify?> GetAsync(GetWithdrawFormToVerifyQuery query, CancellationToken cancellationToken = default)
    {
        var data = await paymentServiceClient
           .GetWithdrawFormAsync(query.FormId, cancellationToken)
       .ConfigureAwait(false);

        return data == null
            ? null
            : new WithdrawFormBeforeVerify(
                data.Id,
                data.UserId,
                data.UserType,
                data.BrandId,
                data.BrandPaymentId,
                data.Amount,
                data.Status,
                data.BankInfoId,
                string.IsNullOrEmpty(data.ApplyInfo)
        ? null
        : JsonSerializer.Deserialize<GameWalletSettings>(data.ApplyInfo, GameWalletUtility.SerializerOptions),
                data.PaymentReturn);
    }
}
