import useSWR from "swr";
import {fullAPIURI} from "./configs";
import axios from "axios";
import {catchResponse, decodeError, fetcher, swrConfig} from "../../services/util";
import {encodeLazyState} from "./lazyState";

export function useListData(schemaName: string | undefined, lazyState: any) {
    let res = useSWR(fullAPIURI(`/entities/${schemaName}?${encodeLazyState(lazyState)}`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}

export function useItemData(schemaName: any, id: any) {
    let res =  useSWR(fullAPIURI(`/entities/${schemaName}/${id}`), fetcher, swrConfig)
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

export function useJunctionData(schemaName: any, id:any, field:any, exclude:boolean, lazyState:any ) {
    const lazy = encodeLazyState(lazyState)
    const url= fullAPIURI(`/entities/junction/${schemaName}/${id}/${field}?exclude=${exclude}&${lazy}`);
    let res = useSWR(schemaName &&id &&field ?  url:null, fetcher,swrConfig)
    return {...res, error:decodeError(res.error)}
}

export async function saveJunctionItems(schemaName: any, id:any, field:any, items:any ) {
    return catchResponse(()=>axios.post( fullAPIURI(`/entities/junction/${schemaName}/${id}/${field}/save`), items))
}

export async function deleteJunctionItems(schemaName:any, id:any, field:any, items: any){
    return catchResponse(()=>axios.post(fullAPIURI(`/entities/junction/${schemaName}/${id}/${field}/delete`), items))
}

export function useCollectionData(schemaName: string, id:any, field:any, lazyState:any ) {
    const lazy = encodeLazyState(lazyState)
    const url =  fullAPIURI(`/entities/collection/${schemaName}/${id}/${field}?${lazy}`);
    let res = useSWR(schemaName &&id &&field  ?url:null, fetcher,swrConfig)
    return {...res, error:decodeError(res.error)}   
}

export async function addCollectionItem(schemaName: any, id:any, field:any, item:any ) {
    return catchResponse(()=>axios.post(fullAPIURI(`/entities/collection/${schemaName}/${id}/${field}/insert`),item))
}

export function getLookupData(schemaName: string, query: string){
    return catchResponse(()=>axios.get(fullAPIURI(`/entities/lookup/${schemaName}?query=${encodeURIComponent(query)}`)))
}

export function useLookupData(schemaName: string, query: string) {
    let res = useSWR(fullAPIURI(`/entities/lookup/${schemaName}?query=${encodeURIComponent(query)}`), fetcher, swrConfig);
    return {...res, error: decodeError(res.error)}
}

