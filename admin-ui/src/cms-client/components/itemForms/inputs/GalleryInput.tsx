import React from "react";
import {FileUpload} from "primereact/fileupload";
import {InputText} from "primereact/inputtext";
import {InputPanel} from "./InputPanel";
import {Galleria} from "primereact/galleria";

const responsiveOptions = [
    {
        breakpoint: '991px',
        numVisible: 4
    },
    {
        breakpoint: '767px',
        numVisible: 3
    },
    {
        breakpoint: '575px',
        numVisible: 1
    }
];
const itemTemplate = (item:any) => {
    return <img src={item.itemImageSrc} style={{ width: '100%' }} />
}

const thumbnailTemplate = (item:any) => {
    return <img src={item.thumbnailImageSrc} alt={item.alt} />
}

export function GalleryInput(props: {
    data: any,
    column: { field: string, header: string },
    register: any
    className: any
    control: any
    id: any
    uploadUrl: any
}) {
    return <InputPanel  {...props} component={(field: any) => {
        const {data, column} = props
        const fullURL = data[column.field].toString().split(',')
        const items = fullURL.map((x:any) =>({itemImageSrc:x, thumbnailImageSrc:x}))
        return <>
            <InputText type={'hidden'} id={field.name} value={field.value} className={' w-full'}
                       onChange={(e) => field.onChange(e.target.value)}/>

            <Galleria responsiveOptions={responsiveOptions} numVisible={5}
                      item={itemTemplate}
                      thumbnail={thumbnailTemplate} value={items}/>
            <FileUpload multiple mode={"basic"} auto url={field.value} onUpload={(e) => {
                field.onChange(e.xhr.responseText)
            }} name={'file'}/>
        </>
    }}/>
}