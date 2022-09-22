import React from 'react';
import './loginControl.css';
import { useAuth } from "react-oidc-context";


function LoginControl(props) {
    const auth = useAuth();

    const logout = () => {
        auth.removeUser();
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
                <span ><a href="#" onClick={logout}>Log out</a></span>

                {/* <div>Claim sub: {auth.user.profile['sub']}</div> */}
            </div>
        );
    }
    else {
        return (<div className="auth"><a href="#" onClick={() => void auth.signinRedirect()}>Log in</a></div>);
    }
}


export default LoginControl;