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
public class WithdrawController : ControllerBase
{
    private readonly ILogger<WithdrawController> _logger;

    public WithdrawController(ILogger<WithdrawController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("callback", Name = "NotifyWithdraw")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<string> WithdrawCallbackAsync(
    [FromServices] GameWalletHasher hasher,
        [FromServices] IGetService<GetWithdrawFormQuery, WithdrawForm?> getService,
        [FromServices] IPaymentServiceClient paymentServiceClient,
        [FromServices] IPaymentClient paymentClient,
        [FromForm] CallbackWithdrawViewModel source)
    {
        _logger.LogInformation("{source}", source);
        var formId = Guid.Parse(source.RefId);

        var withdrawForm = await getService
            .GetAsync(new(formId), HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (withdrawForm == null)
        {
            _logger.LogWarning("Can't found withdraw form({id}).", formId);
            return "fail";
        }

        var data = new string[] { source.InvoiceNo, source.Status.ToString(), source.Currency, source.Amount.ToString(), source.PaymentDate };
        var sign = source.Signature;
        var hash = hasher.ComputeHash(withdrawForm.ApplyInfo!.Settings.ApiKey, data);

        _logger.LogInformation("Compute hash: {hash}, sign: {sign}", hash, sign);

        if (sign != hash)
        {
            _logger.LogWarning("Sign value invalid. input: {sign}, compute: {hash}", sign, hash);
            return "fail";
        }

        var status = source.Status;

        var paymentReturn = new GameWalletWithdrawReturn()
        {
            FormId = formId,
            ActualAmount = source.Amount,
            Sign = sign,
            ThirdPartyOrderNo = source.InvoiceNo,
            Status = source.StatusMessage,
        };

        var fsm = PaymentStateMachineFactory.CreateWithdrawStateMachine(withdrawForm.Status);

        switch (source.StatusMessage)
        {
            case "Success":
                if (fsm.CanFire(PaymentTrigger.PaymentWithdrawCompleted))
                {
                    await fsm.FireAsync(PaymentTrigger.PaymentWithdrawCompleted).ConfigureAwait(false);

                    await paymentServiceClient.SubmitWithdrawFormStatusAsync(
                        withdrawForm.Id,
                        paymentReturn: JsonSerializer.Serialize(
                            paymentReturn,
                            GameWalletUtility.SerializerOptions),
                        actualAmount: source.Amount,
                        cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

                    await paymentClient.PaidWithdrawAsync(
                        withdrawForm.Id,
                        withdrawForm.BrandId).ConfigureAwait(false);
                }

                break;

            case "Reject":
                if (fsm.CanFire(PaymentTrigger.Reject))
                {
                    await fsm.FireAsync(PaymentTrigger.Reject).ConfigureAwait(false);

                    await paymentServiceClient.SubmitWithdrawFormStatusAsync(
                        withdrawForm.Id,
                        paymentReturn: JsonSerializer.Serialize(
                            paymentReturn,
                            GameWalletUtility.SerializerOptions),
                        actualAmount: source.Amount,
                        cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

                    await paymentClient.RejectWithdrawAsync(
                        withdrawForm.Id,
                        withdrawForm.BrandId,
                        HttpContext.RequestAborted).ConfigureAwait(false);
                }

                break;

            default:
                _logger.LogWarning("FormId:{formId}, ThirdPartyOrderNo:{ThirdPartyOrderNo} , payment Status:{status}",
                    formId,
                    paymentReturn.ThirdPartyOrderNo,
                    paymentReturn.Status);
                return "fail";
        }

        return "success";
    }
}
