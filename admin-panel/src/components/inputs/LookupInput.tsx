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
    const {items,column} = props;
    return  <InputPanel  {...props} component={ (field:any) => {
        return <Dropdown
            id={field.name}
            value={field.value ? field.value[column.lookup.primaryKey] : null}
            options={items}
            focusInputRef={field.ref}
            onChange={(e) => {
                field.onChange({[column.lookup.primaryKey]:e.value})
            }}
            className={'w-full'}
            optionValue={column.lookup.primaryKey}
            optionLabel={column.lookup.titleAttribute}
        />
    }
    }/>
}