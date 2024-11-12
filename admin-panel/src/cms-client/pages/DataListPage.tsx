import {Link, useParams } from "react-router-dom";
import {useListData} from "../services/entity";
import {useLazyStateHandlers} from "../containers/useLazyStateHandlers";
import {getListColumns, } from "../services/columnUtil";
import {Button} from "primereact/button";
import {getFullAssetsURL} from "../services/configs";
import {PageLayout} from "./PageLayout";
import {FetchingStatus} from "../../components/FetchingStatus";
import {LazyDataTable} from "../../components/dataTable/LazyDataTable";
import {encodeLazyState} from "../services/lazyState";
import {useEffect} from "react";
import { useLocation } from 'react-router-dom';

export function DataListPage({baseRouter}:{baseRouter:string}){
    const {schemaName} = useParams()
    return <PageLayout schemaName={schemaName??''} baseRouter={baseRouter} page={DataListPageComponent}/>
}

export function DataListPageComponent({schema,baseRouter}:{schema:any,baseRouter:string}) {
    const columns = getListColumns(schema)
    const {primaryKey,titleAttribute} = schema;
    let {lazyState, eventHandlers} = useLazyStateHandlers(schema.defaultPageSize,columns, useLocation().search.replace("?",""))
    const {data, error, isLoading}= useListData(schema.name,lazyState)

    useEffect(()=>{
        window.history.replaceState(null,"", `?${encodeLazyState(lazyState)}`);
    },[lazyState]);

    return <>
        <FetchingStatus isLoading={isLoading} error={error}/>
        <h2>{schema.title} list</h2>
        <Link to={"new"}><Button>Create New {schema.title}</Button></Link>
        <div className="card">
            <LazyDataTable {...{entityName:schema.name,baseRouter,columns ,primaryKey, titleAttribute ,data, eventHandlers, lazyState,  getFullAssetsURL}}/>
        </div>
    </>
}
