import {useState} from "react";

export function usePicklist(data :any, schema:any, column: {junction: any } )
{
    const id = (data ?? {})[schema?.primaryKey ?? '']
    const targetSchema = column.junction.targetEntity;
    const listColumns = targetSchema?.attributes?.filter((column: any) => column.inList ) ?? []; 
    
    const [existingItems, setExistingItems] = useState(null)
    const [toAddItems, setToAddItems] = useState(null)
    return {
        id, targetSchema, listColumns,
        existingItems, setExistingItems, toAddItems, setToAddItems
    }
}