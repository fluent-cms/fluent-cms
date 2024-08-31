import useSWR from "swr";
import {catchResponse, decodeError, fetcher, swrConfig} from "../../services/util";
import axios from "axios";
import {fullAuthAPIURI} from "../configs";

export  function useUsers(){
    let res = useSWR(fullAuthAPIURI(`/accounts/users`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}

export  function useRoles(){
    let res = useSWR(fullAuthAPIURI(`/accounts/roles`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}

export  function useEntities(){
    let res = useSWR(fullAuthAPIURI(`/schemas?type=entity`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}

export  function useOneUsers(id:string){
    let res = useSWR(fullAuthAPIURI(`/accounts/users/${id}`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}

export function saveUser(formData:any){
    return catchResponse(()=>axios.post(fullAuthAPIURI(`/accounts/users`), formData))
}

export function deleteUser(id:string){
    return catchResponse(()=>axios.delete(fullAuthAPIURI(`/accounts/users/${id}`)))
}
export  function useOneRole(name:string){
    let res = useSWR(  !name? null:fullAuthAPIURI(`/accounts/roles/${name}`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}

export function saveRole(payload:any){
    return catchResponse(()=>axios.post(fullAuthAPIURI(`/accounts/roles`), payload))
}

export function deleteRole(name:string){
    return catchResponse(()=>axios.delete(fullAuthAPIURI(`/accounts/roles/${name}`)))
}