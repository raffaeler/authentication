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

namespace MvcApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var corsPolicy = "MyCorsPolicy";
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(corsPolicy, policy =>
                {
                    policy
                        .AllowAnyOrigin()
                        //.WithOrigins("https://localhost:3443"/*, Keycloak url */)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            var authServerSection = builder.Configuration.GetSection("AuthServer");
            builder.Services.Configure<AuthServerConfiguration>(authServerSection);
            var authServerConfig = authServerSection.Get<AuthServerConfiguration>();

            // Start authorization config
            // HttpContextAccessor is needed to access HttpContext from the requirement handler
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("Otp",
                    policy => policy.Requirements.Add(new OtpRequirement()));
            });

            builder.Services.AddScoped<IAuthorizationHandler, OtpRequirementHandler>();
            // End authorization config

            // start authentication config

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

                OnRedirectToIdentityProviderForSignOut = ctx => DebugTrace("OnRedirectToIdentityProviderForSignOut"),

                OnAccessDenied = ctx => DebugTrace($"OIDC: OnAccessDenied"),
                OnAuthenticationFailed = ctx => DebugTrace($"OIDC: OnAuthenticationFailed"),
                OnAuthorizationCodeReceived = ctx => DebugTrace($"OIDC: OnAuthorizationCodeReceived"),
                OnMessageReceived = ctx => DebugTrace($"OIDC: OnMessageReceived"),
                OnRemoteSignOut = ctx => DebugTrace($"OIDC: OnRemoteSignOut"),
                OnSignedOutCallbackRedirect = ctx => DebugTrace($"OIDC: OnSignedOutCallbackRedirect"),
                OnTicketReceived = ctx => DebugTrace($"OIDC: OnTicketReceived"),
                OnTokenResponseReceived = ctx => DebugTrace($"OIDC: OnTokenResponseReceived"),
                OnTokenValidated = ctx => DebugTrace($"OIDC: OnTokenValidated"),
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

                    return DebugTrace($"OIDC: OnUserInformationReceived");
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
                    OnChallenge = x => DebugTrace($"JWT: Challenge "),
                    OnMessageReceived = x => DebugTrace($"JWT: OnMessageReceived "),
                    OnAuthenticationFailed = x => DebugTrace($"JWT: {x.Exception.ToString()}"),
                    OnTokenValidated = x => DebugTrace($"JWT: Token has been validated: {x.Result}"),
                    OnForbidden = x => DebugTrace($"JWT: Forbidden"),
                };
                
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ClockSkew = TimeSpan.FromMinutes(5),
                };

                // this should never be disabled
                // it is done in the demo because we use local dns names
                options.TokenValidationParameters.ValidateIssuer = false;
                //options.TokenValidationParameters.ValidateAudience = false;   // use this only to test audience issues
            }
            );


            // end authentication config



            var app = builder.Build();

            app.UseCors(corsPolicy);


            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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


        private static Task DebugTrace(string caller)
        {
            Debug.WriteLine(caller);
            return Task.CompletedTask;
        }
    }
}