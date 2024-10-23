import {Link, useParams } from "react-router-dom";
import {useListData} from "../services/entity";
import {useLazyStateHandlers} from "../containers/useLazyStateHandlers";
import {getListColumns, } from "../services/columnUtil";
import {Button} from "primereact/button";
import {getFullAssetsURL} from "../services/configs";
import {PageLayout} from "./PageLayout";
import {FetchingStatus} from "../../components/FetchingStatus";
import {LazyDataTable} from "../../components/dataTable/LazyDataTable";

export function DataListPage(){
    const {schemaName} = useParams()
    return <PageLayout schemaName={schemaName??''} page={DataListPageComponent}/>
}

export function DataListPageComponent({schema}:{schema:any}){
    const columns = getListColumns(schema,schema.name,schema.name)
    const {primaryKey,titleAttribute} = schema;
    const {lazyState, eventHandlers} = useLazyStateHandlers(schema.defaultPageSize,columns)
    const {data, error, isLoading}= useListData(schema.name,lazyState)
    return <>
        <FetchingStatus isLoading={isLoading} error={error}/>
        <h2>{schema.title} list</h2>
        <Link to={"new"}><Button>Create New {schema.title}</Button></Link>
        <div className="card">
            <LazyDataTable {...{columns ,primaryKey, titleAttribute ,data, eventHandlers, lazyState,  getFullURL: getFullAssetsURL}}/>
        </div>
    </>
}
