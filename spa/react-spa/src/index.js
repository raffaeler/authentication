import React from 'react';
import ReactDOM from 'react-dom/client';
import './index.css';
import App from './App';
import reportWebVitals from './reportWebVitals';

import { AuthProvider } from "react-oidc-context";


const replaceState = (_user: User | void): void => {
  window.history.replaceState(
    {},
    document.title,
    window.location.pathname
  )

  //console.log(_user);
}

const oidcConfig = {
  metadataUrl: "https://local:8443/realms/Demo/.well-known/openid-configuration",
  authority: "https://local:8443/realms/Demo/protocol/openid-connect/auth",
  
  client_id: "ReactApp",
  //client_id: "AspNetMvc",

  redirect_uri: "https://localhost:3443",
  onSigninCallback: replaceState,
  
  loadUserInfo: true,
  //acr_values: "mfa",  // force the request of the OTP (requires the custom flow)
  acr_values: "pwd",    // just ask username/password
};

const root = ReactDOM.createRoot(document.getElementById('root'));
root.render(
  <React.StrictMode>
    <AuthProvider {...oidcConfig}>
      <App />
    </AuthProvider>
  </React.StrictMode>
);

// If you want to start measuring performance in your app, pass a function
// to log results (for example: reportWebVitals(console.log))
// or send to an analytics endpoint. Learn more: https://bit.ly/CRA-vitals
reportWebVitals();



/*
// https://authts.github.io/oidc-client-ts/classes/OidcClientSettingsStore.html
=== List of all the oidcConfig properties ===

acr_values
authority
client_authentication
client_id
client_secret
clockSkewInSeconds
display
extraQueryParams
extraTokenParams
filterProtocolClaims
loadUserInfo
max_age
mergeClaims
metadata
metadataSeed
metadataUrl
post_logout_redirect_uri
prompt
redirect_uri
refreshTokenCredentials
resource
response_mode
response_type
scope
signingKeys
staleStateAgeInSeconds
stateStore
ui_locales
userInfoJwtIssuer
*/
