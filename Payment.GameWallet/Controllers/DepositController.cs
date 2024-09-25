using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Payment.GameWallet.ApplicationServices;
using Payment.GameWallet.Client;
using Payment.GameWallet.ViewModels;
using Tgs.Logging;
using Tgs.Payment.Clients;
using Tgs.ServiceUtility;

namespace Payment.GameWallet.Controllers;

[Route("[controller]")]
[ApiController]
public class DepositController(ILogger<DepositController> logger,
    GameWalletHasher hasher,
    IGetService<GetDepositFormQuery,
    DepositForm?> getService,
    IPaymentServiceClient paymentServiceClient,
    IPaymentClient paymentClient
    ) : ControllerBase
{

    [HttpPost("callback", Name = "NotifyDeposit")]
    [Consumes("application/json")]
    public async Task<string> DepositCallbackAsync(
        [FromServices] TgsLogHandler tgsLog,
        [FromBody] CallbackDepositViewModel source)
    {
        var keysHash = new string[] { source.Merchant, source.Reference, source.Customer, source.Amount, source.Currency, source.StatusCode };
        var extendedData = JsonSerializer.Serialize(source);
        logger.LogInformation("Deposit Callback Receive: {Source}", source);

        var paymentReturn = new GameWalletDepositReturn()
        {
            StatusCode = source.StatusCode,
            FormId = Guid.Parse(source.Reference),
            Transaction = source.TransactionId,
            RealAmount = decimal.Parse(source.ActualAmount),
            OrderAmount = decimal.Parse(source.Amount),
            ReceiveAccountName = "GameWallet",
            ReceiveBankAccount = source.Merchant,
            Key = source.Key,
            ExtendData = extendedData
        };

        var response = await HandleDepositCallBackAsync(paymentReturn, keysHash).ConfigureAwait(false);

        source.Key = string.Empty;
        tgsLog.LogApi(HttpContext, source, response, paymentReturn.FormId);
        return response;
    }

    private async Task<string> HandleDepositCallBackAsync(GameWalletDepositReturn paymentReturn, IEnumerable<string> keysHash)
    {
        var formId = paymentReturn.FormId;

        var depositForm = await getService
            .GetAsync(new(formId), HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (depositForm == null)
        {
            logger.LogWarning("Can't found deposit form({Id}).", formId);
            return "fail";
        }

        var hash = hasher.ComputeMd5(string.Join("", keysHash.Append(depositForm!.ApplyInfo!.Settings.MerchantSecurityCode)));

        logger.LogInformation("Compute hash: {Hash}, sign: {Sign}", hash, paymentReturn.Key);

        if (paymentReturn.Key != hash)
        {
            logger.LogWarning("Sign value invalid. input: {Sign}, compute: {Hash}", paymentReturn.Key, hash);
            return "fail";
        }

        var fsm = PaymentStateMachineFactory.CreateDepositStateMachine(depositForm.Status);

        switch (paymentReturn.StatusCode)
        {
            case "006":
                if (fsm.CanFire(PaymentTrigger.PaymentDepositCompleted))
                {
                    await fsm.FireAsync(PaymentTrigger.PaymentDepositCompleted).ConfigureAwait(false);

                    await paymentServiceClient.SubmitDepositFormStatusAsync(
                        depositForm.Id,
                        paymentReturn: JsonSerializer.Serialize(
                            paymentReturn,
                            GameWalletUtility.SerializerOptions),
                        actualAmount: paymentReturn.RealAmount,
                        thirdPartyOrderNo: paymentReturn.Transaction,
                        cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

                    await paymentClient.ApproveDepositAsync(
                        depositForm.Id,
                        depositForm.BrandId,
                        paymentReturn.RealAmount,
                        HttpContext.RequestAborted).ConfigureAwait(false);
                }

                break;
            case "009":
                break;
            case "001":
            case "007":
                if (fsm.CanFire(PaymentTrigger.Reject))
                {
                    await fsm.FireAsync(PaymentTrigger.Reject).ConfigureAwait(false);

                    await paymentServiceClient.SubmitDepositFormStatusAsync(
                        depositForm.Id,
                        paymentReturn: JsonSerializer.Serialize(
                            paymentReturn,
                            GameWalletUtility.SerializerOptions),
                        thirdPartyOrderNo: paymentReturn.Transaction,
                        cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

                    await paymentClient.RejectDepositAsync(
                        depositForm.Id,
                        depositForm.BrandId,
                        HttpContext.RequestAborted).ConfigureAwait(false);
                }

                break;
            default:
                logger.LogWarning("FormId:{FormId}, ThirdPartyOrderNo:{ThirdPartyOrderNo} , payment Status:{Status}",
                    formId,
                    paymentReturn.Transaction,
                    paymentReturn.StatusMessage);
                return "fail";
        }

        return "success";
    }
}
