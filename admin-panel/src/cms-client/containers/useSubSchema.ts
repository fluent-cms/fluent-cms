import {getListColumns, getWriteColumns} from "../services/columnUtil";
import {useState} from "react";

export function useSubSchema(data :any,
                      schema:any,
                      column: { field: string, header: string, crosstable: any }
){
    const id = (data ?? {})[schema?.primaryKey ?? '']
    const targetSchema = column.crosstable.targetEntity;
    const listColumns = getListColumns(targetSchema,targetSchema.name, schema.name)
    const formColumns = getWriteColumns(targetSchema)
    const formId = "sublistForm" + targetSchema.name
    const [existingItems, setExistingItems] = useState(null)
    const [toAddItems, setToAddItems] = useState(null)
    return {
        id, listColumns, formColumns, formId, targetSchema, existingItems, setExistingItems, toAddItems, setToAddItems
    }
}

