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
}) {
    return <InputPanel  {...props} component={(field: any) => {
        const {uploadUrl} = props
        const fullURL = field.value
        return <>
            <InputText id={field.name} value={field.value} className={' w-full'}
                       onChange={(e) => field.onChange(e.target.value)}/>
            { fullURL && props.previewImage &&  <img src={fullURL} alt={''} height={150}/>}
            { fullURL && props.download && <a href={fullURL}><h4>Download</h4></a> }
            <FileUpload mode={"basic"} auto url={uploadUrl} onUpload={(e) => {
                field.onChange(e.xhr.responseText)
            }} name={'file'}/>
        </>
    }}/>
}