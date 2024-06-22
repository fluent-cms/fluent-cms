import {deleteSubPageItems, saveSubPageItems, useSubPageData} from "../services/entity";
import {Button} from "primereact/button";
import {FormDialog} from "../components/dialogs/FormDialog";
import {useFormDialogState} from "../components/dialogs/useFormDialogState";
import {SelectDataTable} from "../components/dataTable/SelectDataTable";

import {ItemForm} from "../components/itemForms/ItemForm";
import {fileUploadURL} from "../configs";
import {userRequestStatus} from "../components/itemForms/userFormStatusUI";
import {useSubSchema} from "./useSubSchema";
import {useLazyStateHandlers} from "./useLazyStateHandlers";

export function Sublist({column, schemaName, data, schema, getFullURL}: {
    schemaName: any
    data: any,
    column: { field: string, header: string, subTable: any },
    schema: any
    getFullURL : (arg:string) =>string
}) {
    const uploadUrl=fileUploadURL()
    const {visible, handleShow, handleHide} = useFormDialogState()
    const {id,listColumns,formColumns,formId,targetSchema, existingItems, setExistingItems} =
        useSubSchema(data, schema, schemaName,column)
    const {lazyState ,eventHandlers}= useLazyStateHandlers(10)
    const {data:subgridData, mutate} = useSubPageData(schemaName, id, column.field, false,lazyState)
    const {checkError, Status, confirm} = userRequestStatus(column.field)

    const onSubmit = async (formData:any) => {
        await saveSubPageItems(schemaName,id, column.field, [formData])
        handleHide()
        mutate()
    }

    const onDelete = async () => {
        confirm('Do you want to delete these item?', async () => {
            const res = await deleteSubPageItems(schemaName, id, column.field, existingItems)
            checkError(res, 'Delete Succeed')
            if (!res.err) {
                mutate()
            }
        })
    }
    return <div className={'card col-12'}>
        <label id={column.field} className="font-bold">
            {column.header}
        </label><br/>
        <Status/>
        <Button outlined label={'Create ' + column.header} onClick={handleShow} size="small"/>
        {' '}
        <Button type={'button'} label={"Delete " } severity="danger" onClick={onDelete} outlined size="small" />
        <SelectDataTable
            data={subgridData}
            primaryKey={targetSchema.primaryKey}
            titleAttribute={targetSchema.titleAttribute}
            columns={listColumns}
            selectedItems={existingItems}
            setSelectedItems={setExistingItems}
            lazyState={lazyState}
            eventHandlers={eventHandlers}
        />
        <FormDialog
            visible={visible}
            handleHide={handleHide}
            formId={formId}
            header={'Create ' + column.header}>
            <ItemForm  {...{data,columns:formColumns, onSubmit, formId,uploadUrl, getFullURL }} />
        </FormDialog>
    </div>
}
