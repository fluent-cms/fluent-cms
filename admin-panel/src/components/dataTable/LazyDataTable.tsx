import {DataTable} from "primereact/datatable";
import {createColumn} from "./columns/createColumn";

export function LazyDataTable({baseRouter,schema,columns, data, lazyState, eventHandlers, getFullAssetsURL}: {
    schema:{
        name:string,
        primaryKey:string
        titleAttribute:string
        attributes:any[]
    },
    columns:any[],
    baseRouter: string
    data: { items: any[], totalRecords: number }
    lazyState: any
    eventHandlers: any
    getFullAssetsURL : (arg:string) =>string
}) {
    
    const {items, totalRecords} = data ?? {}
    const {primaryKey,titleAttribute,name} = schema;
    
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
        {columns.map((column: any, i: number) => createColumn({column, primaryKey,titleAttribute,getFullAssetsURL, baseRouter, entityName:name}))}
    </DataTable>
}