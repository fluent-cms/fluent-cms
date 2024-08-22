import axios from "axios";
import useSWR from "swr";
import {catchResponse, fetcher, swrConfig} from "../../services/util";
import {fullAuthAPIURI} from "./configs";


export function useUserInfo() {
    return useSWR<Profile>(fullAuthAPIURI(`/profile/info`), fetcher, swrConfig)
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
