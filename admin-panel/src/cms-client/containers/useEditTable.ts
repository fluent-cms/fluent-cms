import {useState} from "react";

export function useEditTable(data :any, schema:any, column: {collection: any } )
{
    const id = (data ?? {})[schema?.primaryKey ?? '']
    const targetSchema = column.collection.targetEntity;
    const listColumns = targetSchema?.attributes?.filter((column: any) => column.inList ) ?? [];

    const [existingItems, setExistingItems] = useState(null)
    const [toAddItems, setToAddItems] = useState(null)
    return {
        id, targetSchema, listColumns,
        existingItems, setExistingItems, toAddItems, setToAddItems
    }
}