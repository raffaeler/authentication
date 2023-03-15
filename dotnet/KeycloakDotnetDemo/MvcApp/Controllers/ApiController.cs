using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MvcApp.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class ValuesController : ControllerBase
{
    // GET: api/<ApiController>
    [HttpGet]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    //[Authorize(Policy = "pwd")] // this is not needed (default is pwd)
    public IEnumerable<string> ValuesPlain()
    {
        return new string[] { "plain1", "plain2" };
    }

    [HttpGet]
    [Authorize(Policy = "mfa", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public IEnumerable<string> ValuesMfa()
    {
        return new string[] { "totp1", "totp2" };
    }

    [HttpGet]
    [Authorize(Policy = "hwk", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public IEnumerable<string> ValuesHwk()
    {
        return new string[] { "key1", "key2" };
    }


}
