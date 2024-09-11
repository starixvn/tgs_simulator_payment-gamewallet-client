using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tgs.Payment.Clients;
using Payment.GameWallet.ApplicationServices;
using Payment.GameWallet.Client;
using Payment.GameWallet.ViewModels;
using Tgs.ServiceUtility;

namespace Payment.GameWallet.Controllers;

[Route("[controller]")]
[ApiController]
public class DepositController(ILogger<DepositController> logger,
    GameWalletHasher hasher,
    IGetService<GetDepositFormQuery,
    DepositForm?> getService,
    IPaymentServiceClient paymentServiceClient,
    IPaymentClient paymentClient) : ControllerBase
{

    [HttpPost("callback", Name = "NotifyDeposit")]
    [Consumes("application/json")]
    public async Task<string> DepositCallbackAsync(
        [FromBody] CallbackDepositViewModel source)
    {
        logger.LogInformation($"Received Callback. {JsonSerializer.Serialize(source)}");
        var keysHash = new string[] { source.Merchant, source.Reference, source.Customer, source.Amount, source.Currency, source.StatusCode};

        var paymentReturn = new GameWalletDepositReturn()
        {
            StatusCode = source.StatusCode,
            FormId = Guid.Parse(source.Reference),
            Transaction = source.TransactionId,
            RealAmount = decimal.Parse(source.ActualAmount),
            OrderAmount = decimal.Parse(source.Amount),
            Key = source.Key,
            ExtendData = JsonSerializer.Serialize(source)
        };

        return await HandleDepositCallBackAsync(paymentReturn, keysHash).ConfigureAwait(false);
    }

    private async Task<string> HandleDepositCallBackAsync(GameWalletDepositReturn paymentReturn, IEnumerable<string> keysHash)
    {
        var formId = paymentReturn.FormId;

        var depositForm = await getService
            .GetAsync(new(formId), HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (depositForm == null)
        {
            logger.LogWarning("Can't found deposit form({id}).", formId);
            return "fail";
        }

        var hash = hasher.ComputeMd5(string.Join("", keysHash.Append(depositForm!.ApplyInfo!.Settings.MerchantSecurityCode)));

        logger.LogInformation("Compute hash: {hash}, sign: {sign}", hash, paymentReturn.Key);

        if (paymentReturn.Key != hash)
        {
            logger.LogWarning("Sign value invalid. input: {sign}, compute: {hash}", paymentReturn.Key, hash);
            return "fail";
        }

        var fsm = PaymentStateMachineFactory.CreateDepositStateMachine(depositForm.Status);

        switch (paymentReturn.StatusCode)
        {
            case "001":
                if (fsm.CanFire(PaymentTrigger.PaymentDepositCompleted))
                {
                    await fsm.FireAsync(PaymentTrigger.PaymentDepositCompleted).ConfigureAwait(false);

                    await paymentServiceClient.SubmitDepositFormStatusAsync(
                        depositForm.Id,
                        paymentReturn: JsonSerializer.Serialize(
                            paymentReturn,
                            GameWalletUtility.SerializerOptions),
                        actualAmount: paymentReturn.RealAmount,
                        handlingFee: depositForm.Amount - paymentReturn.RealAmount,
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

            default:
                logger.LogWarning("FormId:{formId}, ThirdPartyOrderNo:{ThirdPartyOrderNo} , payment Status:{status}",
                    formId,
                    paymentReturn.Transaction,
                    paymentReturn.StatusMessage);
                return "fail";

            case "007":
                if (fsm.CanFire(PaymentTrigger.Reject))
                {
                    await fsm.FireAsync(PaymentTrigger.Reject).ConfigureAwait(false);

                    await paymentServiceClient.SubmitDepositFormStatusAsync(
                        depositForm.Id,
                        paymentReturn: JsonSerializer.Serialize(
                            paymentReturn,
                            GameWalletUtility.SerializerOptions),
                        cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

                    await paymentClient.RejectDepositAsync(
                        depositForm.Id,
                        depositForm.BrandId,
                        HttpContext.RequestAborted).ConfigureAwait(false);
                }

                break;
        }

        return "success";
    }
}
