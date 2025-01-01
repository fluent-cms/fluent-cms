import {useState} from "react";

export function useEditTable(data :any, schema:any, column: {collection: any } )
{
    const id = (data ?? {})[schema?.primaryKey ?? '']
    const targetSchema = column.collection.targetEntity;
    
    const listColumns = targetSchema?.attributes?.filter(
        (x: any) =>{
            return x.inList 
                && x.field != column.collection.linkAttribute.field;
        }
    ) ?? [];
    
    const inputColumns = targetSchema?.attributes?.filter(
        (x: any) =>{
            return x.inDetail && !x.isDefault
                && x.dataType != "junction" && x.dataType != "collection" 
                && x.field != column.collection.linkAttribute.field;
        }
    ) ??[];

    const [existingItems, setExistingItems] = useState(null)
    const [toAddItems, setToAddItems] = useState(null)
    return {
        id, targetSchema, listColumns, inputColumns,
        existingItems, setExistingItems, toAddItems, setToAddItems
    }
}