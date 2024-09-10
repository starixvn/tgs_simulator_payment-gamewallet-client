using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Payment.GameWallet.ApplicationServices;
using Payment.GameWallet.ViewModels;
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
        [FromServices] IGetService<GetDepositFormQuery, DepositForm?> getService)
        //[FromBody] VerifyTransactionViewModel requestData)
    {
        //logger.LogInformation("Verify Transaction: {requestData}", JsonSerializer.Serialize(requestData));
        //var formId = Guid.Parse(requestData.Reference);

        //var withdrawForm = await getService
        //    .GetAsync(new(formId), HttpContext.RequestAborted)
        //    .ConfigureAwait(false);

        //if (withdrawForm == null)
        //{
        //    logger.LogWarning("Can't found withdraw form({id}).", formId);
        //    return BadRequest($"Can't found reference {formId}");
        //}

        //string[] keysHash = [requestData.Merchant, requestData.Reference, requestData.Amount, withdrawForm!.ApplyInfo!.Settings.MerchantSecurityCode];
        //var hash = hasher.ComputeMd5(string.Join("", keysHash));

        //if (hash != requestData.Sign)
        //{
        //    logger.LogWarning("Sign value invalid. input: {sign}, compute: {hash}", requestData.Sign, hash);
        //    return BadRequest("Sign value invalid.");
        //}

        //var actualAmount = decimal.Parse(requestData.Amount).ToString("0.00");
        //var orderAmount = decimal.Parse(requestData.Amount).ToString("0.00");

        //if ( actualAmount != orderAmount)
        //{
        //    logger.LogWarning("Invalid Amount. FromForm: {orderAmount}, FromRequest: {actualAmount}", orderAmount, actualAmount);
        //    return BadRequest($"Invalid Amount. FromForm: {orderAmount}, FromRequest: {actualAmount}");
        //}

        return Ok();
    }
}
