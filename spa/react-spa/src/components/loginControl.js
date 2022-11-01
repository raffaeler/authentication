import React from 'react';
import './loginControl.css';
import { useAuth } from "react-oidc-context";


function LoginControl(props) {
    const auth = useAuth();

    const removeSessionStorageOidc = () => {
        var oidcKeys = Object.keys(sessionStorage)
            .filter((key) => key.startsWith('oidc.user'));
        //console.log(oidcKeys);
        oidcKeys.forEach(k => sessionStorage.removeItem(k));
    }

    const login = async () => {
        await auth.signinRedirect();
    }

    const loginMfa = async () => {
        await auth.signinRedirect({
            acr_values: "mfa"
        });
    }

    const loginHwk = async () => {
        await auth.signinRedirect({
            acr_values: "hwk"
        });
    }

    const logout = async () => {
        await auth.removeUser();
        props.onLogout();
    }

    const logoutAndRevoke = async () => {
        await auth.revokeTokens(["access_token", "refresh_token"]);
        await auth.removeUser();
        removeSessionStorageOidc();
        props.onLogout();
    }

    if (auth.isLoading) {
        return <div>Loading...</div>;
    }

    if (auth.error) {
        return <div>Authentication error: {auth.error.message}</div>;
    }

    if (auth.isAuthenticated) {
        return (
            <div className="auth">
                <span className="helloUser">Hello {auth.user?.profile.name}</span>
                <span className="helloUser"><a href="#" onClick={logout}>Log out</a></span>
                <span className="helloUser"><a href="#" onClick={logoutAndRevoke}>Log out and Revoke</a></span>

                {/* <div>Claim sub: {auth.user.profile['sub']}</div> */}
            </div>
        );
    }
    else {
        return (
            <div className="auth">
                <span className="helloUser"><a href="#" onClick={login}>Log in</a></span>
                <span className="helloUser"><a href="#" onClick={loginMfa}>Log in [MFA]</a></span>
                
                {/*
                    The following link is used for the "Super Secret" page
                    The scenario is when using three levels of Step-Up auth which are:
                    - Password (pwd)
                    - TOTP Google Authenticator (mfa)
                    - Hardware FIDO2 key (hwk)
                 */}

                {/* <span className="helloUser"><a href="#" onClick={loginHwk}>Log in [HWK]</a></span> */}
            </div>
        );
    }
}


export default LoginControl;