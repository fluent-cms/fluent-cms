
export function getWriteColumns(schema:any) {
    return schema?.attributes?.filter((column: any) =>
        !!column.inDetail
        && !column.isDefault
        && column.type !=="junction"
    ) ?? []
}

export function getSubPageColumns(schema:any) {
    return schema?.attributes?.filter((column: any) => column.type === 'junction' || column.type === 'subtable') ?? []
}

export function getListColumns(schema:any) {
    return schema?.attributes?.filter((column: any) => column.inList ) ?? []
}
