import React, { useState } from 'react';
import { InputText } from 'primereact/inputtext';
import { Password } from 'primereact/password';
import { Button } from 'primereact/button';
import { Card } from 'primereact/card';
import 'primereact/resources/themes/saga-blue/theme.css';
import 'primereact/resources/primereact.min.css';
import 'primeicons/primeicons.css';
import {Link} from "react-router-dom";
import {login} from "../services/auth";
import useSWR from "swr";
import {fullAuthAPIURI} from "../config";
import {fetcher, swrConfig} from "../../cms-client/services/util";

export  const LoginPage: React.FC = () => {
    const [email, setEmail] = useState<string>('');
    const [password, setPassword] = useState<string>('');
    const [error, setError] = useState('');
    const {mutate} =  useSWR(fullAuthAPIURI(`/manage/info`), fetcher, swrConfig)

    const handleLogin =async () => {
        const res = await login({email, password})
        if (res.err){
            setError("login failed");
        }else {
            await mutate()
        }
    };

    const containerStyle: React.CSSProperties = {
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        height: '100vh',
        backgroundColor: '#f5f5f5', // Optional: Add a background color
    };


    return (
        <div style={containerStyle}>
            <Card title="Login" className="p-shadow-5" style={{ width: '300px' }}>
                <div className="p-fluid">
                    {error && (
                        <div className="p-field">
                            <span className="p-error">{error}</span>
                        </div>
                    )}
                    <div className="p-field">
                        <label htmlFor="mail">Email</label>
                        <InputText
                            id="email"
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                        />
                    </div>
                    <div className="p-field">&nbsp;</div>
                    <div className="p-field">
                        <label htmlFor="password">Password</label>
                        <Password
                            id="password"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                            feedback={false}
                        />
                    </div>
                    <div className="p-field">&nbsp;</div>
                    <Button
                        label="Login"
                        icon="pi pi-check"
                        onClick={handleLogin}
                        className="p-mt-2"
                    />
                    <div className="p-field">&nbsp;</div>

                    <div className="p-mt-3">
                        <Link to="/register">Don't have an account? Register</Link>
                    </div>
                </div>
            </Card>
        </div>
    );
};