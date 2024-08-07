import {Column} from "primereact/column";
import {AvatarGroup} from "primereact/avatargroup";
import {Avatar} from "primereact/avatar";

export function imageColumn({column, primaryKey, getFullURL}:{
    primaryKey:string,
    column: {field:any, header:any, linkToEntity:string}
    getFullURL : (arg:string) =>string
}){
    const bodyTemplate = (item:any) => {
        const fullURL =  getFullURL( item[column.field]);
        return <AvatarGroup>
            <a href={`${column.linkToEntity}/${item[primaryKey]}`} >
                <Avatar image={fullURL} size="large" shape="circle" />
            </a>
        </AvatarGroup>
    };
    return <Column key={column.field} field={column.field} header={column.header} body={bodyTemplate}></Column>
}