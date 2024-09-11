using System.Text.Json;
using Json.More;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Payment.GameWallet.Controllers;

[Route("[controller]")]
[ApiController]
[AllowAnonymous]
public class DemoController : ControllerBase
{
    [HttpPost]
    public async Task<string> LogAsync()
    {
        var rawRequestBody = await new StreamReader(Request.Body).ReadToEndAsync();

        return rawRequestBody;
    }

    [Route("success")]
    [HttpGet]
    public string Success()
    {
        return "OK";
    }
    [Route("failed")]
    [HttpGet]
    public string Failed()
    {
        return "Failed";
    }

}
