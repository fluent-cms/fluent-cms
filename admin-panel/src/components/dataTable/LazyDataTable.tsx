import {DataTable} from "primereact/datatable";
import {createColumn} from "./columns/createColumn";

export function LazyDataTable({columns, data, primaryKey,titleAttribute, lazyState, eventHandlers, getFullURL}: {
    primaryKey: string
    titleAttribute: string
    data: { items: any[], totalRecords: number }
    lazyState: any
    eventHandlers: any
    columns: any[]
    getFullURL : (arg:string) =>string
}) {
    const {items, totalRecords} = data ?? {}
    return columns && data && <DataTable
        sortMode="multiple"
        dataKey={primaryKey}
        value={items}
        paginator
        totalRecords={totalRecords}
        rows={lazyState.rows}
        lazy
        first={lazyState.first}
        filters={lazyState.filters}
        multiSortMeta={lazyState.multiSortMeta}
        sortField={lazyState.sortField}
        sortOrder={lazyState.sortOrder}
        {...eventHandlers}
    >
        {columns.map((column: any, i: number) => createColumn({column, primaryKey,titleAttribute,getFullURL}))}
    </DataTable>
}