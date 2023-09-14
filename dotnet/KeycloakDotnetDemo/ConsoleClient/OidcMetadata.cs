namespace ConsoleClient;

/// <summary>
/// This class represents the most important parameters obtained
/// from the Oidc standard metadata endpont.
/// DO NOT refactor the names (unless you want to create
/// a custom JSON converter to match the json properties)
/// </summary>
public class OidcMetadata
{
    public string Issuer { get; set; } = string.Empty;
    public string Authorization_endpoint { get; set; } = string.Empty;
    public string Token_endpoint { get; set; } = string.Empty;
    public string Introspection_endpoint { get; set; } = string.Empty;
    public string Userinfo_endpoint { get; set; } = string.Empty;
    public string End_session_endpoint { get; set; } = string.Empty;
}
