using Microsoft.AspNetCore.Mvc;
using Payment.GameWallet.ApplicationServices;
using Payment.GameWallet.ViewModels;
using Tgs.ServiceUtility;

namespace Payment.GameWallet.Controllers;

[Route("[controller]")]
[ApiController]
public class VerifyTransactionController(ILogger<VerifyTransactionController> logger) : ControllerBase
{
    [HttpPost("verify", Name = "VerifyTransaction")]
    [Consumes("application/json")]
    public async Task<IActionResult> VerifyTransactionAsync(
        [FromServices] GameWalletHasher hasher,
        [FromServices] IGetService<GetDepositFormQuery, DepositForm?> getService,
        [FromBody] VerifyTransactionViewModel requestData)
    {
        logger.LogInformation("Verify Transaction: {requestData}", requestData);
        var formId = Guid.Parse(requestData.Reference);

        var withdrawForm = await getService
            .GetAsync(new(formId), HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (withdrawForm == null)
        {
            logger.LogWarning("Can't found withdraw form({id}).", formId);
            return BadRequest();
        }

        string[] keysHash = [requestData.Merchant, requestData.Reference, requestData.Amount, withdrawForm!.ApplyInfo!.Settings.MerchantSecurityCode];
        var hash = hasher.ComputeMd5(string.Join("", keysHash));

        if (hash != requestData.Sign)
        {
            logger.LogWarning("Sign value invalid. input: {sign}, compute: {hash}", requestData.Sign, hash);
            return BadRequest();
        }
        
        if (decimal.Parse(requestData.Amount).ToString("0.##") != withdrawForm.Amount.ToString("0.##"))
        {
            logger.LogWarning("Invalid Amount. FromForm: {orderAmount}, FromRequest: {actualAmount}", withdrawForm.Amount, requestData.Amount);
            return BadRequest();
        }

        return Ok();
    }
}
