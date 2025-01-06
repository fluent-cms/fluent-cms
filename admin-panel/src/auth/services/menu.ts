import useSWR from "swr";
import {fullAPIURI} from "../../cms-client/services/configs";
import {fetcher, swrConfig} from "../../services/util";
import { Schema,MenuItem } from "../types/schema";

export function useTopMenuBar (): MenuItem[]{
    const { data} = useSWR<Schema>(fullAPIURI('/schemas/name/top-menu-bar?type=menu'), fetcher, swrConfig)
    return data?.settings?.menu?.menuItems ?? [] as MenuItem[];
}