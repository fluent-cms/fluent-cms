import {
    deleteRole,
    saveRole,
    useEntities,
    useOneRole,
} from "../services/accounts";
import {useForm} from "react-hook-form";
import {useParams} from "react-router-dom";
import {Button} from "primereact/button";
import {useRequestStatus} from "../../cms-client/containers/useFormStatus";
import {NewUser} from "./RoleListPage";
import {MultiSelectInput} from "../../components/inputs/MultiSelectInput";
import {FetchingStatus} from "../../components/FetchingStatus";

export function RoleDetailPage() {
    const {name} = useParams()
    const {data: roleData, isLoading: loadingRole, error: errorRole} = useOneRole(name==NewUser?'':name!);
    const {data: entities, isLoading: loadingEntity, error: errorEntities} = useEntities();
    const {checkError, Status, confirm} = useRequestStatus('');
    const {
        register,
        handleSubmit,
        control
    } = useForm()

    if (loadingRole || loadingEntity || errorRole || errorEntities) {
        return <FetchingStatus isLoading={loadingRole || loadingEntity} error={errorRole || errorEntities}/>
    }

    const entitiesOption = entities.map((x: { name: string }) => x.name).join(',');

    const role = roleData|| {fullAccessEntities:'', restrictedAccessEntities:''};
    ['fullAccessEntities', 'restrictedAccessEntities'].forEach(x => {
        if (Array.isArray(role[x])) {
            if (role[x].length > 0) {
                role[x] = role[x].join(',')
            } else {
                delete (role[x]);
            }
        }
    })

    const onSubmit = async (formData: any) => {
        if (name != NewUser){
            formData.name = name;
        }

        ['roles', 'fullAccessEntities', 'restrictedAccessEntities'].forEach(x => {
            formData[x] = formData[x]?.split(',') ?? [];
        })

        const {error} = await saveRole(formData);
        checkError(error, 'Save Role Succeed')
        if (!error && name == NewUser){
            window.location.href = '/roles/' + formData.name;
        }
    }

    const onDelete = async () => {
        confirm('Do you want to delete this role?', async () => {
            const {error} = await deleteRole(name!);
            checkError(error, 'Delete Succeed')
            if (!error) {
                await new Promise(r => setTimeout(r, 500));
                window.location.href = "/roles";

            }
        });
    }

    return <>
        {name !== NewUser && <h2>Editing `{role?.name}`</h2>}
        <Status/>
        <form onSubmit={handleSubmit(onSubmit)} id="form">
            <div className="formgrid grid">
                {name === NewUser && <div className={'field col-12  md:col-4'}>
                    <label>Name</label>
                    <input type={'text'} className="w-full p-inputtext p-component" id={'name'}
                           {...register('name', { required: 'name is required' })}
                    />
                </div>}
                <MultiSelectInput data={role ?? {}} column={{
                    field: 'fullAccessEntities',
                    header: 'Full Access Entities',
                    options: entitiesOption
                }} register={register} className={'field col-12  md:col-4'} control={control} id={name}/>
                <MultiSelectInput data={role??{}} column={{
                    field: 'restrictedAccessEntities',
                    header: 'Restricted Access Entities',
                    options: entitiesOption
                }} register={register} className={'field col-12  md:col-4'} control={control} id={name}/>
            </div>
            <Button type={'submit'} label={"Save Role"} icon="pi pi-check"/>
            {' '}
            <Button type={'button'} label={"Delete Role"} severity="danger" onClick={onDelete}/>
        </form>
    </>
}