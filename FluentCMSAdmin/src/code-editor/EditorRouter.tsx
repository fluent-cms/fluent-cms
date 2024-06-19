import {Route, Routes,} from "react-router-dom";
import {List} from "./pages/List";
import {Compare} from "./pages/Compare";


export function EditorRouter() {
    return <Routes>
        <Route path={'/:tableName/:recordID'} element={<List/>}> </Route>
        <Route path={'/:tableName/:recordID/compare'} element={<Compare/>}> </Route>
    </Routes>
}