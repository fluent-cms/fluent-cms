import {Column} from "primereact/column";

export function textColumn({column}:{column: {field:any, header:any}}){
    return <Column key={column.field} field={column.field} header={column.header} sortable filter></Column>
}

