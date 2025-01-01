import {Dropdown} from "primereact/dropdown";
import {InputPanel} from "./InputPanel";
import React, {useState} from "react";
import {AutoComplete} from "primereact/autocomplete";

export function LookupInput(props: {
    data: any,
    column: { field: string, header: string, lookup: any; },
    control: any
    className: any
    register: any,
    items: any[]
    id: any
    search:any 
    hasMore:boolean,
}) {
    const {items, column, search, hasMore} = props;
    const [filteredItems, setFilteredItems] = useState(items);
    const searchItems = async (event: any) => {
        var items = await search(event.query)
        console.log("searchResult", event.query, items);
        setFilteredItems(items);
    }

    return <InputPanel  {...props} component={(field: any) => {
        return hasMore ?
            <AutoComplete
                className={'w-full'}
                dropdown
                id={field.name}
                field={column.lookup.targetEntity.titleAttribute}
                value={field.value}
                suggestions={filteredItems}
                completeMethod={searchItems}
                onChange={(e) => {
                    var selectedItem = typeof (e.value) === "object" ? e.value : {[column.lookup.targetEntity.titleAttribute]: e.value};
                    field.onChange(selectedItem);
                }}
            />
            :
            <Dropdown
                id={field.name}
                value={field.value ? field.value[column.lookup.targetEntity.primaryKey] : null}
                options={items}
                focusInputRef={field.ref}
                onChange={(e) => {
                    field.onChange({[column.lookup.targetEntity.primaryKey]: e.value})
                }}
                className={'w-full'}
                optionValue={column.lookup.targetEntity.primaryKey}
                optionLabel={column.lookup.targetEntity.titleAttribute}
                filter
            />
    }
    }/>
}