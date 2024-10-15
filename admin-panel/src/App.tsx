import 'primereact/resources/themes/lara-light-blue/theme.css'; //theme
import 'primereact/resources/primereact.min.css'; //core css
import 'primeicons/primeicons.css'; //icons
import 'primeflex/primeflex.css'; // flex
import './App.css';
import {TopMenuBar} from "./layout/TopMenuBar";
import React  from "react";
import {setAPIUrlPrefix, setAssetsBaseURL} from "./cms-client/services/configs";
import {configs} from "./config";
import {EntityRouter} from "./cms-client/EntityRouter";
import {Route, Routes} from "react-router-dom";
import axios from "axios";
import {useUserInfo} from "./auth/services/auth";
import UserAvatarDropdown from "./layout/UserAvatarDropDown";
import {setFullAuthAPIUrl} from "./auth/configs";
import {AccountRouter, NotLoginAccountRouter} from "./auth/AccountRouter";
setAPIUrlPrefix(configs.apiURL)
setAssetsBaseURL(configs.assetURL);
setFullAuthAPIUrl(configs.authAPIURL)

axios.defaults.withCredentials = true
function App() {

    const {data:profile} = useUserInfo()
    const start = <a href={'/'}><img alt="logo" src={`${configs.adminBaseRouter}/fluent-cms.png`} height="40" className="mr-2"></img></a>;
    const end = (
        <div className="flex align-items-center gap-2">
            <UserAvatarDropdown email={profile?.email??''}/>
        </div>
    );
    return (
        profile? <>
            <TopMenuBar start={start} end={end} profile={profile}/>
            <Routes>
                <Route path={`${configs.entityBaseRouter}/*`} element={<EntityRouter/>}/>
                <Route path={configs.adminBaseRouter} element={<EntityRouter />} />
                <Route path={`${configs.authBaseRouter}/*`} element={<AccountRouter/>}/>
            </Routes>
        </>:<>
            <Routes>
                <Route path={`${configs.authBaseRouter}/*`} element={<NotLoginAccountRouter/>}/>
                <Route path={configs.adminBaseRouter} element={<NotLoginAccountRouter />} />
            </Routes>
            </>
    );
}
export default App;