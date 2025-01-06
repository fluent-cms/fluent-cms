import {
    deleteRole,
    saveRole,
    useResource,
    useSingleRole,
} from "../services/accounts";
import {useForm} from "react-hook-form";
import {useParams} from "react-router-dom";
import {Button} from "primereact/button";
import {useConfirm} from "../../components/useConfirm";
import {NewUser} from "./RoleListPage";
import {arrayToCvs, cvsToArray, MultiSelectInput} from "../../components/inputs/MultiSelectInput";
import {FetchingStatus} from "../../components/FetchingStatus";
import { getEntityPermissionColumns } from "../types/utils";
import {useRef, useState} from "react";
import {Message} from "primereact/message";
import {Toast} from "primereact/toast";


export function RoleDetailPage({baseRouter}:{baseRouter:string}) {
    const {name} = useParams()
    const {data: roleData, isLoading: loadingRole, error: errorRole, mutate: mutateRole} = useSingleRole(name==NewUser?'':name!);
    const {data: entities, isLoading: loadingEntity, error: errorEntities} = useResource();
    const {confirm,Confirm} = useConfirm('roleDetailPage');
    const [err,setErr] = useState('');

    const {
        register,
        handleSubmit,
        control
    } = useForm()
    const toast = useRef<any>(null);

    if (loadingRole || loadingEntity || errorRole || errorEntities) {
        return <FetchingStatus isLoading={loadingRole || loadingEntity} error={errorRole || errorEntities}/>
    }

    const entitiesOption = entities?.join(',')??"";
    const columns = getEntityPermissionColumns(entitiesOption);
    const role = arrayToCvs(roleData, columns.map(x=>x.field));

    const onSubmit = async (formData: any) => {
        if (name != NewUser){
            formData.name = name;
        }

        const payload = cvsToArray(formData,columns.map(x=>x.field));
        const {error} = await saveRole(payload);
        
        mutateRole();
        setErr(error);
        
        if (!error ){
            toast.current.show({severity: 'success', summary: 'Successfully Saved Role' });
            if ( name == NewUser) {
                await new Promise(r => setTimeout(r, 500));
                window.location.href = baseRouter + '/roles/' + formData.name;
            }
        }
    }

    const onDelete = async () => {
        confirm('Do you want to delete this role?', async () => {
            const {error} = await deleteRole(name!);
            setErr(error);
            
            if (!error) {
                toast.current.show({severity: 'success', summary: 'Successfully Deleted Role' });
                await new Promise(r => setTimeout(r, 500));
                window.location.href = baseRouter + '/roles/' ;
            }
        });
    }

    return <>
        {name !== NewUser && <h2>Editing Role `{role?.name}`</h2>}
        <Confirm/>
        <form onSubmit={handleSubmit(onSubmit)} id="form">
            {err&& err.split('\n').map(e =>(<><Message severity={'error'} text={e}/>&nbsp;&nbsp;</>))}
            <Toast ref={toast} position="top-right"/>
            <div className="formgrid grid">
                {name === NewUser && <div className={'field col-12  md:col-4'}>
                    <label>Name</label>
                    <input type={'text'} className="w-full p-inputtext p-component" id={'name'}
                           {...register('name', { required: 'name is required' })}
                    />
                </div>}
                {
                    columns.map(x =>  <MultiSelectInput data={role ?? {}} column={x} register={register} className={'field col-12  md:col-4'} control={control} id={name}/>)
                }
            </div>
            <Button type={'submit'} label={"Save Role"} icon="pi pi-check"/>
            {' '}
            <Button type={'button'} label={"Delete Role"} severity="danger" onClick={onDelete}/>
        </form>
    </>
}