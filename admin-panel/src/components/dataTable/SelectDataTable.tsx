import {DataTable} from "primereact/datatable";
import {Column} from "primereact/column";
import {createColumn} from "./columns/createColumn";

export function SelectDataTable({primaryKey, titleAttribute, columns, data, selectedItems, setSelectedItems, lazyState, eventHandlers,getFullURL}: {
    primaryKey: string
    titleAttribute: string
    columns: any[]
    data: { items: any[], totalRecords: number }
    selectedItems: any
    setSelectedItems: any
    lazyState: any
    eventHandlers: any
    getFullURL : (arg:string) => string,
}) {
    const {items, totalRecords} = data ?? {}
    return columns && data && <DataTable
        sortMode="multiple"
        dataKey={primaryKey}
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
        {columns.map((column: any, i: number) => createColumn({column, primaryKey, titleAttribute,getFullURL }))}
    </DataTable>
}