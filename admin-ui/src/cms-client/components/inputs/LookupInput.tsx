import {Dropdown} from "primereact/dropdown";
import {InputPanel} from "./InputPanel";
import React from "react";

export function LookupInput(props: {
    data: any,
    column: { field: string, header: string,lookup:any;},
    control: any
    className: any
    register: any,
    items: any[]
    id:any
}) {
    return  <InputPanel  {...props} component={ (field:any) =>
        <Dropdown
            id={field.name}
            value={field.value}
            options={props.items}
            focusInputRef={field.ref}
            onChange={(e) => field.onChange(e.value)}
            className={'w-full'}
            optionValue={props.column.lookup.primaryKey}
            optionLabel={props.column.lookup.titleAttribute}
        />
    }/>
}