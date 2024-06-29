import 'primereact/resources/themes/lara-light-blue/theme.css'; //theme
import 'primereact/resources/primereact.min.css'; //core css
import 'primeicons/primeicons.css'; //icons
import 'primeflex/primeflex.css'; // flex
import './App.css';
import {TopMenuBar} from "./cms-client/components/layout/TopMenuBar";
import React  from "react";
import {setAPIUrlPrefix, setAssetsBaseURL} from "./cms-client/configs";
import {configs} from "./config";
import {EntityRouter} from "./cms-client/EntityRouter";
import {Navigate, Route, Routes} from "react-router-dom";
import {EditorRouter} from "./code-editor/EditorRouter";
import {setVersionAPI} from "./code-editor/configs";
import axios from "axios";
import {setAuthAPIURL} from "./auth/config";
import {useUserInfo} from "./auth/services/auth";
import {LoginPage} from "./auth/pages/LoginPage";
import {RegisterPage} from "./auth/pages/RegisterPage";
import {Avatar} from "primereact/avatar";
import UserAvatarDropdown from "./auth/components/UserAvatarDropDown";
setAPIUrlPrefix(configs.apiURL)
setVersionAPI(configs.versionAPIURL)
setAssetsBaseURL(configs.assetURL);
setAuthAPIURL(configs.authAPIURL)

axios.defaults.withCredentials = true

function App() {

    var {data:userInfo} = useUserInfo()
    const start = <img alt="logo" src="/fluent-cms.png" height="40" className="mr-2"></img>;
    const end = (
        <div className="flex align-items-center gap-2">
            <UserAvatarDropdown email={userInfo?.email??''}/>
        </div>
    );
    return (
        userInfo? <>
            <TopMenuBar start={start} end={end}/>
            <Routes>
                <Route path={'/entities/*'} element={<EntityRouter/>}/>
                <Route path={'/editor/*'} element={<EditorRouter/>}/>
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
