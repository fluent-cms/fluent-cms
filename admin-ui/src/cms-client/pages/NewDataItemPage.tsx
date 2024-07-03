import { useParams} from "react-router-dom";
import {useSchema} from "../services/schema";
import {ItemForm} from "../containers/ItemForm";
import {getLinkToEntity, getWriteColumns} from "../services/columnUtil";
import {addItem} from "../services/entity";
import {Button} from "primereact/button";
import {fileUploadURL, getFullAssetsURL} from "../configs";
import {useRequestStatus} from "../containers/useFormStatusUI";

export function NewDataItemPage() {
    const uploadUrl = fileUploadURL()

    const {schemaName, id} = useParams()
    const formId = "newForm" + schemaName
    const schema = useSchema(schemaName)
    const data = {}
    const columns = getWriteColumns(schema)
    const {checkError, Status} = useRequestStatus(schemaName)

    const onSubmit = async (formData: any) => {
        const res = await addItem(schemaName, formData)
        checkError(res, 'saved')
        if (!res.err) {
            await new Promise(r => setTimeout(r, 500));
            window.location.href = getLinkToEntity(schemaName??'',schemaName??'') + "/" + res.data
        }
    }

    return <>
        <Status/>
        <ItemForm {...{data, id, onSubmit, columns, formId,uploadUrl,  getFileFullURL: getFullAssetsURL}}/>
        <Button label={'Save ' + schema.title} type="submit" form={formId}/>
    </>
}