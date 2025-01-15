import {Column} from "primereact/column";
import {AvatarGroup} from "primereact/avatargroup";
import {Avatar} from "primereact/avatar";
import { XAttr } from "../../../cms-client/types/schemaExt";

export function imageColumn({column, getFullAssetsURL}:{
    column: XAttr
    getFullAssetsURL : (arg:string) =>string
}){
    const bodyTemplate = (item:any) => {
        var value = item[column.field];
        const urls:string[] = Array.isArray(value) ? value : [value];
        const fullURLs =  urls.map(x=>getFullAssetsURL(x ));
        
        return <AvatarGroup>
            {
                fullURLs.map(x=> <Avatar key={x} image={x} size="large" shape="circle" />)
            }
        </AvatarGroup>
    };
    return <Column key={column.field} field={column.field} header={column.header} body={bodyTemplate}></Column>
}