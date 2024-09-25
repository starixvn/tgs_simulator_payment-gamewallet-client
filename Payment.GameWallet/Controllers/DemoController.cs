using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tgs.Logging;

namespace Payment.GameWallet.Controllers;

[Route("[controller]")]
[ApiController]
[AllowAnonymous]
public class DemoController : ControllerBase
{
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
