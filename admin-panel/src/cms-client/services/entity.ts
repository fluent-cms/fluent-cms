import useSWR from "swr";
import {fullAPIURI} from "./configs";
import axios from "axios";
import {catchResponse, decodeError, fetcher, swrConfig} from "../../services/util";
import {encodeLazyState} from "./lazyState";
import { LookupListResponse } from "../types/lookupListResponse";
import { ListResponse } from "../types/listResponse";

export function useListData(schemaName: string | undefined, lazyState: any) {
    let res = useSWR<ListResponse>(fullAPIURI(`/entities/${schemaName}?${encodeLazyState(lazyState)}`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}

export function useTreeData(schemaName: string | undefined ) {
    let res = useSWR<any[]>(fullAPIURI(`/entities/tree/${schemaName}`), fetcher,swrConfig);
    return {...res, error:decodeError(res.error)}
}
export function useItemData(schemaName: string, id: any) {
    let res =  useSWR(fullAPIURI(`/entities/${schemaName}/${id}`), fetcher, swrConfig)
    return {...res, error:decodeError(res.error)}
}

export async function updateItem(schemaName:string,item:any){
    return catchResponse(()=>axios.post(fullAPIURI(`/entities/${schemaName}/update`),item))
}

export async function addItem(schemaName:string, item:any){
    return catchResponse(()=>axios.post(fullAPIURI(`/entities/${schemaName}/insert`),item))
}

export async function deleteItem(schemaName:string, item:any){
    return catchResponse(()=>axios.post(fullAPIURI(`/entities/${schemaName}/delete`), item))
}

export function useJunctionIds(schemaName: string, id: any, field:string) {
    const url= fullAPIURI(`/entities/junction/target_ids/${schemaName}/${id}/${field}`);
    let res = useSWR<any[]>(schemaName &&id &&field ?  url:null, fetcher,swrConfig)
    return {...res, error:decodeError(res.error)}
}
export function useJunctionData(schemaName: string, id:any, field:string, exclude:boolean, lazyState:any ) {
    const lazy = encodeLazyState(lazyState)
    const url= fullAPIURI(`/entities/junction/${schemaName}/${id}/${field}?exclude=${exclude}&${lazy}`);
    let res = useSWR<ListResponse>(schemaName &&id &&field ?  url:null, fetcher,swrConfig)
    return {...res, error:decodeError(res.error)}
}

export async function saveJunctionItems(schemaName: string, id:any, field:string, items:any ) {
    return catchResponse(()=>axios.post( fullAPIURI(`/entities/junction/${schemaName}/${id}/${field}/save`), items))
}

export async function deleteJunctionItems(schemaName:string, id:any, field:string, items: any){
    return catchResponse(()=>axios.post(fullAPIURI(`/entities/junction/${schemaName}/${id}/${field}/delete`), items))
}

export function useCollectionData(schemaName: string, id:any, field:string, lazyState:any ) {
    const lazy = encodeLazyState(lazyState)
    const url =  fullAPIURI(`/entities/collection/${schemaName}/${id}/${field}?${lazy}`);
    let res = useSWR(schemaName &&id &&field  ?url:null, fetcher,swrConfig)
    return {...res, error:decodeError(res.error)}   
}

export async function addCollectionItem(schemaName: string, id:any, field:string, item:any ) {
    return catchResponse(()=>axios.post(fullAPIURI(`/entities/collection/${schemaName}/${id}/${field}/insert`),item))
}

export function getLookupData(schemaName: string, query: string){
    return catchResponse(()=>axios.get<LookupListResponse>(fullAPIURI(`/entities/lookup/${schemaName}?query=${encodeURIComponent(query)}`)))
}

export function useLookupData(schemaName: string, query: string) {
    let res = useSWR<LookupListResponse>(fullAPIURI(`/entities/lookup/${schemaName}?query=${encodeURIComponent(query)}`), fetcher, swrConfig);
    return {...res, error: decodeError(res.error)}
}

