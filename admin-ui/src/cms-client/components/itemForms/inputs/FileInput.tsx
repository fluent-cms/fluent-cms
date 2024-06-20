import React from "react";
import {FileUpload} from "primereact/fileupload";
import {InputText} from "primereact/inputtext";
import {InputPanel} from "./InputPanel";

export function FileInput(props: {
    data: any,
    column: { field: string, header: string },
    register: any
    className: any
    control: any
    id: any
    uploadUrl: any
    previewImage:boolean
    download:boolean
    getFullURL : (arg:string) =>string


}) {
    return <InputPanel  {...props} component={(field: any) => {
        const {uploadUrl} = props
        return <>
            <InputText id={field.name} value={field.value} className={' w-full'}
                       onChange={(e) => field.onChange(e.target.value)}/>
            { field.value && props.previewImage &&  <img src={props.getFullURL(field.value)} alt={''} height={150}/>}
            { field.value && props.download && <a href={props.getFullURL(field.value)}><h4>Download</h4></a> }
            <FileUpload mode={"basic"} auto url={uploadUrl} onUpload={(e) => {
                field.onChange(e.xhr.responseText)
            }} name={'files'}/>
        </>
    }}/>
}