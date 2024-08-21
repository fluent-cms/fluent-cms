import useSWR from "swr";
import {fullAPIURI} from "../../cms-client/configs";
import {catchResponse, decodeError, fetcher, swrConfig} from "../../cms-client/services/util";
import axios from "axios";

export  function useUsers(){
    let res = useSWR(fullAPIURI(`/accounts/users`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}

export  function useRoles(){
    let res = useSWR(fullAPIURI(`/accounts/roles`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}

export  function useEntities(){
    let res = useSWR(fullAPIURI(`/schemas?type=entity`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}

export  function useOneUsers(id:string){
    let res = useSWR(fullAPIURI(`/accounts/users/${id}`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}

export function saveUser(formData:any){
    return catchResponse(()=>axios.post(fullAPIURI(`/accounts/users`), formData))
}

export function deleteUser(id:string){
    return catchResponse(()=>axios.delete(fullAPIURI(`/accounts/users/${id}`)))
}
export  function useOneRole(name:string){
    let res = useSWR(  !name? null:fullAPIURI(`/accounts/roles/${name}`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}

export function saveRole(payload:any){
    return catchResponse(()=>axios.post(fullAPIURI(`/accounts/roles`), payload))
}

export function deleteRole(name:string){
    return catchResponse(()=>axios.delete(fullAPIURI(`/accounts/roles/${name}`)))
}