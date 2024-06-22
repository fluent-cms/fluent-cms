import {Dropdown} from "primereact/dropdown";
import {InputPanel} from "./InputPanel";
import React from "react";
import {useSchema} from "../../../services/schema";
import {useListData} from "../../../services/entity";

export function LookupDownInput(props: {
    data: any,
    column: { field: string, header: string,lookupEntity:any; options: any[]},
    control: any
    className: any
    register: any
    id:any
}) {
    //todo: here I violate  'dumb component, smart container' principle, need to fix it later
    const schemaName = props.column.options[0];
    const data = useListData(schemaName,null);

    return  data && <InputPanel  {...props} component={ (field:any) =>
        <Dropdown
            id={field.name}
            value={field.value}
            options={data.items}
            focusInputRef={field.ref}
            onChange={(e) => field.onChange(e.value)}
            className={'w-full'}
            optionValue={props.column.lookupEntity.primaryKey}
            optionLabel={props.column.lookupEntity.titleAttribute}
        />
    }/>
}