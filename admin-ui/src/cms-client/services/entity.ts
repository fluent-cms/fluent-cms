import useSWR, {useSWRConfig} from "swr";
import {fullAPIURI} from "../configs";
import {lazyStateUtil} from "../utils/lazyState";
import axios from "axios";
import {catchResponse, fetcher, swrConfig} from "./util";


export function useListData(schemaName: string | undefined, lazyState: any) {
    const {data} = useSWR(fullAPIURI(`/entities/${schemaName}?${lazyStateUtil.encode(lazyState)}`), fetcher,swrConfig)
    console.log({data})
    return data
}

export function useItemData(schemaName: any, id: any) {
    const {data} = useSWR(fullAPIURI(`/entities/${schemaName}/${id}`), fetcher, swrConfig)
    return data??{}
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

export function useSubPageData(schemaName: any, id:any, field:any, exclude:boolean, lazyState:any ) {
    const lazy = lazyStateUtil.encode(lazyState)
    return useSWR(schemaName &&id &&field ?  fullAPIURI(`/entities/${schemaName}/${id}/${field}?exclude=${exclude}&${lazy}`):null, fetcher,swrConfig)
}

export async function saveSubPageItems(schemaName: any, id:any, field:any, items:any ) {
    return catchResponse(()=>axios.post( fullAPIURI(`/entities/${schemaName}/${id}/${field}/save`), items))
}

export async function deleteSubPageItems(schemaName:any, id:any, field:any,items: any){
    return catchResponse(()=>axios.post(fullAPIURI(`/entities/${schemaName}/${id}/${field}/delete`), items))
}