using System.Diagnostics;

using CommonAuth;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using MvcApp.Models;

namespace MvcApp.Controllers;

[Controller]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IAuthorizationService _authorizationService;
    private readonly AuthServerConfiguration _authServerConfiguration;


    public HomeController(ILogger<HomeController> logger,
        IAuthorizationService authorizationService,
        IOptions<AuthServerConfiguration> authServerConfigurationOption)
    {
        _logger = logger;
        _authorizationService = authorizationService;
        _authServerConfiguration = authServerConfigurationOption.Value;
    }

    public IActionResult Index()
    {
        return View();
    }

    [Authorize(Policy = "mfa")]
    //[Authorize]
    public async Task<IActionResult> Secret()
    {
        // require the OTP imperatively ("mfa" or "hwk")
        /*
        var authResult = await _authorizationService.AuthorizeAsync(
            this.User, "mfa");

        if (!authResult.Succeeded)
        {
            // Calling Challenge invokes the authn providers
            //return Challenge();

            // Calling Forbid() (HTTP 403) needs one the following code:
            // This will invoke the
            // CookieAuthenticationEvents.OnRedirectToAccessDenied event
            return Forbid();

            // Calling Unauthorized() (HTTP 401) cause the client-side error
            //return Unauthorized();
        }
        */
        return View();
    }

    [Authorize(Policy = "hwk")]
    public async Task<IActionResult> SuperSecret()
    {
        return View();
    }


    public IActionResult Login()
    {
        // extension method defined in CommonAuth
        return this.SignIn(_authServerConfiguration);
    }

    public IActionResult Logout()
    {
        // extension method defined in CommonAuth
        return this.SignOut(_authServerConfiguration);
    }

    [HttpPost("signout-remote")]
    public IActionResult SignoutRemote()
    {
        return Ok("ok");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}