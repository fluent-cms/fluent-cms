import {DataTable} from "primereact/datatable";

export function LazyDataTable({columns, data, dataKey, lazyState, eventHandlers, createColumn}: {
    data: { items: any[], totalRecords: number }
    dataKey: any
    lazyState: any
    eventHandlers: any
    createColumn: any
    columns: any[]
}) {
    const {items, totalRecords} = data ?? {}
    console.log("9999999",{items, totalRecords, columns})
    return columns && data && <DataTable
        sortMode="multiple"
        dataKey={dataKey}
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
        {columns.map((column: any, i: number) => createColumn({column, dataKey}))}
    </DataTable>
}