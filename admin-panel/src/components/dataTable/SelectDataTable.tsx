import {DataTable} from "primereact/datatable";
import {Column} from "primereact/column";
import {createColumn} from "./columns/createColumn";
import {XAttr, XEntity} from "../../cms-client/types/schemaExt";
import { ListResponse } from "../../cms-client/types/listResponse";

export function SelectDataTable({baseRouter,schema, columns, data, selectedItems, setSelectedItems, lazyState, eventHandlers,getFullAssetsURL}: {
    columns: XAttr[]
    schema:XEntity | undefined
    data: ListResponse | undefined
    selectedItems: any
    setSelectedItems: any
    lazyState: any
    eventHandlers: any
    getFullAssetsURL : (arg:string) => string,
    baseRouter: string
}) {
    const {items, totalRecords} = data ?? {}
    return columns && data && schema && <DataTable
        sortMode="multiple"
        dataKey={schema.primaryKey}
        value={items}
        paginator
        totalRecords={totalRecords}
        rows={lazyState?.rows??10}
        lazy={!!lazyState}
        first={lazyState?.first}
        filters={lazyState?.filters}
        multiSortMeta={lazyState?.multiSortMeta}
        sortField={lazyState?.sortField}
        sortOrder={lazyState?.sortOrder}
        {...eventHandlers}

        selection={selectedItems}
        onSelectionChange={(e) => setSelectedItems(e.value)}
    >
        <Column selectionMode="multiple" headerStyle={{width: '3rem'}}></Column>
        {columns.map((column: any, i: number) => createColumn({column, schema,getFullAssetsURL, baseRouter}))}
    </DataTable>
}