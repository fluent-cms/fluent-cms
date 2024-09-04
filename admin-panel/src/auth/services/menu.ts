import useSWR from "swr";
import {fullAPIURI} from "../../cms-client/services/configs";
import {fetcher, swrConfig} from "../../services/util";

export function useTopMenuBar (): MenuItem[]{
    const { data} = useSWR(fullAPIURI('/schemas/name/top-menu-bar?type=menu'), fetcher, swrConfig)
    return data?.settings?.menu?.menuItems ?? [] as MenuItem[];
}