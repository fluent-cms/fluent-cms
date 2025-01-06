import {useRoles} from "../services/accounts";
import {Link} from "react-router-dom";
import {DataTable} from "primereact/datatable";
import {Column} from "primereact/column";
import {Button} from "primereact/button";
export const NewUser = '__new';

export function RoleListPage() {
    const {data: roles,isLoading} = useRoles();
    const data = roles?.map((x: string)=> ({name:x}));

    const emailTemplate = (record:{name:string}) => {
        return <Link to={record.name}>{record.name}</Link>
    }
    return <>
        <h2>Role list</h2>
        <Link to={NewUser}><Button>Create New Role</Button></Link>
        <div className="card"></div>
        <DataTable loading={isLoading} dataKey={"name"} value={data} paginator rows={100} >
            <Column header={'Name'} field={'name'} sortable filter body={emailTemplate}/>
        </DataTable>
    </>
}