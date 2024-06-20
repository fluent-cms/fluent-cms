import {Route, Routes,} from "react-router-dom";
import {DataListPage} from "./containers/DataListPage";
import {DataItemPage} from "./pages/DataItemPage";
import {NewDataItemPage} from "./pages/NewDataItemPage";


export function EntityRouter() {
    return <Routes>
            <Route path={'/:schemaName'} element={<DataListPage/>}> </Route>
            <Route path={'/:schemaName/new'} element={<NewDataItemPage/>}> </Route>
            <Route path={'/:schemaName/:id'} element={<DataItemPage/>}> </Route>
    </Routes>
}