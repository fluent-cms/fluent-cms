import {useParams} from "react-router-dom";
import {ItemForm} from "../containers/ItemForm";
import {deleteItem, updateItem, useItemData,publish,unpublish, savePublicationSettings} from "../services/entity";
import {Divider} from "primereact/divider";
import {Button} from "primereact/button";
import {Picklist} from "../containers/Picklist";
import {useCheckError} from "../../components/useCheckError";
import {useConfirm} from "../../components/useConfirm";
import {fileUploadURL, getFullAssetsURL} from "../services/configs";
import {PageLayout} from "./PageLayout";
import {FetchingStatus} from "../../components/FetchingStatus";
import {EditTable} from "../containers/EditTable";
import {TreeContainer} from "../containers/TreeContainer";
import {DisplayType, XEntity} from "../types/schemaExt";
import { SaveDialog } from "../../components/dialogs/SaveDialog";
import { useDialogState } from "../../components/dialogs/useDialogState";
import { PublicationSettings } from "../containers/PublicationSettings";
import { DefaultAttributeNames } from "../types/defaultAttributeNames";
import { PublicationStatus } from "../types/schema";

export function DataItemPage({baseRouter}: { baseRouter: string }) {
    const {schemaName} = useParams()
    return <PageLayout schemaName={schemaName ?? ''} baseRouter={baseRouter} page={DataItemPageComponent}/>
}

export function DataItemPageComponent({schema, baseRouter}: { schema: XEntity, baseRouter: string }) {
    const {id} = useParams()
    const {data, error: useItemError, isLoading,mutate} = useItemData(schema.name, id)
    const {handleErrorOrSuccess, CheckErrorStatus} = useCheckError();
    const {handleErrorOrSuccess: checkDialogErorr, CheckErrorStatus: CheckDialogErrorStatus} = useCheckError();
    
    const {confirm, Confirm} = useConfirm("dataItemPage" + schema.name);
    const {visible, showDialog, hideDialog} = useDialogState()
    
    const referingUrl = new URLSearchParams(location.search).get("ref");
   
    const itemEditFormId = "editForm" + schema.name
    const itemPublicationFormId = "publication" + schema.name

    const inputColumns = schema?.attributes?.filter(
        (x) => {
            return x.inDetail && !x.isDefault && x.dataType != "collection" && x.dataType != "junction";
        }
    ) ?? [];
    
    const tables = schema?.attributes?.filter(attr =>
        attr.displayType === DisplayType.Picklist
        || attr.displayType == DisplayType.EditTable) ?? []
    
    const trees = schema.attributes.filter(x => x.displayType == DisplayType.Tree);


    const onSubmit = async (formData: any) => {
        formData[schema.primaryKey] = id
        const {error} = await updateItem(schema.name, formData)
        await handleErrorOrSuccess(error, 'Save Succeed', null)
    }

    const onDelete = async () => {
        confirm('Do you want to delete this item?', async () => {
            data[schema.primaryKey] = id
            const {error} = await deleteItem(schema.name, data)
            await handleErrorOrSuccess(error, 'Delete Succeed', ()=> {
                window.location.href = referingUrl ?? `${baseRouter}/${schema.name}`
            });
        })
    }
    const changePublicationStatus =  () => {
        showDialog();
    }

    const onPublish = async () => {
        const {error} = await publish(schema.name, data)
        await handleErrorOrSuccess(error, 'Publish Succeed', mutate)
    }
    
    const onUnpublish = async () => {
        const {error} = await unpublish(schema.name, data)
        await handleErrorOrSuccess(error, 'Publish Succeed',mutate)
    }
    
    const onSubmitPublicationSettings = async (formData:any) =>{
        formData[schema.primaryKey] = data[schema.primaryKey];
        const {error} = await savePublicationSettings(schema.name, formData)
        await checkDialogErorr(error, 'Publish Succeed', ()=>{
            mutate();
            hideDialog();
        })
    }
    
    if (isLoading || useItemError) {
        return <FetchingStatus isLoading={isLoading} error={useItemError}/>
    }
    return <>
        <Button type={'submit'} label={"Save " + schema.displayName} icon="pi pi-check" form={itemEditFormId}/>
        {' '}
        {
            data && data[DefaultAttributeNames.PublicationStatus] === PublicationStatus.Published
            ?<Button type={'button'} label={"Unpublish"}  onClick={onUnpublish}/>
                : <Button type={'button'} label={"Publish"}  onClick={onPublish}/>
        }
        {' '}
        <Button type={'button'} label={"Publish Settings"}  onClick={changePublicationStatus}/>
        {' '}
        <Button type={'button'} label={"Delete " + schema.displayName} severity="danger" onClick={onDelete}/>
        <CheckErrorStatus/>
        <Confirm/>
        <div className="grid">
            <div className={`col-12 md:col-12 lg:${trees.length > 0? "col-9":"col-12"}`}>
                <ItemForm uploadUrl={fileUploadURL()} formId={itemEditFormId} columns={inputColumns} {...{
                    schema,
                    data,
                    id,
                    onSubmit,
                    getFullAssetsURL
                }} />
                {
                    tables.map((column) => {
                        const props = {schema, data, column, getFullAssetsURL, baseRouter}
                        return <div key={column.field}>
                            <Divider/>
                            {column.displayType === 'picklist' && <Picklist key={column.field} {...props}/>}
                            {column.displayType === 'editTable' && <EditTable key={column.field} {...props}/>}
                        </div>
                    })
                }
            </div>
            {trees.length > 0 &&  <div className="col-12 md:col-12 lg:col-3">
                {
                    trees.map((column) => {
                        return <div key={column.field}>
                            <TreeContainer key={column.field} entity={schema} data={data}
                                           column={column}></TreeContainer>
                            <Divider/>
                        </div>
                    })
                }
            </div>
            }
        </div>
        <SaveDialog
            formId={itemPublicationFormId}
            visible={visible}
            handleHide={hideDialog}
            header={'Save '}>
            <CheckDialogErrorStatus/>
            <PublicationSettings onSubmit={onSubmitPublicationSettings} data={data} formId={itemPublicationFormId}/>
        </SaveDialog>
    </>
}