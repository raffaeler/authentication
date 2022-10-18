using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace WebAppMvc.Policies;

public class OtpRequirement : IAuthorizationRequirement
{
}
