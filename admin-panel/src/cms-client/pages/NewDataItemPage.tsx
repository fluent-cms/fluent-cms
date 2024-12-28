import {ItemForm} from "../containers/ItemForm";
import {addItem} from "../services/entity";
import {Button} from "primereact/button";
import {fileUploadURL, getFullAssetsURL} from "../services/configs";
import {useRequestStatus} from "../containers/useFormStatus";
import {useParams} from "react-router-dom";
import {PageLayout} from "./PageLayout";

export function NewDataItemPage({baseRouter}:{baseRouter:string}) {
    const {schemaName} = useParams()
    return <PageLayout schemaName={schemaName??''} baseRouter={baseRouter} page={NewDataItemPageComponent}/>
}

export function NewDataItemPageComponent({schema,baseRouter}:{schema:any, baseRouter:string }) {
    const {checkError, Status} = useRequestStatus(schema.name)
    const formId = "newForm" + schema.name
    const uploadUrl = fileUploadURL()

    const onSubmit = async (formData: any) => {
        const {data, error} = await addItem(schema.name, formData)
        checkError(error, 'saved')
        if (!error) {
            await new Promise(r => setTimeout(r, 500));
            window.location.href = `${baseRouter}/${schema.name}/${data[schema.primaryKey]}`;
        }
    }

    return <>
        <Button label={'Save ' + schema.title} type="submit" form={formId}  icon="pi pi-check"/>
        <Status/>
        <ItemForm {...{schema,data:{}, onSubmit,  formId,uploadUrl,  getFullAssetsURL}}/>
    </>
}