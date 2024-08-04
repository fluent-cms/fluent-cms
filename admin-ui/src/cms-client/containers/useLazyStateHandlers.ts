import {useReducer} from "react";
import {FilterMatchMode} from "primereact/api";

function createDefaultState(rows:any, cols:any[]) {
    const defaultState :any= {
        first: 0,
        rows,
        filters : createDefaultFilter(cols),
    }

    return defaultState
}

function createDefaultFilter(cols:any[]) {
    const getMathMode = (col:any) =>{
        switch (col.type){
            case 'number': return FilterMatchMode.EQUALS
            case 'datetime': return FilterMatchMode.DATE_IS
            default: return FilterMatchMode.STARTS_WITH
        }
    }
    const filters:any = {}

    cols.forEach(col =>{
        filters[col.field] = {operator: 'and', constraints: [{ value: null, matchMode: getMathMode(col) }]}
    });
    return filters
}

function reducer(state: any, action: any) {
    const {type, payload} = action
    switch (type) {
        case 'onPage':
            return {...state, first: payload.first, rows: payload.rows}
        case 'onFilter':
            return {...state, filters: {...state?.filters??{},...payload.filters}}
        case 'onSort':
            return {...state, multiSortMeta: payload.multiSortMeta}
    }
    return state
}

export function useLazyStateHandlers(rows:number, cols: any[]) {
    const [lazyState, dispatch] = useReducer(reducer, createDefaultState(rows, cols))
    return {
        lazyState,
        eventHandlers: {
            onPage: (payload: any) => {
                dispatch({type: 'onPage', payload})
            },
            onFilter: (payload: any) => {
                dispatch({type: 'onFilter', payload})
            },
            onSort:(payload :any)=>{
                dispatch({type: 'onSort', payload})
            }
        }
    }
}
