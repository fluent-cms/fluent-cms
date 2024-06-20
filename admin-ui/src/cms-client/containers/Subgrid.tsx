import {deleteSubPageItems, saveSubPageItems, useSubPageData} from "../services/entity";
import {createColumn} from "../components/dataTable/columns/createColumn";
import {Button} from "primereact/button";
import {useFormDialogState} from "../components/dialogs/useFormDialogState";
import {SelectDataTable} from "../components/dataTable/SelectDataTable";
import {ListDialog} from "../components/dialogs/ListDialog";
import {userRequestStatus} from "../components/itemForms/userFormStatusUI";
import {useSubSchema} from "./useSubSchema";
import {useLazyStateHandlers} from "./useLazyStateHandlers";

export function Subgrid({column, schemaName, data, schema}: {
    schemaName: any
    data: any,
    column: { field: string, header: string, subTable: any },
    schema: any
}) {
    const {visible, handleShow, handleHide} = useFormDialogState()
    const {
        id,
        listColumns,
        targetSchema,
        existingItems,
        setExistingItems,
        toAddItems,
        setToAddItems
    } = useSubSchema(data, schema, schemaName, column)
    const {lazyState ,eventHandlers}= useLazyStateHandlers(10)
    const {data: subgridData, mutate: subgridMutate} = useSubPageData(schemaName, id, column.field, false, lazyState)

    const {lazyState :excludedLazyState,eventHandlers:excludedEventHandlers}= useLazyStateHandlers(10)
    const {data: excludedSubgridData, mutate: execMutate} = useSubPageData(schemaName, id, column.field, true,excludedLazyState)
    const {checkError, Status, confirm} = userRequestStatus(column.field)

    const mutateDate = () => {
        subgridMutate()
        execMutate()

    }

    const handleSave = async () => {
        await saveSubPageItems(schemaName, id, column.field, toAddItems)
        handleHide()
        mutateDate()
    }

    const onDelete = async () => {
        confirm('Do you want to delete these item?', async () => {
            const res = await deleteSubPageItems(schemaName, id, column.field, existingItems)
            checkError(res, 'Delete Succeed')
            if (!res.err) {
                mutateDate()
            }
        })
    }

    return <div className={'card col-12'}>
        <label id={column.field} className="font-bold">
            {column.header}
        </label><br/>
        <Status/>
        <Button outlined label={'Select ' + column.header} onClick={handleShow} size="small"/>
        {' '}
        <Button type={'button'} label={"Delete "} severity="danger" onClick={onDelete} outlined size="small"/>
        <SelectDataTable
            data={subgridData}
            createColumn={createColumn}
            columns={listColumns}
            dataKey={targetSchema.dataKey}
            selectedItems={existingItems}
            setSelectedItems={setExistingItems}
            lazyState={lazyState}
            eventHandlers={eventHandlers}
        />
        <ListDialog
            visible={visible}
            handleHide={handleHide}
            handleSave={handleSave}
            header={'Select ' + column.header}>
            <SelectDataTable
                data={excludedSubgridData}
                createColumn={createColumn}
                columns={listColumns}
                dataKey={targetSchema.dataKey}
                selectedItems={toAddItems}
                setSelectedItems={setToAddItems}
                lazyState={excludedLazyState}
                eventHandlers={excludedEventHandlers}
            />
        </ListDialog>
    </div>
}
