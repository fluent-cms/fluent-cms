let authPrefix = "";
export function setAuthAPIURL(v: string) {
    authPrefix = v;
}
export function fullAuthAPIURI (subPath :string){
    return authPrefix + subPath
}