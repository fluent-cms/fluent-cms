import {DataTable} from "primereact/datatable";
import {Column} from "primereact/column";
import {useState} from "react";

export function SelectDataTable({dataKey, columns, data, createColumn, selectedItems, setSelectedItems, lazyState, eventHandlers}: {
    dataKey: any
    columns: any[]
    data: { items: any[], totalRecords: number }
    createColumn: any
    selectedItems: any
    setSelectedItems: any
    lazyState: any
    eventHandlers: any
}) {
    const {items, totalRecords} = data ?? {}
    return columns && data && <DataTable
        selection={selectedItems}
        onSelectionChange={(e) => setSelectedItems(e.value)}
        dataKey={dataKey}
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
    >
        <Column selectionMode="multiple" headerStyle={{width: '3rem'}}></Column>
        {columns.map((column: any, i: number) => createColumn({column, dataKey}))}
    </DataTable>
}