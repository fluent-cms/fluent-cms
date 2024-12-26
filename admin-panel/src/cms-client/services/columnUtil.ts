
export function getWriteColumns(schema:any) {
    return schema?.attributes?.filter((column: any) =>
        !!column.inDetail
        && !column.isDefault
        && column.displayType !=="junction"
    ) ?? []
}

export function getSubPageColumns(schema:any) {
    return schema?.attributes?.filter((column: any) => column.displayType === 'junction') ?? []
}

export function getListColumns(schema:any) {
    return schema?.attributes?.filter((column: any) => column.inList ) ?? []
}
