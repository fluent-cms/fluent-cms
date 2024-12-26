import {useParams} from "react-router-dom";
import {ItemForm} from "../containers/ItemForm";
import {deleteItem, updateItem, useItemData} from "../services/entity";
import {Divider} from "primereact/divider";
import {getSubPageColumns, getWriteColumns} from "../services/columnUtil";
import {Button} from "primereact/button";
import {Junction} from "../containers/Junction";
import {useRequestStatus} from "../containers/useFormStatus";
import {fileUploadURL, getFullAssetsURL} from "../services/configs";
import {PageLayout} from "./PageLayout";
import {FetchingStatus} from "../../components/FetchingStatus";

export function DataItemPage({baseRouter}:{baseRouter:string}) {
    const {schemaName} = useParams()
    return <PageLayout schemaName={schemaName??''} baseRouter={baseRouter} page={DataItemPageComponent}/>
}

export function DataItemPageComponent({schema, baseRouter}:{schema:any, baseRouter:string}) {
    const {id} = useParams()
    const {data,error,isLoading}= useItemData(schema.name, id)
    const {checkError, Status, confirm} = useRequestStatus(schema.name + id)

    const uploadUrl = fileUploadURL()
    const columns = getWriteColumns(schema)
    const subPages = getSubPageColumns(schema)
    const formId = "editForm" + schema.name

    const onSubmit = async (formData: any) => {
        formData[schema.primaryKey] = id
        const {error} = await updateItem(schema.name,formData)
        checkError(error, 'Save Succeed')
    }

    const onDelete = async () => {
        confirm('Do you want to delete this item?',async () => {
            data[schema.primaryKey] = id
            const {error}  =await deleteItem(schema.name,data)
            checkError(error, 'Delete Succeed')
            if (!error){
                await new Promise(r => setTimeout(r, 500));
                window.location.href= `${baseRouter}/${schema.name}`;
            }
        })
    }

    if (isLoading || error){
        return <FetchingStatus isLoading={isLoading} error={error}/>
    }
    return <>
        <Status/>
        <ItemForm {...{data, id, onSubmit, columns,formId,uploadUrl,  getFullAssetsURL}} />
        <Button type={'submit'} label={"Save " + schema.title} icon="pi pi-check" form={formId}/>
        {' '}
        <Button type={'button'} label={"Delete " + schema.title} severity="danger" onClick={onDelete}/>
        {
            subPages.map((column: any) => {
                const props = {schema, data, column,  getFullAssetsURL,baseRouter}
                return <div key={column.field}>
                    <Divider/>
                    { column.displayType === 'junction' && <Junction key={column.field} {...props}/> }
                </div>
            })
        }
    </>
}