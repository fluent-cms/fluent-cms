import {deleteUser, saveUser, useResource, useSingleUser, useRoles} from "../services/accounts";
import {useForm} from "react-hook-form";
import {useParams} from "react-router-dom";
import {Button} from "primereact/button";
import {useConfirm} from "../../components/useConfirm";
import {FetchingStatus} from "../../components/FetchingStatus";
import {MultiSelectInput} from "../../components/inputs/MultiSelectInput";
import {entityPermissionColumns} from "../types/utils";
import {Toast} from "primereact/toast";
import {useRef, useState} from "react";
import {Message} from "primereact/message";

export function UserDetailPage({baseRouter}: { baseRouter: string }) {
    const {id} = useParams()
    const {data: userData, isLoading: loadingUser, error: errorUser, mutate: mutateUser} = useSingleUser(id!);
    const {data: roles, isLoading: loadingRoles, error: errorRoles} = useRoles();
    const {data: entities, isLoading: loadingEntity, error: errorEntities} = useResource();
    const {confirm, Confirm} = useConfirm('userDetailPage');
    const [err, setErr] = useState('');
    const {
        register,
        handleSubmit,
        control
    } = useForm()
    const toast = useRef<any>(null);

    const onSubmit = async (formData: any) => {
        formData.id = id;
        const {error} = await saveUser(formData);
        setErr(error);
        if (!error) {
            toast.current.show({severity: 'success', summary: 'Successfully Updated User'});
        }
        mutateUser();
    }

    const onDelete = async () => {
        confirm('Do you want to delete this user?', async () => {
            const {error} = await deleteUser(id!);
            setErr(error);
            if (!error) {
                await new Promise(r => setTimeout(r, 500));
                window.location.href = baseRouter + "/users";
            }
        });
    }

    return <>
        <FetchingStatus isLoading={loadingUser || loadingRoles || loadingEntity}
                        error={errorUser || errorRoles || errorEntities}/>
        {userData && roles && <>
            <Confirm/>
            <h2>Editing {userData.email}</h2>
            <form onSubmit={handleSubmit(onSubmit)} id="form">
                {err && err.split('\n').map(e => (<><Message severity={'error'} text={e}/>&nbsp;&nbsp;</>))}

                <Toast ref={toast} position="top-right"/>
                <Button type={'submit'} label={"Save User"} icon="pi pi-check"/>
                {' '}
                <Button type={'button'} label={"Delete User"} severity="danger" onClick={onDelete}/>
                <br/>
                <div className="formgrid grid">
                    <MultiSelectInput
                        column={{
                            field: 'roles',
                            header: "Roles"
                        }} options={roles}
                        register={register} 
                        className={'field col-12  md:col-4'} 
                        control={control} 
                        id={id}
                        data={userData}/>

                    {entityPermissionColumns.map(x => (
                        <MultiSelectInput
                            data={userData}
                            options={entities ?? []}
                            column={x}
                            register={register}
                            className={'field col-12  md:col-4'} 
                            control={control}
                            id={id}/>))
                    }
                </div>
            </form>
        </>
        }
    </>
}