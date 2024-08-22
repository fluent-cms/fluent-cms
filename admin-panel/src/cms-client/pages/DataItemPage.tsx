import {useParams} from "react-router-dom";
import {ItemForm} from "../containers/ItemForm";
import {deleteItem, updateItem, useItemData} from "../services/entity";
import {Divider} from "primereact/divider";
import {getLinkToEntity, getSubPageColumns, getWriteColumns} from "../services/columnUtil";
import {Button} from "primereact/button";
import {Crosstable} from "../containers/Crosstable";
import {useRequestStatus} from "../containers/useFormStatus";
import {fileUploadURL, getFullAssetsURL} from "../services/configs";
import {PageLayout} from "./PageLayout";
import {FetchingStatus} from "../../components/FetchingStatus";

export function DataItemPage() {
    const {schemaName} = useParams()
    return <PageLayout schemaName={schemaName??''} page={DataItemPageComponent}/>
}

export function DataItemPageComponent({schema}:{schema:any}) {
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
                window.location.href= getLinkToEntity(schema.name??'', schema.name??'')
            }
        })
    }

    if (isLoading || error){
        return <FetchingStatus isLoading={isLoading} error={error}/>
    }
    return <>
        <Status/>
        <ItemForm {...{data, id, onSubmit, columns,formId,uploadUrl,  getFileFullURL:getFullAssetsURL}} />
        <Button type={'submit'} label={"Save " + schema.title} icon="pi pi-check" form={formId}/>
        {' '}
        <Button type={'button'} label={"Delete " + schema.title} severity="danger" onClick={onDelete}/>
        {
            subPages.map((column: any) => {
                const props = {schema, data, column,  getFullURL:getFullAssetsURL}
                return <div key={column.field}>
                    <Divider/>
                    { column.type === 'crosstable' && <Crosstable key={column.field} {...props}/> }
                </div>
            })
        }
    </>
}