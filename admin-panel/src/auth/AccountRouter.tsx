import {Route, Routes} from "react-router-dom";
import {UserListPage} from "./pages/UserListPage";
import {UserDetailPage} from "./pages/UserDetailPage";
import {RoleListPage} from "./pages/RoleListPage";
import {RoleDetailPage} from "./pages/RoleDetailPage";
import {ChangePasswordPage} from "./pages/ChangePasswordPage";
import React from "react";
import {configs} from "../config";
import {LoginPage} from "./pages/LoginPage";
import {RegisterPage} from "./pages/RegisterPage";

export const LoginRoute= "/login";
export const RegisterRoute= "/register";

export const UserRoute= "/users";
export const RoleRoute= "/roles";
export const ChangePasswordRoute= "/profile/password";

export function AccountRouter({baseRouter}:{baseRouter:string}) {
    return <Routes>
        <Route path={UserRoute} element={<UserListPage/>}/>
        <Route path={`${UserRoute}/:id`} element={<UserDetailPage baseRouter={baseRouter}/>}/>
        <Route path={RoleRoute} element={<RoleListPage/>}/>
        <Route path={`${RoleRoute}/:name`} element={<RoleDetailPage baseRouter={baseRouter}/>}/>
        <Route path={ChangePasswordRoute} element={<ChangePasswordPage/>}/>
    </Routes>
}

export function NotLoginAccountRouter() {
    return <Routes>
        <Route path={`${LoginRoute}`} element={<LoginPage/>}/>
        <Route path={`${RegisterRoute}`} element={<RegisterPage/>}/>
        <Route path={`/`} element={<LoginPage/>}/>
    </Routes>
}