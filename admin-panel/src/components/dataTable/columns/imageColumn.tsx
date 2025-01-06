import {Column} from "primereact/column";
import {AvatarGroup} from "primereact/avatargroup";
import {Avatar} from "primereact/avatar";
import { XAttr } from "../../../cms-client/types/schemaExt";

export function imageColumn({column, getFullAssetsURL}:{
    column: XAttr
    getFullAssetsURL : (arg:string) =>string
}){
    const bodyTemplate = (item:any) => {
        const fullURL =  getFullAssetsURL( item[column.field]);
        return <AvatarGroup>
                <Avatar image={fullURL} size="large" shape="circle" />
        </AvatarGroup>
    };
    return <Column key={column.field} field={column.field} header={column.header} body={bodyTemplate}></Column>
}