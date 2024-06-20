import React from "react";
import {Calendar} from "primereact/calendar";
import {InputPanel} from "./InputPanel";

export function DatetimeInput(
    props: {
        data: any,
        column: { field: string, header: string },
        register: any
        className:any
        control:any
        id:any
    }) {
    return <InputPanel  {...props} component={ (field:any) =>{
        let d:any = new Date(field.value)
        // @ts-ignore
        if (isNaN(d)){
            d =null
        }
        return <Calendar id={field.name} showTime hourFormat="24" value={d} className={'w-full'} readOnlyInput={false}  onChange={(e) => {
            if (e.value){
                field.onChange(e.value)
            }
        }} />
    }}/>
}