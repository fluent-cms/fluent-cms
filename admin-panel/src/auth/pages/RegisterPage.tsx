import React, { useState } from 'react';
import { InputText } from 'primereact/inputtext';
import { Password } from 'primereact/password';
import { Button } from 'primereact/button';
import { Card } from 'primereact/card';
import { Link } from 'react-router-dom';
import 'primereact/resources/themes/saga-blue/theme.css';
import 'primereact/resources/primereact.min.css';
import 'primeicons/primeicons.css';
import {register} from "../services/auth";
import {configs} from "../../config";
import {LoginRoute} from "../AccountRouter";

export const RegisterPage: React.FC = () => {
    const [email, setEmail] = useState<string>('');
    const [password, setPassword] = useState<string>('');
    const [confirmPassword, setConfirmPassword] = useState<string>('');
    const [success, setSuccess] = useState<boolean>(false);
    const [errors, setErrors] = useState<string[]>([]);

    const handleRegister =async () => {
        if (confirmPassword != password){
            setErrors(["passwords don't match"]);
            return;
        }
        setErrors([]);
        // Implement registration logic here
        const {errorDetail:error} = await register({email, password})
        if (error){
            if (error.errors){
                setErrors(Object.values(error.errors).map((x:any)=>x[0]) )
            }else {
                setErrors(["register failed"]);
            }
        }else {
            setSuccess(true);
        }
    };

    const containerStyle: React.CSSProperties = {
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        height: '100vh',
        backgroundColor: '#f5f5f5',
    };

    const cardStyle: React.CSSProperties = {
        width: '300px',
    };

    return (
        <div style={containerStyle}>
            <Card title="Register" className="p-shadow-5" style={cardStyle}>
                <div className="p-fluid">
                    {errors.map(error=> (<div className="p-field"> <span className="p-error">{error}</span> </div>)) }
                    {success ? (
                        <div className="p-field">
                            <span className="p-message ">
                                Registration succeeded. <Link to="/login">Click here to go to login</Link>
                            </span>
                        </div>
                    ) : (<>
                        <div className="p-field">
                            <label htmlFor="username">Email</label>
                            <InputText
                                id="username"
                                value={email}
                                onChange={(e) => setEmail(e.target.value)}
                            />
                        </div>
                        <div className="p-field">
                            <label htmlFor="password">Password</label>
                            <Password toggleMask
                                id="password"
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                                feedback={false}
                            />
                        </div>
                        <div className="p-field">
                            <label htmlFor="confirmPassword">Confirm Password</label>
                            <Password toggleMask
                                id="confirmPassword"
                                value={confirmPassword}
                                onChange={(e) => setConfirmPassword(e.target.value)}
                                feedback={false}
                            />
                        </div>
                        <Button
                            label="Register"
                            icon="pi pi-check"
                            onClick={handleRegister}
                            className="p-mt-2"
                        />
                        <div className="p-mt-3">
                            <Link to={`${configs.authBaseRouter}${LoginRoute}`}>Already have an account? Login</Link>
                        </div>
                    </>)
                    }
                </div>
            </Card>
        </div>
    );
};