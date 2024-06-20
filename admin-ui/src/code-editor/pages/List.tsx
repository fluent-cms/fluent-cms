import {appendNextVersion, useVersionList} from "../services/version";
import {DataTable} from "primereact/datatable";
import {Column} from "primereact/column";
import {Button} from "primereact/button";
import {useParams} from "react-router-dom";
import { useNavigate} from "react-router-dom";
import qs from "qs";
import {compareParam} from "../types";

export function List(){
    const navigate = useNavigate()
    const {tableName, recordID} = useParams()
    const data = useVersionList({table_name:tableName, record_id: recordID})
    const compareTemplate = (rowData:{id:any, id1:any}) => {
        return rowData.id && <Button type="button" icon={'pi pi-search'} className="p-button-sm p-button-text" onClick={() => {
            const param: compareParam = {
                current:rowData.id, old:rowData.id1
            }
            navigate('compare?' + qs.stringify(param))
        }} />;
    };
    appendNextVersion(data)
    return data &&<DataTable dataKey={'id'} paginator totalRecords={data.length} rows={50} value={data}>
        <Column field={'id'} header={'Version'}/>
        <Column field={'createdAt'} header={'Created At'}/>
        <Column style={{ flex: '0 0 4rem' }} body={compareTemplate}></Column>
    </DataTable>
}