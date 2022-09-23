import React from 'react';
import './loginControl.css';
import { useAuth } from "react-oidc-context";


function LoginControl(props) {
    const auth = useAuth();

    const login = async () => {
        await auth.signinRedirect();
    }

    const loginMfa = async () => {
        await auth.signinRedirect({
            acr_values: "mfa"
        });
    }

    const logout = async () => {
        await auth.removeUser();
        props.onLogout();
    }

    const logoutAndRevoke = async () => {
        await auth.revokeTokens(["access_token", "refresh_token"])
        await auth.removeUser();
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
            </div>
        );
    }
}


export default LoginControl;