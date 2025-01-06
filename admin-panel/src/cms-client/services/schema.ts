import useSWR from "swr";
import {fullAPIURI } from "./configs";
import {decodeError, fetcher, swrConfig} from "../../services/util";
import { XEntity } from "../types/schemaExt";

export function useSchema (schemaName:string){
    let { data,error,isLoading} = useSWR<XEntity>(fullAPIURI(`/schemas/entity/${schemaName}`), fetcher, swrConfig)
    if (error){
        error = decodeError(error)
    }
    return {data, isLoading, error}
}