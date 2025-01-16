import {
    deleteRole,
    saveRole,
    useEntities,
    useSingleRole,
} from "../services/accounts";
import {useForm} from "react-hook-form";
import {useParams} from "react-router-dom";
import {Button} from "primereact/button";
import {useConfirm} from "../../components/useConfirm";
import {NewUser} from "./RoleListPage";
import {MultiSelectInput} from "../../components/inputs/MultiSelectInput";
import {FetchingStatus} from "../../components/FetchingStatus";
import { entityPermissionColumns } from "../types/utils";
import {useRef, useState} from "react";
import {Message} from "primereact/message";
import {Toast} from "primereact/toast";


export function RoleDetailPage({baseRouter}:{baseRouter:string}) {
    const {name} = useParams()
    const {data: roleData, isLoading: loadingRole, error: errorRole, mutate: mutateRole} = useSingleRole(name==NewUser?'':name!);
    const {data: entities, isLoading: loadingEntity, error: errorEntities} = useEntities();
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

    const onSubmit = async (formData: any) => {
        if (name != NewUser){
            formData.name = name;
        }
        const {error} = await saveRole(formData);
        
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
        {name !== NewUser && <h2>Editing Role `{roleData?.name}`</h2>}
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
                    entityPermissionColumns.map(x =>  (
                        <MultiSelectInput 
                            data={roleData??{}} 
                            column={x} 
                            register={register} 
                            className={'field col-12  md:col-4'} 
                            control={control} 
                            id={name}
                            options={entities??[]}
                        />)
                    )
                }
            </div>
            <Button type={'submit'} label={"Save Role"} icon="pi pi-check"/>
            {' '}
            <Button type={'button'} label={"Delete Role"} severity="danger" onClick={onDelete}/>
        </form>
    </>
}