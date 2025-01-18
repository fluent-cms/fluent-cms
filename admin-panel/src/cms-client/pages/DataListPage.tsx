import {Link, useParams } from "react-router-dom";
import {useListData} from "../services/entity";
import {useLazyStateHandlers} from "../containers/useLazyStateHandlers";
import {Button} from "primereact/button";
import {getFullAssetsURL} from "../services/configs";
import {PageLayout} from "./PageLayout";
import {FetchingStatus} from "../../components/FetchingStatus";
import {LazyDataTable} from "../../components/dataTable/LazyDataTable";
import {encodeLazyState} from "../services/lazyState";
import {useEffect} from "react";
import { useLocation } from 'react-router-dom';
import {XEntity} from "../types/schemaExt";

export function DataListPage({baseRouter}:{baseRouter:string}){
    const {schemaName} = useParams()
    return <PageLayout schemaName={schemaName??''} baseRouter={baseRouter} page={DataListPageComponent}/>
}

export function DataListPageComponent({schema,baseRouter}:{schema:XEntity,baseRouter:string}) {
    const columns = schema?.attributes?.filter(column => column.inList && column.dataType != 'junction' && column.dataType !='collection') ?? [];
    let {lazyState, eventHandlers} = useLazyStateHandlers(schema.defaultPageSize, columns, useLocation().search.replace("?",""))
    const {data, error, isLoading}= useListData(schema.name,lazyState)

    useEffect(()=>{
        window.history.replaceState(null,"", `?${encodeLazyState(lazyState)}`);
    },[lazyState]);

    return <>
        <FetchingStatus isLoading={isLoading} error={error}/>
        <h2>{schema.displayName} list</h2>
        <Link to={"new"}><Button>Create New {schema.displayName}</Button></Link>
        <div className="card">
            <LazyDataTable {...{columns,schema,baseRouter,data, eventHandlers, lazyState,  getFullAssetsURL}}/>
        </div>
    </>
}
