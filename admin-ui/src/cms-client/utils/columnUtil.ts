
export function getWriteColumns(schema:any) {
    return schema?.attributes?.filter((column: any) => !!column.inDetail && !column.isDefault) ?? []
}

export function getSubPageColumns(schema:any) {
    return schema?.attributes?.filter((column: any) => column.type === 'subgrid' || column.type === 'sublist') ?? []
}

export function getLinkToEntity(targetSchemaName: string, currentSchemaName :string){
    const segments = window.location.href.split('/')
    while (segments.pop() != currentSchemaName){}
    let prefix = segments.join('/')
    if (!prefix.endsWith('/')){
        prefix += '/'
    }
    return prefix + targetSchemaName
}

export function getListColumns(schema:any, targetSchemaName:any, currentSchemaName :any) {
    const link = getLinkToEntity(targetSchemaName, currentSchemaName)
    const cols = schema?.attributes?.filter((column: any) => !!column.inList ) ?? []
    cols.forEach((col:any) =>{
        col.linkToEntity = link
    })
    return cols
}
