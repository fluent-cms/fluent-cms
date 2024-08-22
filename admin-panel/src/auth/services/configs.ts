let apiBaseURL = "";
export function setFullAuthAPIUrl(v: string) {
    apiBaseURL = v;
}
export function fullAuthAPIURI (subPath :string){
    return apiBaseURL + subPath
}