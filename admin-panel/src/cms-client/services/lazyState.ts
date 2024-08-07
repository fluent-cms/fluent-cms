import qs from 'qs';

export function encodeLazyState(state:any) {
    if (!state){
        return ''
    }
    state = deepClone(state);
    return qs.stringify(
        sanitizeLazyState(state),
        {encodeValuesOnly: true, skipNulls: true, arrayFormat:'repeat'}
    );
}

function sanitizeLazyState(payload:any){
    sanitizeFilter(payload.filters);
    flatFilter(payload.filters);

    const state = {
        offset: payload.first,
        limit: payload.rows,
        ...payload.filters,
        sort: flatSort(payload.multiSortMeta),
    }
    deleteEmptyProperties(state)
    return state
}


function flatSort(sorts:any[]){
    const item:any = {}
    sorts = sorts ?? [];
    sorts.forEach((sort:any) =>{
        item[sort.field] = sort.order;
    })
    return item
}

function flatFilter(filter:any){
    Object.keys(filter??{}).forEach(k =>{
        const item = filter[k];
        item.constraints.forEach((constraint:any) =>{
            if (!item[constraint.matchMode]){
                item[constraint.matchMode] = [constraint.value];
            }else {
                item[constraint.matchMode].push(constraint.value);
            }
        })
        delete item.constraints;
    })
    return filter
}

function sanitizeFilter(filter: any) {
    Object.keys(filter ?? {}).forEach(k => {
        const item = filter[k]
        if (item?.constraints?.length > 0 && item.constraints[0].value === null) {
            delete filter[k]
        }else if (item?.constraints?.length == 1 || item?.operator == 'and'){
            delete filter[k].operator
        }
    })
}

function deleteEmptyProperties(obj: any) {
    for (const prop in obj) {
        if (obj[prop] === null || obj[prop] === undefined) {
            delete obj[prop];
        }
    }
}

function deepClone(obj: any): any {
    if (obj === null || typeof obj !== 'object') {
        return obj;
    }

    if (Array.isArray(obj)) {
        const arrCopy = [] as any[];
        for (const item of obj) {
            arrCopy.push(deepClone(item));
        }
        return arrCopy as any;
    }

    const objCopy = {} as { [key: string]: any };
    for (const key in obj) {
        if (obj.hasOwnProperty(key)) {
            objCopy[key] = deepClone(obj[key]);
        }
    }
    return objCopy ;
}