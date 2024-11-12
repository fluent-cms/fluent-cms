
export function getWriteColumns(schema:any) {
    return schema?.attributes?.filter((column: any) =>
        !!column.inDetail
        && !column.isDefault
        && column.type !=="crosstable"
    ) ?? []
}

export function getSubPageColumns(schema:any) {
    return schema?.attributes?.filter((column: any) => column.type === 'crosstable' || column.type === 'subtable') ?? []
}

export function getListColumns(schema:any) {
    return schema?.attributes?.filter((column: any) => column.inList ) ?? []
    /*
    const link = getLinkToEntity(targetSchemaName, currentSchemaName)
    const cols = schema?.attributes?.filter((column: any) => column.inList ) ?? []
    cols.forEach((col:any) =>{
        col.linkToEntity = link
    })
    return cols

     */
}
