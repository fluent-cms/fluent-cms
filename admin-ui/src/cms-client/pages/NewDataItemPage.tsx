import {ItemForm} from "../containers/ItemForm";
import {getLinkToEntity, getWriteColumns} from "../services/columnUtil";
import {addItem} from "../services/entity";
import {Button} from "primereact/button";
import {fileUploadURL, getFullAssetsURL} from "../configs";
import {useRequestStatus} from "../containers/useFormStatus";
import {useParams} from "react-router-dom";
import {PageLayout} from "./PageLayout";

export function NewDataItemPage() {
    const {schemaName} = useParams()
    return <PageLayout schemaName={schemaName??''} page={NewDataItemPageComponent}/>
}

export function NewDataItemPageComponent({schema}:{schema:any}) {
    const {checkError, Status} = useRequestStatus(schema.name)
    const formId = "newForm" + schema.name
    const columns = getWriteColumns(schema)
    const uploadUrl = fileUploadURL()

    const onSubmit = async (formData: any) => {
        const {data, error} = await addItem(schema.name, formData)
        checkError(error, 'saved')
        if (!error) {
            await new Promise(r => setTimeout(r, 500));
            window.location.href = getLinkToEntity(schema.name??'',schema.name??'') + "/" + data[schema.primaryKey]
        }
    }

    return <>
        <Status/>
        <ItemForm {...{data:{}, onSubmit, columns, formId,uploadUrl,  getFileFullURL: getFullAssetsURL}}/>
        <Button label={'Save ' + schema.title} type="submit" form={formId}/>
    </>
}