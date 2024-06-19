import axios from "axios";
export const swrConfig = {
    revalidateOnFocus:false
}
export const fetcher = (url: string) => axios.get(url).then(res => res.data)

export async function catchResponse(req: any) {
    try {
        const res  =await req()
        return {data:res.data}
    } catch (err: any) {
        return {err: err?.response?.data ?? JSON.stringify(err, null, 2)}
    }
}
