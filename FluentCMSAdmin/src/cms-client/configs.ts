let apiBaseURL = ""
export function setAPIUrlPrefix(v: string) {
    apiBaseURL = v
}
export function fullAPIURI (subPath :string){
    return apiBaseURL + subPath
}

export function fileUploadURL (){
    return apiBaseURL + '/files'
}