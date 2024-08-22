let apiBaseURL = "";
let assetsBaseURL = "";
export function setAPIUrlPrefix(v: string) {
    apiBaseURL = v;
}
export function fullAPIURI (subPath :string){
    return apiBaseURL + subPath
}

export function fileUploadURL (){
    return apiBaseURL + '/files'
}

export function setAssetsBaseURL (baseURL:string){
    assetsBaseURL = baseURL;
}
export function getFullAssetsURL(url: string) {
    return assetsBaseURL + url;
}