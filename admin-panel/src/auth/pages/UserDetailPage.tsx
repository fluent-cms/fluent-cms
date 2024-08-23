import {deleteUser, saveUser, useEntities, useOneUsers, useRoles} from "../services/accounts";
import {useForm} from "react-hook-form";
import {useParams} from "react-router-dom";
import {Button} from "primereact/button";
import {useRequestStatus} from "../../cms-client/containers/useFormStatus";
import {FetchingStatus} from "../../components/FetchingStatus";
import {arrayToCvs, cvsToArray, MultiSelectInput} from "../../components/inputs/MultiSelectInput";
import { getEntityPermissionColumns} from "../types/Profile";


export function UserDetailPage() {
    const {id} = useParams()
    const {data:userData,isLoading:loadingUser, error: errorUser} = useOneUsers(id!);
    const {data:roles, isLoading:loadingRoles, error:errorRoles } = useRoles();
    const {data:entities, isLoading:loadingEntity, error:errorEntities} = useEntities();
    const {checkError, Status, confirm} = useRequestStatus('');
    const {
        register,
        handleSubmit,
        control
    } = useForm()

    if (loadingUser || loadingRoles || loadingEntity ||errorUser || errorRoles || errorEntities){
        return <FetchingStatus isLoading={loadingUser || loadingRoles || loadingEntity} error={errorUser || errorRoles || errorEntities}/>
    }


    const rolesOption = roles.join(',');
    const entitiesOption = entities.map((x: {name:string})=>x.name).join(',');

    const columns = [
        {field:'roles',header:'Roles',options: rolesOption},
        ...getEntityPermissionColumns(entitiesOption)
    ];

    const user = arrayToCvs(userData, columns.map(x=>x.field));
    const onSubmit = async (formData: any)=>{
        formData.id = id;
        var payload = cvsToArray(formData,columns.map(x=>x.field));
        const {error} = await saveUser(payload);
        checkError(error, 'Save User Succeed')
    }

    const onDelete = async () =>{
        confirm('Do you want to delete this user?',async () => {
            const {error} = await deleteUser(id!);
            checkError(error, 'Delete Succeed')
            if (!error) {
                await new Promise(r => setTimeout(r, 500));
                window.location.href = "/users";
            }
        });
    }

    return <>
        <h2>Editing {user.email}</h2>
        <Status/>
        <form onSubmit={handleSubmit(onSubmit)} id="form">
            <div className="formgrid grid">
                {
                    columns.map(x => (<MultiSelectInput data={user} column={x} register={register} className={'field col-12  md:col-4'} control={control} id={id}/>))
                }
            </div>
            <Button type={'submit'} label={"Save User"} icon="pi pi-check" />
            {' '}
            <Button type={'button'} label={"Delete User" } severity="danger" onClick={onDelete}/>
        </form>
    </>
}