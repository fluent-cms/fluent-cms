import useSWR from "swr";
import {fetcher} from "./util";
import {getVersionAPI} from "../configs";
import qs from "qs";
import axios from "axios";
import {version} from "../types";

export function useVersionList (param:{
    table_name:any,
    record_id :any
    offset?: number,
    limit?:number,
}){
    const { data} = useSWR(getVersionAPI() + "?" + qs.stringify(param), fetcher,  {
        revalidateOnFocus:false
    })
    return data
}

export function useVersionCompare (id: any[] ){
    const { data} = useSWR(getVersionAPI() + "?" + qs.stringify({id},{arrayFormat:"repeat"}), fetcher,  {
        revalidateOnFocus:false
    })
    return data
}

export async function saveVersion(param:{tableName:any, recordID :any}, data :any) {
    param.recordID = +param.recordID
    const res = await axios.post(getVersionAPI(), {param, data})
    return res.data
}

export function appendNextVersion(data : {id:any}[]){
    let last :any
    data?.forEach( (r)=>{
        if (last){
            last['id1'] = r.id
        }
        last = r
    })
}



export function formatCompareData(data:{id:any, createdAt:any, value:any}[]):version[]{
    const empty :version = {
        id:0,
        createdAt:'',
        value:''
    }
    if (!data || data.length == 0){
        return [empty,empty]
    }

    const ret = data.map(({createdAt, id, value}) =>{
        return {
            ...{id, createdAt},
            value: JSON.stringify(JSON.parse(value), undefined,4)
        }
    })

    if (ret.length == 1){
        return [empty, ret[0]]
    }
    return ret
}
