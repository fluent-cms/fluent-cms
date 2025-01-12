import {Column} from "primereact/column";
import {Link} from "react-router-dom";
import { XAttr, XEntity } from "../../../cms-client/types/schemaExt";

export function textColumn({column, baseRouter, schema}:{
    baseRouter:string
    schema:XEntity
    column: XAttr,
}){
    let field = column.field;
    if (column.displayType == "lookup"){
        field = column.field + "." + column.lookup!.titleAttribute;
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
        if (column.dataType === "lookup" && val){
            val = val[column.lookup!.titleAttribute]
        }

        if (column.field == schema.titleAttribute){
            return <Link to={`${baseRouter}/${schema.name}/${item[schema.primaryKey]}?ref=${encodeURIComponent(window.location.href)}`}>{val}</Link>
        }else {
            return <>{val}</>
        }
    };
    return <Column dataType={dataType} key={column.field} field={field} header={column.header} sortable filter body={bodyTemplate}></Column>
}