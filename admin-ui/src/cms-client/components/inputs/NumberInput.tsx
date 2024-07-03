import React from "react";
import {InputText} from "primereact/inputtext";
import {InputPanel} from "./InputPanel";
import {InputNumber} from "primereact/inputnumber";

export function NumberInput(
    props: {
    data: any,
    column: { field: string, header: string },
    register: any
    className:any
    control:any
        id:any
}) {
    return <InputPanel  {...props} component={ (field:any) =>
        <InputNumber id={field.name} value={field.value} className={'w-full'}
                     onValueChange={(e) => field.onChange(e.value)} />
    }/>
}