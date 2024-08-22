import 'primereact/resources/themes/lara-light-blue/theme.css'; //theme
import 'primereact/resources/primereact.min.css'; //core css
import 'primeicons/primeicons.css'; //icons
import 'primeflex/primeflex.css'; // flex
import './App.css';
import {TopMenuBar} from "./auth/components/TopMenuBar";
import React  from "react";
import {setAPIUrlPrefix, setAssetsBaseURL} from "./cms-client/services/configs";
import {configs} from "./config";
import {EntityRouter} from "./cms-client/EntityRouter";
import {Navigate, Route, Routes} from "react-router-dom";
import axios from "axios";
import {useUserInfo} from "./auth/services/auth";
import {LoginPage} from "./auth/pages/LoginPage";
import {RegisterPage} from "./auth/pages/RegisterPage";
import UserAvatarDropdown from "./auth/components/UserAvatarDropDown";
import {UserListPage} from "./auth/pages/UserListPage";
import {UserDetailPage} from "./auth/pages/UserDetailPage";
import {ChangePasswordPage} from "./auth/pages/ChangePasswordPage";
import {RoleListPage} from "./auth/pages/RoleListPage";
import {RoleDetailPage} from "./auth/pages/RoleDetailPage";
import {setFullAuthAPIUrl} from "./auth/services/configs";
setAPIUrlPrefix(configs.apiURL)
setAssetsBaseURL(configs.assetURL);
setFullAuthAPIUrl(configs.authAPIURL)

axios.defaults.withCredentials = true

function App() {

    const {data:profile} = useUserInfo()
    const start = <img alt="logo" src="/fluent-cms.png" height="40" className="mr-2"></img>;
    const end = (
        <div className="flex align-items-center gap-2">
            <UserAvatarDropdown email={profile?.email??''}/>
        </div>
    );
    return (
        profile? <>
            <TopMenuBar start={start} end={end} profile={profile}/>
            <Routes>
                <Route path={'/entities/*'} element={<EntityRouter/>}/>
                <Route path={'/users'} element={<UserListPage/>}/>
                <Route path={'/users/:id'} element={<UserDetailPage/>}/>
                <Route path={'/roles'} element={<RoleListPage/>}/>
                <Route path={'/roles/:name'} element={<RoleDetailPage/>}/>
                <Route path={'/profile/password'} element={<ChangePasswordPage/>}/>
            </Routes>
        </>:<>
            <Routes>
                <Route path={'/login'} element={<LoginPage/>}/>
                <Route path={'/register'} element={<RegisterPage/>}/>
                <Route path="/" element={<Navigate to="/login" />} />
            </Routes>
            </>
    );
}
export default App;