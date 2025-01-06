export function getEntityPermissionColumns(entitiesOption :string){
    return [
        {field:'readWriteEntities',header:'Read Write Entities',options: entitiesOption},
        {field:'restrictedReadWriteEntities',header:'Restricted Read Write Entities',options: entitiesOption},
        {field:'readonlyEntities',header:'Readonly Entities',options: entitiesOption},
        {field:'restrictedReadonlyEntities',header:'Restricted Readonly Entities',options: entitiesOption},
    ]
}