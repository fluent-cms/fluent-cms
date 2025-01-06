import {useState} from "react";
import {XAttr, XEntity } from "../types/schemaExt";

export function useEditTable(data :any, schema:XEntity, column: XAttr )
{
    const id = (data ?? {})[schema?.primaryKey ?? '']
    const targetSchema = column.collection;
    
    const listColumns = targetSchema?.attributes?.filter(
        (x: any) =>{
            return x.inList
                && x.dataType != "Junction" && x.dataType != "Collection"
        }
    ) ?? [];
    
    const inputColumns = targetSchema?.attributes?.filter(
        (x: any) =>{
            return x.inDetail && !x.isDefault
                && x.dataType != "Junction" && x.dataType != "Collection" 
        }
    ) ??[];

    const [existingItems, setExistingItems] = useState(null)
    const [toAddItems, setToAddItems] = useState(null)
    return {
        id, targetSchema, listColumns, inputColumns,
        existingItems, setExistingItems, toAddItems, setToAddItems
    }
}