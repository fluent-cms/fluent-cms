import {useSchema} from "../services/schema";
import {getListColumns, getWriteColumns} from "../utils/columnUtil";
import {useState} from "react";

export function useSubSchema(data :any,
                      schema:any,
                      schemaName:any,
                      column: { field: string, header: string, subTable: any }
){
    const id = (data ?? {})[schema?.dataKey ?? '']
    const targetSchema = useSchema(column.subTable.schema)
    const listColumns = getListColumns(targetSchema,column.subTable.schema, schemaName)
    const formColumns = getWriteColumns(targetSchema)
    const formId = "sublistForm" + targetSchema.name
    const [existingItems, setExistingItems] = useState(null)
    const [toAddItems, setToAddItems] = useState(null)
    return {
        id, listColumns, formColumns, formId, targetSchema, existingItems, setExistingItems, toAddItems, setToAddItems
    }
}

