import {ItemForm} from "../containers/ItemForm";
import { getWriteColumns} from "../services/columnUtil";
import {addItem} from "../services/entity";
import {Button} from "primereact/button";
import {fileUploadURL, getFullAssetsURL} from "../services/configs";
import {useRequestStatus} from "../containers/useFormStatus";
import {useParams} from "react-router-dom";
import {PageLayout} from "./PageLayout";

export function NewDataItemPage({base}:{base:string}) {
    const {schemaName} = useParams()
    return <PageLayout schemaName={schemaName??''} base={base} page={NewDataItemPageComponent}/>
}

export function NewDataItemPageComponent({schema,base}:{schema:any, base:string }) {
    console.log(base);

    const {checkError, Status} = useRequestStatus(schema.name)
    const formId = "newForm" + schema.name
    const columns = getWriteColumns(schema)
    const uploadUrl = fileUploadURL()

    const onSubmit = async (formData: any) => {
        const {data, error} = await addItem(schema.name, formData)
        checkError(error, 'saved')
        if (!error) {
            await new Promise(r => setTimeout(r, 500));
            window.location.href = `${base}/${schema.name}/${data[schema.primaryKey]}`;
        }
    }

    return <>
        <Status/>
        <ItemForm {...{data:{}, onSubmit, columns, formId,uploadUrl,  getFileFullURL: getFullAssetsURL}}/>
        <Button label={'Save ' + schema.title} type="submit" form={formId}/>
    </>
}