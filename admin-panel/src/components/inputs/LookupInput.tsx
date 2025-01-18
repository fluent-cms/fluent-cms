import {Dropdown} from "primereact/dropdown";
import {InputPanel} from "./InputPanel";
import React, {useState} from "react";
import {AutoComplete} from "primereact/autocomplete";
import {XAttr} from "../../cms-client/types/schemaExt";

export function LookupInput(props: {
    data: any,
    column: XAttr,
    control: any
    className: any
    register: any,
    items: any[]
    id: any
    search:(s:string) => Promise<any[]|undefined>, 
    hasMore:boolean,
}) {
    const {items, column, search, hasMore} = props;
    const [filteredItems, setFilteredItems] = useState(items);
    const searchItems = async (event: any) => {
        const items = await search(event.query)
        setFilteredItems(items??[]);
    }

    return <InputPanel  {...props} component={(field: any) => {
        return hasMore ?
            <AutoComplete
                className={'w-full'}
                dropdown
                id={field.name}
                field={column.lookup!.labelAttributeName}
                value={field.value}
                suggestions={filteredItems}
                completeMethod={searchItems}
                onChange={(e) => {
                    var selectedItem = typeof (e.value) === "object" ? e.value : {[column.lookup!.labelAttributeName]: e.value};
                    field.onChange(selectedItem);
                }}
            />
            :
            <Dropdown
                id={field.name}
                value={field.value ? field.value[column.lookup!.primaryKey] : null}
                options={items}
                focusInputRef={field.ref}
                onChange={(e) => {
                    field.onChange({[column.lookup!.primaryKey]: e.value})
                }}
                className={'w-full'}
                optionValue={column.lookup!.primaryKey}
                optionLabel={column.lookup!.labelAttributeName}
                filter
            />
    }
    }/>
}