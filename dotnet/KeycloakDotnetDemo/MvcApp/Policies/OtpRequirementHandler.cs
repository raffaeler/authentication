using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using System.Linq;
using System.Threading.Tasks;

namespace WebAppMvc.Policies
{
    public class OtpRequirementHandler : AuthorizationHandler<OtpRequirement>
    {
        private readonly ILogger<OtpRequirementHandler> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OtpRequirementHandler(
            ILogger<OtpRequirementHandler> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OtpRequirement requirement)
        {
            var ctx = _httpContextAccessor.HttpContext;
            var userName = context.User.Identity?.Name ?? "guest";
            if (ctx == null) return Task.CompletedTask;

            ctx.Items["acr"] = "mfa";

            var hasAcrOtp = context.User.Claims.Any(c => c.Type == "acr" && c.Value == "mfa");
            if (hasAcrOtp)
            {
                _logger.LogInformation($"User {userName} was authenticated with OTP");
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            _logger.LogInformation($"User {userName} fails the OTP requirement");
            return Task.CompletedTask;
        }

    }
}
