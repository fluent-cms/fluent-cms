import {useParams} from "react-router-dom";
import {ItemForm} from "../containers/ItemForm";
import {deleteItem, updateItem, useItemData} from "../services/entity";
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

export function DataItemPage({baseRouter}: { baseRouter: string }) {
    const {schemaName} = useParams()
    return <PageLayout schemaName={schemaName ?? ''} baseRouter={baseRouter} page={DataItemPageComponent}/>
}

export function DataItemPageComponent({schema, baseRouter}: { schema: XEntity, baseRouter: string }) {
    const {id} = useParams()
    const {data, error, isLoading} = useItemData(schema.name, id)
    const {checkError, CheckErrorStatus} = useCheckError();
    const {confirm, Confirm} = useConfirm("dataItemPage" + schema.name);
    const ref = new URLSearchParams(location.search).get("ref");

    const uploadUrl = fileUploadURL()
    const tables = schema?.attributes?.filter(attr =>
        attr.displayType === DisplayType.Picklist
        || attr.displayType == DisplayType.EditTable) ?? []
    const trees = schema.attributes.filter(x => x.displayType == DisplayType.Tree);
    const formId = "editForm" + schema.name

    const inputColumns = schema?.attributes?.filter(
        (x) => {
            return x.inDetail && !x.isDefault && x.dataType != "collection" && x.dataType != "junction";
        }
    ) ?? [];

    const onSubmit = async (formData: any) => {
        formData[schema.primaryKey] = id
        const {error} = await updateItem(schema.name, formData)
        checkError(error, 'Save Succeed')
    }

    const onDelete = async () => {
        confirm('Do you want to delete this item?', async () => {
            data[schema.primaryKey] = id
            const {error} = await deleteItem(schema.name, data)
            checkError(error, 'Delete Succeed')
            if (!error) {
                await new Promise(r => setTimeout(r, 500));
                window.location.href = ref ?? `${baseRouter}/${schema.name}`;
            }
        })
    }

    if (isLoading || error) {
        return <FetchingStatus isLoading={isLoading} error={error}/>
    }
    return <>
        <Button type={'submit'} label={"Save " + schema.title} icon="pi pi-check" form={formId}/>
        {' '}
        <Button type={'button'} label={"Delete " + schema.title} severity="danger" onClick={onDelete}/>
        <CheckErrorStatus/>
        <Confirm/>
        <div className="grid">
            <div className={`col-12 md:col-12 lg:${trees.length > 0? "col-9":"col-12"}`}>
                <ItemForm columns={inputColumns} {...{
                    schema,
                    data,
                    id,
                    onSubmit,
                    formId,
                    uploadUrl,
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
    </>
}