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
import { useCheckError } from "../../components/useCheckError";


export function RoleDetailPage({baseRouter}:{baseRouter:string}) {
    const {name} = useParams()
    const {data: roleData, isLoading: loadingRole, error: errorRole, mutate: mutateRole} = useSingleRole(name==NewUser?'':name!);
    const {data: entities, isLoading: loadingEntity, error: errorEntities} = useEntities();
    const {confirm,Confirm} = useConfirm('roleDetailPage');
    const {handleErrorOrSuccess, CheckErrorStatus} = useCheckError();
    const { register, handleSubmit, control } = useForm()

    if (loadingRole || loadingEntity || errorRole || errorEntities) {
        return <FetchingStatus isLoading={loadingRole || loadingEntity} error={errorRole || errorEntities}/>
    }

    const onSubmit = async (formData: any) => {
        if (name != NewUser){
            formData.name = name;
        }
        const {error} = await saveRole(formData);
        await handleErrorOrSuccess(error, 'Successfully Saved Role', ()=> {
            if ( name == NewUser) {
                window.location.href = baseRouter + '/roles/' + formData.name;
            }else{
                mutateRole();
            }
        });
    }

    const onDelete = async () => {
        confirm('Do you want to delete this role?', async () => {
            const {error} = await deleteRole(name!);
            await handleErrorOrSuccess(error, 'Delete Succeed', ()=> {
                window.location.href = baseRouter + '/roles/' ;
            })
        });
    }

    return <>
        {name !== NewUser && <h2>Editing Role `{roleData?.name}`</h2>}
        <Confirm/>
        <form onSubmit={handleSubmit(onSubmit)} id="form">
            <CheckErrorStatus/>
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