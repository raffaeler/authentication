// Add bellow trusted domains, access tokens will automatically injected to be send to
// trusted domain can also be a path like https://www.myapi.com/users,
// then all subroute like https://www.myapi.com/useers/1 will be authorized to send access_token to.

// Domains used by OIDC server must be also declared here
// eslint-disable-next-line @typescript-eslint/no-unused-vars
const trustedDomains = {
    default: [
        "https://kc.iamraf.net:8443/realms/Demo/protocol/openid-connect/token",
        "https://kc.iamraf.net:8443/realms/Demo/protocol/openid-connect/revoke",
        "https://kc.iamraf.net:8443/realms/Demo/protocol/openid-connect/userinfo",
        "https://kc.iamraf.net:8443/realms/Demo",
        "https://spa.iamraf.net:3443/",
        "https://app.iamraf.net:5001/",
        new RegExp('^(https://[a-zA-Z0-9-]+.iamraf.net/api/)')
    ],
    // config_classic: ['https://demo.duendesoftware.com'],
    // config_without_silent_login: ['https://demo.duendesoftware.com'],
    // config_without_refresh_token: ['https://demo.duendesoftware.com'],
    // config_without_refresh_token_silent_login: ['https://demo.duendesoftware.com'],
    config_google: ['https://oauth2.googleapis.com', 'https://openidconnect.googleapis.com'],
    //config_with_hash: ['https://demo.duendesoftware.com'],
};
