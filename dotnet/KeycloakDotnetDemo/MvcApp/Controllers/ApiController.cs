using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MvcApp.Controllers;

[Route("[controller]")]
[ApiController]
public class ApiController : ControllerBase
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("Values")]
    public IActionResult GetValues()
    {
        return Ok(new string[] { "One", "Two", "Three" });
    }

}
