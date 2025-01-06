import axios from "axios";
import useSWR from "swr";
import {catchResponse, fetcher, swrConfig} from "../../services/util";
import {fullAuthAPIURI} from "../configs";
import { UserDto } from "../types/userDto";
import { ProfileDto } from "../types/profileDto";


export function useUserInfo() {
    return useSWR<UserDto>(fullAuthAPIURI(`/profile/info`), fetcher, swrConfig)
}

export async function login(item:any) {
    return catchResponse(() => axios.post(fullAuthAPIURI(`/login?useCookies=true`), item));
}

export async function register(item:any) {
    return catchResponse(() => axios.post(fullAuthAPIURI(`/register`), item));
}

export async function changePassword(item:ProfileDto) {
    return catchResponse(() => axios.post(fullAuthAPIURI(`/profile/password`), item));
}
export async function logout() {
    return catchResponse(() => axios.get(fullAuthAPIURI(`/logout`)));
}
