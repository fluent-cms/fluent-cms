import {useForm} from "react-hook-form";
import {createInput} from "./createInput";

export function ItemForm({columns, data, id, onSubmit, formId, uploadUrl, getFullAssetsURL}: {
    columns: any[],
    data: any,
    id?: any
    onSubmit: any
    formId: any
    uploadUrl:any
    getFullAssetsURL : (arg:string) =>string

}) {
    const {
        register,
        handleSubmit,
        control
    } = useForm()

    return columns && <form onSubmit={handleSubmit(onSubmit)} id={formId}>
        <div className="formgrid grid">
            {
                columns.map((column: any) => createInput({data, column, register, control, id, uploadUrl,getFullAssetsURL}))
            }
        </div>
    </form>
}
