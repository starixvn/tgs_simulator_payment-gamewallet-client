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
public class WithdrawController(ILogger<WithdrawController> logger, ITgsLog tgsLog) : ControllerBase
{
    private readonly ILogger<WithdrawController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    [HttpPost("callback", Name = "NotifyWithdraw")]
    [Consumes("application/json")]
    public async Task<string> WithdrawCallbackAsync(
    [FromServices] GameWalletHasher hasher,
        [FromServices] IGetService<GetWithdrawFormQuery, WithdrawForm?> getService,
        [FromServices] IPaymentServiceClient paymentServiceClient,
        [FromServices] IPaymentClient paymentClient,
        [FromBody] CallbackWithdrawViewModel source)
    {
        var response = await HandleWithdrawCallbackAsync(hasher, getService, paymentServiceClient, paymentClient, source);

        LogApi(Guid.Parse(source.Reference), JsonSerializer.Serialize(source), response);

        return response;
    }

    private async Task<string> HandleWithdrawCallbackAsync(
        GameWalletHasher hasher,
        IGetService<GetWithdrawFormQuery,
        WithdrawForm?> getService,
        IPaymentServiceClient paymentServiceClient,
        IPaymentClient paymentClient,
        CallbackWithdrawViewModel source)
    {
        _logger.LogInformation("Withdraw callback received request: {source}", source);
        var formId = Guid.Parse(source.Reference);

        var withdrawForm = await getService
            .GetAsync(new(formId), HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (withdrawForm == null)
        {
            _logger.LogWarning("Can't found withdraw form({id}).", formId);
            return "fail";
        }

        var key = withdrawForm?.ApplyInfo?.Settings?.MerchantSecurityCode
                  ?? throw new InvalidOperationException("Can't not get key, please try withdraw again");

        var data = new string[] { source.Merchant, source.Reference, source.Customer, source.Amount, source.Currency, source.StatusCode, key };
        var sign = source.Key;
        var hash = hasher.ComputeMd5(string.Join("", data));

        _logger.LogInformation("Compute hash: {hash}, sign: {sign}", hash, sign);

        if (sign != hash)
        {
            _logger.LogWarning("Sign value invalid. input: {sign}, compute: {hash}", sign, hash);
            return "fail";
        }

        var paymentReturn = new GameWalletWithdrawReturn()
        {
            FormId = formId,
            ActualAmount = decimal.Parse(source.ActualAmount),
            Sign = sign,
            ThirdPartyOrderNo = source.TransactionId,
            Status = source.StatusMessage,

        };

        var fsm = PaymentStateMachineFactory.CreateWithdrawStateMachine(withdrawForm.Status);

        switch (source.StatusCode)
        {
            case "000":
                if (fsm.CanFire(PaymentTrigger.PaymentWithdrawCompleted))
                {
                    await fsm.FireAsync(PaymentTrigger.PaymentWithdrawCompleted).ConfigureAwait(false);

                    await paymentServiceClient.SubmitWithdrawFormStatusAsync(
                        withdrawForm.Id,
                        paymentReturn: JsonSerializer.Serialize(
                            paymentReturn,
                            GameWalletUtility.SerializerOptions),
                        actualAmount: decimal.Parse(source.ActualAmount),
                        cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

                    await paymentClient.PaidWithdrawAsync(
                        withdrawForm.Id,
                        withdrawForm.BrandId).ConfigureAwait(false);
                }

                break;

            case "001":
                if (fsm.CanFire(PaymentTrigger.Reject))
                {
                    await fsm.FireAsync(PaymentTrigger.Reject).ConfigureAwait(false);

                    await paymentServiceClient.SubmitWithdrawFormStatusAsync(
                        withdrawForm.Id,
                        paymentReturn: JsonSerializer.Serialize(
                            paymentReturn,
                            GameWalletUtility.SerializerOptions),
                        actualAmount: decimal.Parse(source.ActualAmount),
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

    private void LogApi(Guid formId, string request, string response)
    {
        var ip = HttpContext?.Connection?.RemoteIpAddress;

        var apiCall = new ApiLogEvent
        {
            ExecutionTimeMs = 0,
            Ip = ip == null ? string.Empty : ip.ToString(),
            Method = "POST",
            Path = "/api/withdraw/callback",
            Prefix = "",
            RecordedOn = DateTimeOffset.Now,
            RequestContent = request,
            RequestHeaders = "",
            ResponseCode = 400,
            ResponseContent = response,
            ResponseHeaders = "",
            ServerName = Environment.MachineName,
            TrackingId = formId
        };
        
        tgsLog.LogApi(apiCall);
    }
}
