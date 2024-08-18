import React, { useState } from 'react';
import { InputText } from 'primereact/inputtext';
import { Password } from 'primereact/password';
import { Button } from 'primereact/button';
import { Card } from 'primereact/card';
import { Link } from 'react-router-dom';
import 'primereact/resources/themes/saga-blue/theme.css';
import 'primereact/resources/primereact.min.css';
import 'primeicons/primeicons.css';
import {changePassword, register} from "../services/auth";

export const ChangePasswordPage: React.FC = () => {
    const [oldPassword, setOldPassword] = useState<string>('');
    const [password, setPassword] = useState<string>('');
    const [confirmPassword, setConfirmPassword] = useState<string>('');
    const [success, setSuccess] = useState<boolean>(false);
    const [errors, setErrors] = useState<string[]>([]);

    const submit =async () => {
        if (confirmPassword != password){
            setErrors(["passwords don't match"]);
            return;
        }
        setErrors([]);
        const {err} = await changePassword({oldPassword, password})
        console.log(err);
        if (err){
            if (err.title){
                setErrors(err.title.split('\r\n'));
            }else {
                setErrors(["change password failed"]);
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
            <Card title="Change Password" className="p-shadow-5" style={cardStyle}>
                <div className="p-fluid">
                    {errors.map(error=> (<div className="p-field" key={error}> <span className="p-error">{error}</span> </div>)) }
                    {success ? (
                        <div className="p-field">
                            <span className="p-message ">
                                Changing password succeeded. <Link to="/">Click here to go to Home Page</Link>
                            </span>
                        </div>
                    ) : (<>
                        <div className="p-field">
                            <label htmlFor="oldPassword">Password</label>
                            <Password
                                id="username"
                                value={oldPassword} toggleMask
                                onChange={(e) => setOldPassword(e.target.value)}
                            />
                        </div>
                        <div className="p-field">
                            <label htmlFor="password">New Password</label>
                            <Password
                                id="password"
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                                feedback={false} toggleMask
                            />
                        </div>
                        <div className="p-field">
                            <label htmlFor="confirmPassword">Confirm New Password</label>
                            <Password
                                id="confirmPassword"
                                value={confirmPassword} toggleMask
                                onChange={(e) => setConfirmPassword(e.target.value)}
                                feedback={false}
                            />
                        </div>
                        <Button
                            label="Submit"
                            icon="pi pi-check"
                            onClick={submit}
                            className="p-mt-2"
                        />
                    </>)
                    }
                </div>
            </Card>
        </div>
    );
};