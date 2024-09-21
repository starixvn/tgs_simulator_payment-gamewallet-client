using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Payment.GameWallet.ApplicationServices;
using Payment.GameWallet.ViewModels;
using Tgs.Logging.Extensions;
using Tgs.ServiceUtility;

namespace Payment.GameWallet.Controllers;

[Route("transaction-verification")]
[ApiController]
public class TransactionVerificationController(ILogger<TransactionVerificationController> logger) : ControllerBase
{
    [HttpPost(Name = "VerifyTransaction")]
    [Consumes("application/json")]
    public async Task<IActionResult> VerifyTransactionAsync(
        [FromServices] GameWalletHasher hasher,
        [FromServices] IGetService<GetWithdrawFormQuery, WithdrawForm?> getService,
        [FromBody] VerifyTransactionViewModel requestData)
    {
        logger.LogInformation("Verify Transaction: {requestData}", JsonSerializer.Serialize(requestData));
        var formId = Guid.Parse(requestData.Reference);
        HttpContext.AddTrackingId(formId);

        var withdrawForm = await getService
            .GetAsync(new(formId), HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (withdrawForm == null)
        {
            logger.LogWarning("Can't found withdraw form({id}).", formId);
            return BadRequest($"Can't found reference {formId}");
        }

        var key = withdrawForm?.ApplyInfo?.Settings?.MerchantSecurityCode
            ?? throw new InvalidOperationException("Can't not get key, please try withdraw again");

        string[] keysHash = [requestData.Merchant, requestData.Reference, requestData.Amount, key];
        var hash = hasher.ComputeMd5(string.Join("", keysHash));

        if (hash != requestData.Sign)
        {
            logger.LogWarning("Sign value invalid. input: {sign}, compute: {hash}", requestData.Sign, hash);
            return BadRequest("Sign value invalid.");
        }

        var actualAmount = decimal.Parse(requestData.Amount).ToString("0.00");
        var orderAmount = decimal.Parse(requestData.Amount).ToString("0.00");

        if ( actualAmount != orderAmount)
        {
            logger.LogWarning("Invalid Amount. FromForm: {orderAmount}, FromRequest: {actualAmount}", orderAmount, actualAmount);
            return BadRequest($"Invalid Amount. FromForm: {orderAmount}, FromRequest: {actualAmount}");
        }

        return Ok();
    }
}
