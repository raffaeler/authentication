using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using System.Linq;
using System.Threading.Tasks;

namespace WebAppMvc.Policies;

/// <summary>
/// This is the handler for the OtpRequirement
/// </summary>
public class OtpRequirementHandler : AuthorizationHandler<OtpRequirement>
{
    private readonly ILogger<OtpRequirementHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private Dictionary<string, int> Acr2Loa = new Dictionary<string, int>
    {
        { "pwd", 1 },
        { "mfa", 2 },
        { "hwk", 3 },
    };

    public OtpRequirementHandler(
        ILogger<OtpRequirementHandler> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OtpRequirement requirement)
    {
        var ctx = _httpContextAccessor.HttpContext;
        var userName = context.User.Identity?.Name ?? "guest";
        if (ctx == null) return Task.CompletedTask;

        // This puts "mfa" or "hwk" in the context
        // the item in the context will be given to the Identity Provider
        // at the next Challenge
        ctx.Items["acr"] = requirement.Name;

        var acrClaim = context.User.Claims.FirstOrDefault(c => c.Type == "acr");
        if(acrClaim != null)
        {
            if(Acr2Loa.TryGetValue(acrClaim.Value, out int currentLoa) &&
                Acr2Loa.TryGetValue(requirement.Name, out int requiredLoa))
            {
                if(currentLoa >= requiredLoa)
                {
                    _logger.LogInformation(
                        $"User {userName} was authenticated with OTP {requirement.Name}");
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }
            }
        }

        _logger.LogInformation(
            $"User {userName} fails the OTP requirement of {requirement.Name}");
        return Task.CompletedTask;
    }

}
