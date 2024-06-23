import {Column} from "primereact/column";

export function textColumn({primaryKey, column, titleAttribute}:{
    primaryKey: string,
    titleAttribute: string;
    column:  {type :string, field:string, header:any, linkToEntity:string,lookup:any}
}){
    const bodyTemplate = (item:any) => {
        let val = item[column.field]
        var dataField = column.field+"_data";
        if (column.type === "lookup" && item[dataField]){
            val = item[dataField][column.lookup.titleAttribute]
        }

        if (column.field == titleAttribute){
            return <a href={`${column.linkToEntity}/${item[primaryKey]}`} >{val}</a>;
        }else {
            return <>{val}</>
        }
    };
    return <Column key={column.field} field={column.field} header={column.header} sortable filter body={bodyTemplate}></Column>
}

