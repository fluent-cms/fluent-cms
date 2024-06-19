import useSWR from "swr";
import {fullAPIURI } from "../configs";
import {fetcher, swrConfig} from "./util";

export function useSchema (schemaName:string|undefined){
    const { data} = useSWR(fullAPIURI(`/schemas/${schemaName}?camel=true`), fetcher, swrConfig)
    return data?.settings?.entity ?? {}
}