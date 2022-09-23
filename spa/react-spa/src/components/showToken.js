import React from 'react';
import { useAuth } from "react-oidc-context";
import { JSONTree } from 'react-json-tree';


function ShowToken() {
    const auth = useAuth();

    function parseJwt (token) {
        if(!token) return "";
        var base64Url = token.split('.')[1];
        var base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        var jsonPayload = decodeURIComponent(window.atob(base64).split('').map(function(c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));
    
        return JSON.parse(jsonPayload);
    };

    const theme = {
        scheme: 'monokai',
        author: 'wimer hazenberg (http://www.monokai.nl)',
        base00: '#272822',
        base01: '#383830',
        base02: '#49483e',
        base03: '#75715e',
        base04: '#a59f85',
        base05: '#f8f8f2',
        base06: '#f5f4f1',
        base07: '#f9f8f5',
        base08: '#f92672',
        base09: '#fd971f',
        base0A: '#f4bf75',
        base0B: '#a6e22e',
        base0C: '#a1efe4',
        base0D: '#66d9ef',
        base0E: '#ae81ff',
        base0F: '#cc6633',
      };

    const shouldExpandId = (keyPath, data, level) =>  true;
    const shouldExpandAccess = (keyPath, data, level) =>  false;
    const shouldExpandRefresh = (keyPath, data, level) =>  false;

    let idToken = parseJwt(auth.user?.id_token);
    let accessToken = parseJwt(auth.user?.access_token);
    let refreshToken = parseJwt(auth.user?.refresh_token);

    return (
    <>
        <JSONTree
            labelRenderer={([key]) => <strong>{key==="root" ?  "ID Token" : key}</strong>}
            data={idToken}
            shouldExpandNode={shouldExpandId}
            theme={theme}
            invertTheme={true} />

        <JSONTree
            labelRenderer={([key]) => <strong>{key==="root" ?  "Access Token" : key}</strong>}
            data={accessToken}
            shouldExpandNode={shouldExpandAccess}
            theme={theme}
            invertTheme={true} />

        <JSONTree
            labelRenderer={([key]) => <strong>{key==="root" ?  "Refresh Token" : key}</strong>}
            data={refreshToken}
            shouldExpandNode={shouldExpandRefresh}
            theme={theme}
            invertTheme={true} />
    </>)
}

export default ShowToken;