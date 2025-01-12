import {InputPanel} from "./InputPanel";
import React from "react";
import {MultiSelect} from "primereact/multiselect";

export function MultiSelectInput(
    props: {
        data: any,
        column: { field: string, header: string, options?: string},
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
