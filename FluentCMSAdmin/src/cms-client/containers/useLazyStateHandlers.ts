import {useReducer} from "react";

function createDefaultState(rows:any) {
    const defaultState :any= {
        first: 0,
        rows,
    }
    /*
    todo: if filters not set, cannot add 2nd rule, maybe it is a prime react bug
    filters:{
        title: { operator: 'and', constraints: [{ value: null, matchMode: 'startsWidth' }] }
    }*/
    return defaultState
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

export function useLazyStateHandlers(rows:number) {
    const [lazyState, dispatch] = useReducer(reducer, createDefaultState(rows))

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
