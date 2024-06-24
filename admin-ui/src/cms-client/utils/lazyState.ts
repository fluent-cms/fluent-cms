import qs from 'qs';

export const lazyStateUtil = {
    encode:(s :any)=>{
        if (!s){
            return ''
        }
        return qs.stringify(
            sanitizeLazyState(s),
            {encodeValuesOnly: true, skipNulls: true, arrayFormat:'repeat'}
        );
    }
}

function sanitizeLazyState(payload:any){
    const state = {
        first: payload.first,
        rows: payload.rows,
        f: sanitizeFilter({...payload.filters}),
        s: payload.multiSortMeta,
    }
    deleteEmptyProperties(state)
    return state
}
function sanitizeFilter(filter: any) {
    Object.keys(filter ?? {}).forEach(k => {
        const item = filter[k]
        if (item?.constraints?.length > 0 && item.constraints[0].value === null) {
            delete filter[k]
        }
    })
    return filter
}

function deleteEmptyProperties(obj: any) {
    for (const prop in obj) {
        if (obj[prop] === null || obj[prop] === undefined) {
            delete obj[prop];
        }
    }
}
