import 'primereact/resources/themes/lara-light-blue/theme.css'; //theme
import 'primereact/resources/primereact.min.css'; //core css
import 'primeicons/primeicons.css'; //icons
import 'primeflex/primeflex.css'; // flex
import './App.css';
import {TopMenuBar} from "./cms-client/components/layout/TopMenuBar";
import React from "react";
import {setAPIUrlPrefix} from "./cms-client/configs";
import {configs} from "./config";
import {EntityRouter} from "./cms-client/EntityRouter";
import {Route, Routes} from "react-router-dom";
import {EditorRouter} from "./code-editor/EditorRouter";
import {setVersionAPI} from "./code-editor/configs";

setAPIUrlPrefix(configs.apiURL)
setVersionAPI(configs.logsAPIURL)

function App() {
    const start = <img alt="logo" src="/fluent-cms.png" height="40" className="mr-2"></img>;
    const end = <>Welcome</>
    return (
        <>
            <TopMenuBar start={start} end={end}/>
            <Routes>
                <Route path={configs.entityBaseRouter + '/*'} element={<EntityRouter/>}/>
                <Route path={configs.editorBaseRouter + '/*'} element={<EditorRouter/>}/>
            </Routes>
        </>
    );
}

export default App;
