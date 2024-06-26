import axios from "axios";

export const fetcher = (url: string) => axios.get(url).then(res => res.data)
export const ApiPath = process.env.NEXT_PUBLIC_API_PATH ??''
export const FilePath = process.env.NEXT_PUBLIC_FILE_PATH ??''

export const fullApiPath = (p)=> {
  p = ApiPath + p;
  return p;
}
export const fullFilePath = (p)=> {
  p = FilePath + p;
  return p;
}