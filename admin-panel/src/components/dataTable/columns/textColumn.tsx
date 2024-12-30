import {Column} from "primereact/column";
import {Link} from "react-router-dom";

export function textColumn({primaryKey, column, titleAttribute, baseRouter, entityName}:{
    baseRouter:string
    primaryKey: string,
    titleAttribute: string;
    column:  {displayType :string, field:string, header:any, linkToEntity:string,lookup:any}
    entityName:string
}){
    let field = column.field;
    if (column.displayType == "lookup"){
        field = column.field + "." + column.lookup.targetEntity.titleAttribute;
    }
    var dataType = 'text';
    switch (column.displayType){
        case 'number':
            dataType = 'numeric';
            break;
        case 'datetime':
        case 'date':
            dataType = 'date';
            break;
    }

    const bodyTemplate = (item:any) => {
        let val = item[column.field]
        if (column.displayType === "lookup" && val){
            val = val[column.lookup.targetEntity.titleAttribute]
        }

        if (column.field == titleAttribute){
            return <Link to={`${baseRouter}/${entityName}/${item[primaryKey]}?ref=${encodeURIComponent(window.location.href)}`}>{val}</Link>
        }else {
            return <>{val}</>
        }
    };
    return <Column dataType={dataType} key={column.field} field={field} header={column.header} sortable filter body={bodyTemplate}></Column>
}