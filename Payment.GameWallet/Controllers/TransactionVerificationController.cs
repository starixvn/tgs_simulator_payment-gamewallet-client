using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Payment.GameWallet.ApplicationServices;
using Payment.GameWallet.ViewModels;
using Tgs.ServiceUtility;

namespace Payment.GameWallet.Controllers;

[Route("transaction-verification")]
[ApiController]
public class TransactionVerificationController(
    ILogger<TransactionVerificationController> logger, 
    GameWalletHasher hasher, 
    IGetService<GetWithdrawFormQuery, WithdrawForm?> getService) : ControllerBase
{
    [HttpPost(Name = "VerifyTransaction")]
    [Consumes("application/json")]
    public async Task<IActionResult> VerifyTransactionAsync(
        [FromServices] TgsLogHandler tgsLog,
        [FromBody] VerifyTransactionViewModel requestData)
    {
        logger.LogInformation("Verify Transaction: {RequestData}", JsonSerializer.Serialize(requestData));
        var formId = Guid.Parse(requestData.Reference);
        var (isSuccess, message) = await HandleVerifyTransactionAsync(requestData, formId).ConfigureAwait(false);
        requestData.Sign = string.Empty;       
        tgsLog.LogApi(HttpContext, requestData, message, formId);
        return isSuccess ? Ok() : BadRequest(message) ;
    }

    private async Task<(bool isSuccess, string message)> HandleVerifyTransactionAsync(VerifyTransactionViewModel requestData, Guid formId)
    {
        
        var withdrawForm = await getService
                    .GetAsync(new GetWithdrawFormQuery(formId), HttpContext.RequestAborted)
                    .ConfigureAwait(false);

        if (withdrawForm == null)
        {
            logger.LogWarning("Can't found withdraw form({Id}).", formId);
            return (false, $"Can't found reference {formId}");
        }

        var key = withdrawForm.ApplyInfo?.Settings?.MerchantSecurityCode
            ?? throw new InvalidOperationException("Can't not get key, please try withdraw again");

        string[] keysHash = [requestData.Merchant, requestData.Reference, requestData.Amount, key];
        var hash = hasher.ComputeMd5(string.Join("", keysHash));

        if (hash != requestData.Sign)
        {
            logger.LogWarning("Sign value invalid. input: {Sign}, compute: {Hash}", requestData.Sign, hash);
            return (false, "Sign value invalid.");
        }

        var actualAmount = decimal.Parse(requestData.Amount).ToString("0.00");
        var orderAmount = decimal.Parse(requestData.Amount).ToString("0.00");

        if (actualAmount != orderAmount)
        {
            logger.LogWarning("Invalid Amount. FromForm: {OrderAmount}, FromRequest: {ActualAmount}", orderAmount, actualAmount);
            return (false, $"Invalid Amount. FromForm: {orderAmount}, FromRequest: {actualAmount}");
        }

        return (true, string.Empty);
    }
}
