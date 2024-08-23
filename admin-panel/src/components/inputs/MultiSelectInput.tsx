import {InputPanel} from "./InputPanel";
import React from "react";
import {MultiSelect} from "primereact/multiselect";

export function MultiSelectInput(
    props: {
        data: any,
        column: { field: string, header: string, options: string},
        register: any
        className: any
        control: any
        id: any
    }) {
    const {column} = props
    return <InputPanel  {...props} component={(field: any) => {
        return <MultiSelect
            value={field.value?.length > 0 ? field.value?.split(','): []}
            onChange={(e) => {
                const values = e.value.filter((x: any) => !!x)
                return field.onChange(values.join(','))
            }}
            options={column.options?.split(',')}
            display="chip"
            placeholder={"Select " + column.header} className="w-full"/>
    }}/>
}

//if data is not cvs format, use the following two functions to convert
export function arrayToCvs(data:any, columns:string[]) {
    if (!data) {
        return null;
    }

    const item = {...data}
    columns.forEach(x => {
        if (Array.isArray(data[x]) && data[x].length > 0) {
            item[x] = data[x].join(',')
        }
    });
    return item;
}

export function cvsToArray(formData:any, columns:string[]){
    const item = {...formData||{}}
    columns.forEach(x =>{
        if (typeof formData[x] === 'string') {
            item[x] = formData[x]?.split(',');
        }
    })
    return item;
}