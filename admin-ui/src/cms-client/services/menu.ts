import useSWR from "swr";
import {fullAPIURI} from "../configs";
import {fetcher, swrConfig} from "./util";

export function useTopMenuBar (){
    const { data} = useSWR(fullAPIURI('/schemas/top-menu-bar'), fetcher, swrConfig)
    return data?.settings?.menus ?? []
}