import {DataTable} from "primereact/datatable";
import {useUsers} from "../services/accounts";
import {Column} from "primereact/column";
import {Link} from "react-router-dom";

export function UserListPage() {
    const {data,isLoading} = useUsers();
    const roleTemplate = (record:{roles:string[]}) => {
        return record.roles.join(',');
    };
    const emailTemplate = (record:{email:string,id:string[]}) => {
        return <Link to={record.id.toString()}>{record.email}</Link>
    }
    return <>
        <h2>User list</h2>
        <div className="card"></div>
        <DataTable loading={isLoading} dataKey={"id"} value={data} paginator rows={100} >
            <Column header={'Email'} field={'email'} sortable filter body={emailTemplate}/>
            <Column header={'Role'} field={'role'} body={roleTemplate}/>
        </DataTable>
    </>
}