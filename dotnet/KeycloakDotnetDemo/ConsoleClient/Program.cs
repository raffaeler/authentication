using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.ConstrainedExecution;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Security.Policy;
using System.Text.Json;
using System.Threading.Tasks;

using CommonAuth;

using static System.Formats.Asn1.AsnWriter;

namespace ConsoleClient;

internal class Program
{
    static async Task Main(string[] args)
    {
        //await new Program().Start_ClientCredentials();
        //await new Program().Start_PasswordGrant();
        await new Program().Start_ClientCert();
        Console.ReadKey();
    }

    // This demo requires the oAuth "Client Credentials Grant"
    // which is the Keycloack checkbox "Service account roles"
    private async Task Start_ClientCredentials()
    {
        // metadata: https://local:8443/realms/Demo/.well-known/openid-configuration
        using HttpClient ipClient = new(new HttpLogger());
        ipClient.BaseAddress =
            new Uri($"https://kc.iamraf.net:8443/realms/Demo/protocol/openid-connect/");
        //new Uri($"https://local:8443/realms/Demo/protocol/openid-connect/");

        using HttpRequestMessage ipRequest = new(HttpMethod.Post, "token");
        Dictionary<string, string> parameters = new()
        {
            { "grant_type", "client_credentials" },
            { "client_id", "AspNetMvc" },
            { "client_secret", "mNogYsdL07VrDNhlD9QKvLdkKiM6YY6f" },
            //{ "scope", "email" },
        };

        ipRequest.Content = new FormUrlEncodedContent(parameters);
        var ipResponse = await ipClient.SendAsync(ipRequest, HttpCompletionOption.ResponseContentRead);
        ipResponse.EnsureSuccessStatusCode();
        var ipContent = await ipResponse.Content.ReadAsStringAsync();

        var tokenInfo = JsonSerializer.Deserialize<TokenInfo>(ipContent);
        Console.WriteLine(TokenHelpers.GetTokenText(tokenInfo?.Token ?? string.Empty));

        using HttpClient apiClient = new(new HttpLogger());
        apiClient.BaseAddress = new Uri("https://app.iamraf.net:5001/");
        using HttpRequestMessage apiRequest = new(HttpMethod.Get, "api/values/ValuesPlain");
        apiRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenInfo?.Token);
        var apiResponse = await apiClient.SendAsync(apiRequest, HttpCompletionOption.ResponseContentRead);
        apiResponse.EnsureSuccessStatusCode();
        var apiContent = await apiResponse.Content.ReadAsStringAsync();
        Console.WriteLine(apiContent);
    }

    // This demo requires the oAuth "password"
    // which is the Keycloack checkbox "Direct access grants"
    // ------------------------------------------------------------
    // *** This will not work with users who configured the OTP ***
    // ------------------------------------------------------------
    private async Task Start_PasswordGrant()
    {
        // metadata: https://local:8443/realms/Demo/.well-known/openid-configuration
        using HttpClient ipClient = new(new HttpLogger());
        ipClient.BaseAddress =
            new Uri($"https://kc.iamraf.net:8443/realms/Demo/protocol/openid-connect/");
        //new Uri($"https://local:8443/realms/Demo/protocol/openid-connect/");

        using HttpRequestMessage ipRequest = new(HttpMethod.Post, "token");
        Dictionary<string, string> parameters = new()
        {
            { "grant_type", "password" },
            { "client_id", "Unattended" },
            { "username", "raf@disney.com" },
            { "password", "P@ssw0rd2" },
            { "scope", "openid" },
        };

        ipRequest.Content = new FormUrlEncodedContent(parameters);
        var ipResponse = await ipClient.SendAsync(ipRequest, HttpCompletionOption.ResponseContentRead);
        if (!ipResponse.IsSuccessStatusCode)
        {
            var badResponse = await ipResponse.Content.ReadAsStringAsync();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{badResponse}");
            Console.ForegroundColor = ConsoleColor.White;
        }
        ipResponse.EnsureSuccessStatusCode();
        var ipContent = await ipResponse.Content.ReadAsStringAsync();

        var tokenInfo = JsonSerializer.Deserialize<TokenInfo>(ipContent);
        Console.WriteLine(TokenHelpers.GetTokenText(tokenInfo?.Token ?? string.Empty));

        using HttpClient apiClient = new(new HttpLogger());
        apiClient.BaseAddress = new Uri("https://app.iamraf.net:5001/");
        using HttpRequestMessage apiRequest = new(HttpMethod.Get, "api/values/ValuesPlain");
        apiRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenInfo?.Token);
        var apiResponse = await apiClient.SendAsync(apiRequest, HttpCompletionOption.ResponseContentRead);
        if (!apiResponse.IsSuccessStatusCode)
        {
            var badResponse = await apiResponse.Content.ReadAsStringAsync();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{badResponse}");
            Console.ForegroundColor = ConsoleColor.White;
        }
        apiResponse.EnsureSuccessStatusCode();
        var apiContent = await apiResponse.Content.ReadAsStringAsync();
        Console.WriteLine(apiContent);
    }

    private async Task Start_ClientCert()
    {
        var cert = CertificatesHelper.FindByClientAuth(issuerFilter: "Rialdi");

        OidcAuthorizationCodeFlow codeFlow = new(
            metadataUrl: "https://local:8443/realms/Demo/.well-known/openid-configuration",
            clientId: "Unattended",
            redirectUri: "https://app.iamraf.net:5001/",
            clientCertificate: cert);

        codeFlow.EnableVerboseLogging = true;

        var jwtJson = await codeFlow.RequestJwt();
        var tokenInfo = JsonSerializer.Deserialize<TokenInfo>(jwtJson);
        var jwt = tokenInfo?.Token ?? throw new Exception($"Invalid JWT");
        Console.WriteLine(TokenHelpers.GetTokenText(jwt));
        await MakeApiRequest(jwt);
    }

    private async Task MakeApiRequest(string jwt)
    {
        using HttpClient client = new(new HttpLogger());
        client.BaseAddress = new Uri("https://app.iamraf.net:5001/");
        using HttpRequestMessage request = new(HttpMethod.Get, "api/values/ValuesPlain");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine(content);
    }

}
