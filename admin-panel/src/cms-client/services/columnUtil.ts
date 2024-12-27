
export function getWriteColumns(schema:any) {
    return schema?.attributes?.filter((column: any) =>
        !!column.inDetail
        && !column.isDefault
        && column.displayType !=="picklist"
    ) ?? []
}

export function getSubPageColumns(schema:any) {
    return schema?.attributes?.filter((column: any) => column.displayType === 'picklist') ?? []
}

export function getListColumns(schema:any) {
    return schema?.attributes?.filter((column: any) => column.inList ) ?? []
}
