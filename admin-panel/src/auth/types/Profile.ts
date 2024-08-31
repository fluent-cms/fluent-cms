export type Profile = {
    id: string;
    email: string;
    roles: string[];
    readWriteEntities: string[];
    restrictedReadWriteEntities:string[] ;
    readonlyEntities: string[];
    restrictedReadonlyEntities: string[];
    allowedMenus: string[];
};



export function getEntityPermissionColumns(entitiesOption :string){
    return [
        {field:'readWriteEntities',header:'Read Write Entities',options: entitiesOption},
        {field:'restrictedReadWriteEntities',header:'Restricted Read Write Entities',options: entitiesOption},
        {field:'readonlyEntities',header:'Readonly Entities',options: entitiesOption},
        {field:'restrictedReadonlyEntities',header:'Restricted Readonly Entities',options: entitiesOption},
    ]
}