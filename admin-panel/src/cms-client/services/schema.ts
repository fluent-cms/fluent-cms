import useSWR from "swr";
import {fullAPIURI } from "./configs";
import {decodeError, fetcher, swrConfig} from "../../services/util";

export function useSchema (schemaName:string){
    let { data,error,isLoading} = useSWR(fullAPIURI(`/schemas/${schemaName}`), fetcher, swrConfig)
    if (data){
        data = data.settings.entity;
    }
    if (error){
        error = decodeError(error)
    }
    return {data, isLoading, error}
}