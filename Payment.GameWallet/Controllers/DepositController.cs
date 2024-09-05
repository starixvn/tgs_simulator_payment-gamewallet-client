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
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<string> DepositCallbackAsync(
        [FromForm] CallbackDepositViewModel source)
    {
        var keysHash = new string[] { source.Transaction, source.Status.ToString(), source.Amount.ToString(), source.CreatedAt };

        var paymentReturn = new GameWalletDepositReturn()
        {
            Status = source.StatusMessage,
            FormId = Guid.Parse(source.ItemId),
            Transaction = source.Transaction,
            RealAmount = source.Amount,
            Sign = source.Signature,
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

        var hash = hasher.ComputeHash(
            depositForm.ApplyInfo!.Settings.ApiKey,
            keysHash);

        logger.LogInformation("Compute hash: {hash}, sign: {sign}", hash, paymentReturn.Sign);

        if (paymentReturn.Sign != hash)
        {
            logger.LogWarning("Sign value invalid. input: {sign}, compute: {hash}", paymentReturn.Sign, hash);
            return "fail";
        }

        var fsm = PaymentStateMachineFactory.CreateDepositStateMachine(depositForm.Status);

        switch (paymentReturn.Status)
        {
            case "Success":
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

            default:
                logger.LogWarning("FormId:{formId}, ThirdPartyOrderNo:{ThirdPartyOrderNo} , payment Status:{status}",
                    formId,
                    paymentReturn.Transaction,
                    paymentReturn.Status);
                return "fail";
        }

        return "success";
    }

    [HttpPost("callback/promptpay", Name = "NotifyDepositPromptPay")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<string> DepositCallbackPromptPayAsync(
        [FromForm] CallBackDepositPromptPayViewModel source)
    {
        var keysHash = new string[] { source.InvoiceNo, source.ReceiverBank, source.ReceiverAccount, source.Currency, source.Amount.ToString(), source.PaymentDate };

        var paymentReturn = new GameWalletDepositReturn()
        {
            Status = source.StatusMessage,
            FormId = Guid.Parse(source.RefId),
            Transaction = source.InvoiceNo,
            RealAmount = source.Amount,
            OrderAmount = source.OriginalAmount,
            Sign = source.Signature,
            ExtendData = JsonSerializer.Serialize(source)
        };

        return await HandleDepositCallBackAsync(paymentReturn, keysHash).ConfigureAwait(false);
    }
}
