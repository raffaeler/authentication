using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.ConstrainedExecution;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using CommonAuth;

namespace ConsoleClient;

internal class OidcAuthorizationCodeFlow
{
    private JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// At the beginning the OIDC metadata is downloaded from the endpoint
    /// </summary>
    private OidcMetadata? _metadata;

    /// <summary>
    /// After a successful call, this holds the authorization code
    /// as for the standard flow in OIDC
    /// </summary>
    private string? _authorizationCode;

    /// <summary>
    /// This is a piece of information that travels from the
    /// resource owner (this code) to KC and the backend app
    /// </summary>
    private string _stateInfo = "state_info";

    public OidcAuthorizationCodeFlow(string metadataUrl,
        string clientId,
        string redirectUri,
        X509Certificate2 clientCertificate)
	{
        _jsonOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
        };
        MetadataUrl = metadataUrl;
        ClientId = clientId;
        RedirectUri = redirectUri;
        ClientCertificate = clientCertificate;
    }

    public bool EnableVerboseLogging { get; set; }

    public string MetadataUrl { get; }
    public string ClientId { get; }
    public string RedirectUri { get; }
    public X509Certificate2 ClientCertificate { get; }

    private HttpClient CreateClient()
    {
        HttpClient client = EnableVerboseLogging
            ? new HttpClient(new HttpLogger())
            : new HttpClient();
        return client;
    }

    private HttpClient CreateClient(HttpMessageHandler innerHandler)
    {
        HttpClient client = EnableVerboseLogging
            ? new HttpClient(new HttpLogger(innerHandler))
            : new HttpClient(innerHandler);
        return client;
    }

    private async Task MakeMetadataRequest()
    {
        using var client = CreateClient();
        var response = await client.GetAsync(MetadataUrl);
        response.EnsureSuccessStatusCode();

        _metadata = await response.Content.ReadFromJsonAsync<OidcMetadata>(_jsonOptions);
        if (_metadata == null)
        {
            throw new Exception($"{nameof(MakeMetadataRequest)}: cannot read JSON content");
        }
    }

    private async Task MakeAuthorizationCodeRequest()
    {
        if (_metadata == null) throw new ArgumentNullException(nameof(_metadata));

        var baseAuthorizationUrl = _metadata.Authorization_endpoint;
        StringBuilder sb = new(baseAuthorizationUrl);
        sb.Append($"?client_id={ClientId}");
        sb.Append($"&response_type=code");
        sb.Append($"&redirect_uri={RedirectUri}");
        sb.Append($"&scope=email");
        sb.Append($"&state={_stateInfo}");
        var authorizationUrl = sb.ToString();

        var handler = new HttpClientHandler();
        handler.ClientCertificateOptions = ClientCertificateOption.Manual;
        handler.SslProtocols = SslProtocols.Tls12;
        handler.ClientCertificates.Add(ClientCertificate);

        using var client = CreateClient(handler);
        using HttpRequestMessage request = new(HttpMethod.Get, authorizationUrl);

        // read the headers only because we need the url request *after* the redirection
        var response = await client.SendAsync(request,
            HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        Uri uriWithCode = response.RequestMessage?.RequestUri
            ?? throw new Exception($"{nameof(MakeAuthorizationCodeRequest)}: invalid request uri after successful request");

        var querystring = uriWithCode.GetComponents(
            UriComponents.Query, UriFormat.Unescaped);

        _authorizationCode = querystring.Split('&')
            .Where(qp => qp.StartsWith("code="))
            .SingleOrDefault()
            ?.Split('=')
            ?.LastOrDefault();
        if (_authorizationCode == null)
        {
            throw new Exception($"Missing authorization code after successful request");
        }
    }

    private HttpRequestMessage PrepareJwtRequest(string accessTokenUrl,
        string clientId, string code, string redirectUri)
    {
        HttpRequestMessage request = new(HttpMethod.Post, accessTokenUrl);
        Dictionary<string, string> parameters = new()
        {
            { "client_id", clientId },
            { "grant_type", "authorization_code" },
            { "code", code },
            { "redirect_uri", redirectUri },
            //{ "scope", "openid" },
        };

        request.Content = new FormUrlEncodedContent(parameters);
        return request;
    }


    private async Task<string> MakeJwtRequest()
    {
        if (_metadata == null) throw new ArgumentNullException(nameof(_metadata));
        if (_authorizationCode == null) throw new ArgumentNullException(nameof(_authorizationCode));

        var accessTokenUrl = _metadata.Token_endpoint;
        using HttpRequestMessage request = PrepareJwtRequest(
            accessTokenUrl, ClientId, _authorizationCode, RedirectUri);
        using var client = CreateClient();

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
        response.EnsureSuccessStatusCode();
        var jwt = await response.Content.ReadAsStringAsync();
        return jwt;
    }

    public async Task<string> RequestJwt()
    {
        await MakeMetadataRequest();
        await MakeAuthorizationCodeRequest();
        var jwt = await MakeJwtRequest();
        return jwt;
    }

}
