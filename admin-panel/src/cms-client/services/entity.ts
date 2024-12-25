import useSWR from "swr";
import {fullAPIURI} from "./configs";
import axios from "axios";
import {catchResponse, decodeError, fetcher, swrConfig} from "../../services/util";
import {encodeLazyState} from "./lazyState";

export function getLookupData(schemaName: string, query: string){
    return catchResponse(()=>axios.get(fullAPIURI(`/entities/lookup/${schemaName}?query=${encodeURIComponent(query)}`)))
}

export function useLookupData(schemaName: string, query: string) {
    let res = useSWR(fullAPIURI(`/entities/lookup/${schemaName}?query=${encodeURIComponent(query)}`), fetcher, swrConfig);
    return {...res, error: decodeError(res.error)}
}

export function useListData(schemaName: string | undefined, lazyState: any) {
    console.log({lazyState});
    let res = useSWR(fullAPIURI(`/entities/${schemaName}?${encodeLazyState(lazyState)}`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}

export function useItemData(schemaName: any, id: any) {
    let res =  useSWR(fullAPIURI(`/entities/${schemaName}/${id}`), fetcher, swrConfig)
    return {...res, error:decodeError(res.error)}
}

export function useSubPageData(schemaName: any, id:any, field:any, exclude:boolean, lazyState:any ) {
    const lazy = encodeLazyState(lazyState)
    let res = useSWR(schemaName &&id &&field ?  fullAPIURI(`/entities/${schemaName}/${id}/${field}?exclude=${exclude}&${lazy}`):null, fetcher,swrConfig)
    return {...res, error:decodeError(res.error)}
}

export async function updateItem(schemaName:any,item:any){
    return catchResponse(()=>axios.post(fullAPIURI(`/entities/${schemaName}/update`),item))
}

export async function addItem(schemaName:any, item:any){
    return catchResponse(()=>axios.post(fullAPIURI(`/entities/${schemaName}/insert`),item))
}

export async function deleteItem(schemaName:any, item:any){
    return catchResponse(()=>axios.post(fullAPIURI(`/entities/${schemaName}/delete`), item))
}

export async function saveSubPageItems(schemaName: any, id:any, field:any, items:any ) {
    return catchResponse(()=>axios.post( fullAPIURI(`/entities/${schemaName}/${id}/${field}/save`), items))
}

export async function deleteSubPageItems(schemaName:any, id:any, field:any,items: any){
    return catchResponse(()=>axios.post(fullAPIURI(`/entities/${schemaName}/${id}/${field}/delete`), items))
}