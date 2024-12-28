import {useForm} from "react-hook-form";
import {createInput} from "./createInput";

export function ItemForm({schema, data, id, onSubmit, formId, uploadUrl, getFullAssetsURL}: {
    schema: any,
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

    var inputColumns = schema.attributes?.filter((column:any) => 
        column.inDetail && 
        column.dataType !=="junction" && 
        column.dataType !=="collection" && !column.isDefault )
    
    return inputColumns && <form onSubmit={handleSubmit(onSubmit)} id={formId}>
        <div className="formgrid grid">
            {
                inputColumns.map((column: any) => createInput({data, column, register, control, id, uploadUrl,getFullAssetsURL}))
            }
        </div>
    </form>
}
