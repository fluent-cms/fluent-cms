import axios from "axios";
import {catchResponse} from "./util";
import useSWR from "swr";
import {fetcher, swrConfig} from "../../cms-client/services/util";
import {fullAuthAPIURI} from "../config";


export function useUserInfo() {
    return useSWR(fullAuthAPIURI(`/manage/info`), fetcher, swrConfig)
}

export async function login(item:any) {
    return catchResponse(() => axios.post(fullAuthAPIURI(`/login?useCookies=true`), item));
}

export async function register(item:any) {
    return catchResponse(() => axios.post(fullAuthAPIURI(`/register`), item));
}

export async function changePassword(item:any) {
    return catchResponse(() => axios.post(fullAuthAPIURI(`/profile/password`), item));
}
export async function logout() {
    return catchResponse(() => axios.get(fullAuthAPIURI(`/logout`)));
}
