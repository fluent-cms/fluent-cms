import {Route, Routes} from "react-router-dom";
import {LoginPage} from "./pages/LoginPage";
import {RegisterPage} from "./pages/RegisterPage";

export function AuthRouter() {
    return <Routes>
        <Route path={'/:register'} element={<RegisterPage/>}> </Route>
        <Route path={'/:login'} element={<LoginPage/>}> </Route>
    </Routes>
}