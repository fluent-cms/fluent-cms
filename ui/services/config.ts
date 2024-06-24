import axios from "axios";

export const fetcher = (url: string) => axios.get(url).then(res => res.data)
export const ApiPath = process.env.NEXT_PUBLIC_API_PATH ??''
export const fullPath = (p)=> {
  p = ApiPath + p;
  return p;
}