import {Dropdown} from "primereact/dropdown";
import {InputPanel} from "./InputPanel";
import React from "react";
import {useListData} from "../../../services/entity";

export function LookupDownInput(props: {
    data: any,
    column: { field: string, header: string,lookup:any; options: any[]},
    control: any
    className: any
    register: any
    id:any
}) {
    //todo: here I violate  'dumb component, smart container' principle, need to fix it later
    console.log(props.column)
    const data = useListData(props.column.lookup.entityName,null);

    return  data && <InputPanel  {...props} component={ (field:any) =>
        <Dropdown
            id={field.name}
            value={field.value}
            options={data.items}
            focusInputRef={field.ref}
            onChange={(e) => field.onChange(e.value)}
            className={'w-full'}
            optionValue={props.column.lookup.primaryKey}
            optionLabel={props.column.lookup.titleAttribute}
        />
    }/>
}