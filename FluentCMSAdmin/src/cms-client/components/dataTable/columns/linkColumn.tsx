import {Column} from "primereact/column";

export function linkColumn({dataKey, column}:{
    dataKey: any,
    column:  {field:any, header:any, linkToEntity:string}
}){
    const bodyTemplate = (item:any) => {
        return <a href={`${column.linkToEntity}/${item[dataKey]}`} >{item[column.field]}</a>;
    };
    return <Column key={column.field} field={column.field} header={column.header} sortable filter body={bodyTemplate}></Column>
}

