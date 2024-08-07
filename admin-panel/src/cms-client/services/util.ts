import axios from "axios";
export const swrConfig = {
    revalidateOnFocus:false
}
export const fetcher = async (url: string) => {
    const res = await axios.get(url)
    return res.data;
}

export async function catchResponse(req: any) {
    try {
        const res  =await req()
        return {data:res.data}
    } catch (err: any) {
        return {error: decodeError(err)}
    }
}

export function decodeError(error: any) {
    if (!error){
        return null
    }
    return  error.response?.data?.title ?? 'An error has occurred. Please try again.';
}