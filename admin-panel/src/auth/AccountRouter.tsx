import {Route, Routes} from "react-router-dom";
import {UserListPage} from "./pages/UserListPage";
import {UserDetailPage} from "./pages/UserDetailPage";
import {RoleListPage} from "./pages/RoleListPage";
import {RoleDetailPage} from "./pages/RoleDetailPage";
import {ChangePasswordPage} from "./pages/ChangePasswordPage";
import React from "react";

export const LoginRoute= "/login";
export const RegisterRoute= "/register";

export const UserRoute= "/users";
export const RoleRoute= "/roles";
export const ChangePasswordRoute= "/profile/password";

export function AccountRouter() {
    return <Routes>
        <Route path={UserRoute} element={<UserListPage/>}/>
        <Route path={`${UserRoute}/:id`} element={<UserDetailPage/>}/>
        <Route path={RoleRoute} element={<RoleListPage/>}/>
        <Route path={`${RoleRoute}/:name`} element={<RoleDetailPage/>}/>
        <Route path={ChangePasswordRoute} element={<ChangePasswordPage/>}/>
    </Routes>
}