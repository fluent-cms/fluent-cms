import useSWR from "swr";
import {catchResponse, decodeError, fetcher, swrConfig} from "../../services/util";
import axios from "axios";
import {fullAuthAPIURI} from "../configs";
import { UserDto } from "../types/userDto";
import { RoleDto } from "../types/roleDto";

export  function useUsers(){
    let res = useSWR<UserDto[]>(fullAuthAPIURI(`/accounts/users`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}

export  function useRoles(){
    let res = useSWR<string[]>(fullAuthAPIURI(`/accounts/roles`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}

export  function useResource(){
    let res = useSWR<string[]>(fullAuthAPIURI(`/accounts/resources`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}

export  function useSingleUser(id:string){
    let res = useSWR<UserDto>(fullAuthAPIURI(`/accounts/users/${id}`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}

export  function useSingleRole(name:string){
    let res = useSWR<RoleDto>(!name? null:fullAuthAPIURI(`/accounts/roles/${name}`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}

export function saveUser(formData:UserDto){
    return catchResponse(()=>axios.post(fullAuthAPIURI(`/accounts/users`), formData))
}

export function deleteUser(id:string){
    return catchResponse(()=>axios.delete(fullAuthAPIURI(`/accounts/users/${id}`)))
}

export function saveRole(payload:RoleDto){
    return catchResponse(()=>axios.post(fullAuthAPIURI(`/accounts/roles`), payload))
}

export function deleteRole(name:string){
    return catchResponse(()=>axios.delete(fullAuthAPIURI(`/accounts/roles/${name}`)))
}