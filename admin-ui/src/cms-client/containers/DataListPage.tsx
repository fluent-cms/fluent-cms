import {Link, useParams } from "react-router-dom";
import {useSchema} from "../services/schema";
import {LazyDataTable} from "../components/dataTable/LazyDataTable";
import {useListData} from "../services/entity";
import {useLazyStateHandlers} from "./useLazyStateHandlers";
import {createColumn} from "../components/dataTable/columns/createColumn";
import {getListColumns, } from "../utils/columnUtil";
import {Button} from "primereact/button";
import {getFullAssetsURL} from "../configs";

export function DataListPage(){
    //todo: load lazyState from URL, set lazyState to URL
    const {schemaName} = useParams()
    const schema = useSchema(schemaName)
    const columns = getListColumns(schema,schemaName,schemaName)
    const dataKey = schema.dataKey

    const {lazyState, eventHandlers} = useLazyStateHandlers(50)
    let data = useListData(schemaName,lazyState)
    return <>
        <h2>{schema.title} list</h2>
        <Link to={"new"}><Button>Create New {schema.title}</Button></Link>
        <div className="card">
        {schema && <LazyDataTable {...{columns ,dataKey ,data, eventHandlers, lazyState, createColumn, getFullURL: getFullAssetsURL}}/>}
        </div>
    </>
}