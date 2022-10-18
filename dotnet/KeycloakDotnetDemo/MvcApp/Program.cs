using System.Diagnostics;

using CommonAuth;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;

using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using WebAppMvc.Policies;

namespace MvcApp;

public class Program
{
    public static void Main(string[] args)
    {
        var corsPolicy = "MyCorsPolicy";
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllersWithViews();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(corsPolicy, policy =>
            {
                policy
                    //.AllowAnyOrigin()
                    .WithOrigins("https://localhost:3443", "https://local:3443",
                                "https://spa.iamraf.net:3443", "http://spa.iamraf.net:3000")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        var authServerSection = builder.Configuration.GetSection("AuthServer");
        builder.Services.Configure<AuthServerConfiguration>(authServerSection);
        var authServerConfig = authServerSection.Get<AuthServerConfiguration>();

        // === Start authorization config ===
        // HttpContextAccessor is needed to access HttpContext from the requirement handler
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("Otp",
                policy => policy.Requirements.Add(new OtpRequirement()));
        });

        builder.Services.AddScoped<IAuthorizationHandler, OtpRequirementHandler>();
        // === End authorization config ===

        // === start authentication config ===
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;

            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddKeycloak(authServerConfig, new OpenIdConnectEvents()
        {
            OnRedirectToIdentityProvider = ctx =>
            {
                // specify additional parameters here
                if (ctx.HttpContext.Items.TryGetValue("acr", out object? value))
                {
                    var acr = value as string ?? string.Empty;  // "mfa"
                    ctx.ProtocolMessage.SetParameter("acr_values", acr);
                }

                return Task.CompletedTask;
            },

            OnRedirectToIdentityProviderForSignOut = ctx => Dbg("OnRedirectToIdentityProviderForSignOut"),

            OnAccessDenied = ctx => Dbg($"OIDC: OnAccessDenied"),
            OnAuthenticationFailed = ctx => Dbg($"OIDC: OnAuthenticationFailed"),
            OnAuthorizationCodeReceived = ctx => Dbg($"OIDC: OnAuthorizationCodeReceived"),
            OnMessageReceived = ctx => Dbg($"OIDC: OnMessageReceived"),
            OnRemoteSignOut = ctx => Dbg($"OIDC: OnRemoteSignOut"),
            OnSignedOutCallbackRedirect = ctx => Dbg($"OIDC: OnSignedOutCallbackRedirect"),
            OnTicketReceived = ctx => Dbg($"OIDC: OnTicketReceived"),
            OnTokenResponseReceived = ctx => Dbg($"OIDC: OnTokenResponseReceived"),
            OnTokenValidated = ctx => Dbg($"OIDC: OnTokenValidated"),
            OnUserInformationReceived = ctx =>
            {
                //var tokens = ctx.Properties.GetTokens().ToList();
                //var access_token = tokens.FirstOrDefault(t => t.Name == "access_token");
                //var id_token = tokens.FirstOrDefault(t => t.Name == "id_token");
                //if (access_token != null && id_token != null)
                //{
                //    tokens.Remove(id_token);
                //    ctx.Properties.StoreTokens(tokens);
                //}

                return Dbg($"OIDC: OnUserInformationReceived");
            },

            OnRemoteFailure = ctx =>
            {
                // apparently this OIDC client does not handle 'error_uri'

                // ctx.Failure contains the error message
                // we should redirect to a page that shows the message

                //ctx.Response.Redirect("/");
                //ctx.HandleResponse();
                return Task.CompletedTask;
            }

        })
        .AddJwtBearer(options =>
        {
            options.MetadataAddress = authServerConfig.MetadataAddress;
            options.RequireHttpsMetadata = false;
            options.Audience = "AspNetMvc";
            options.Authority = authServerConfig.Authority;
            options.Events = new JwtBearerEvents()
            {
                OnChallenge = x => Dbg($"JWT: Challenge "),
                OnMessageReceived = x => Dbg($"JWT: OnMessageReceived "),
                OnAuthenticationFailed = x => Dbg($"JWT: {x.Exception.ToString()}"),
                OnTokenValidated = x => Dbg($"JWT: Token has been validated: {x.Result}"),
                OnForbidden = x => Dbg($"JWT: Forbidden"),
            };
            
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ClockSkew = TimeSpan.FromMinutes(5),
            };

            // this should never be disabled
            // it is done in the demo because we use:
            // - https://host.docker.internal:8443 from this project
            // - https://kc.iamraf.net:8443 when demo-ed by raf using a custom DNS
            // - https://local:8443 from the SPA client project
            options.TokenValidationParameters.ValidateIssuer = false;
            //options.TokenValidationParameters.ValidateAudience = false;   // use this only to test audience issues
        });
        // === end authentication config ===

        var app = builder.Build();

#if DEBUG
        // ===================================
        // === Don't do this in production ===
        // ===================================
        // The following middleware instructs the browser not to remove
        // the 'WWW-Authenticate' header from the response.
        // This header is "security-sensible" in calls made
        // from other domains (when using CORS)
        // In case of errors, the "WWW-Authenticate" header contains
        // important error message which we show in the web page
        // ===================================
        // Alternatively, you can see this error by making
        // a non-CORS call using Fiddler/Postman using these parameters:
        // Headers:
        //     Authorization: Bearer (an access_token)
        //     Content-Type: application/json
        // Call:
        //     GET, https://localhost:5001/api/values
        // ===================================
        app.Use((context, next) =>
        {
            context.Response.Headers.Add("Access-Control-Expose-Headers", "WWW-Authenticate");
            return next.Invoke();
        });
#endif

        app.UseCors(corsPolicy);

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseAuthentication();    // Add the default ASP.NET Core Authentication Service
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }


    private static Task Dbg(string caller)
    {
        Debug.WriteLine(caller);
        return Task.CompletedTask;
    }
}