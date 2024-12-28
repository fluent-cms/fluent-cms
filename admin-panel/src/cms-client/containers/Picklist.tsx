import {deleteJunctionItems, saveJunctionItems, useJunctionData} from "../services/entity";
import {Button} from "primereact/button";
import {useRequestStatus} from "./useFormStatus";
import {usePicklist} from "./usePicklist";
import {useLazyStateHandlers} from "./useLazyStateHandlers";
import {useDialogState} from "../../components/dialogs/useDialogState";
import {SelectDataTable} from "../../components/dataTable/SelectDataTable";
import {SaveDialog} from "../../components/dialogs/SaveDialog";

export function Picklist({baseRouter,column, data, schema, getFullAssetsURL}: {
    data: any,
    column: { field: string, header: string, junction: any },
    schema: any
    getFullAssetsURL : (arg:string) =>string
    baseRouter:string
}) {
    const {visible, handleShow, handleHide} = useDialogState()
    const {
        id, targetSchema, listColumns,
        existingItems, setExistingItems,
        toAddItems, setToAddItems
    } = usePicklist(data, schema, column)
    
    const {lazyState ,eventHandlers}= useLazyStateHandlers(10, listColumns,"");
    const {data: subgridData, mutate: subgridMutate} = useJunctionData(schema.name, id, column.field, false, lazyState);

    const {lazyState :excludedLazyState,eventHandlers:excludedEventHandlers}= useLazyStateHandlers(10, listColumns,"");
    const {data: excludedSubgridData, mutate: execMutate} = useJunctionData(schema.name, id, column.field, true,excludedLazyState)
    const {checkError, Status, confirm} = useRequestStatus(column.field)

    const mutateDate = () => {
        setExistingItems(null);
        setToAddItems(null)
        subgridMutate()
        execMutate()

    }

    const handleSave = async () => {
        await saveJunctionItems(schema.name, id, column.field, toAddItems)
        handleHide()
        mutateDate()
    }

    const onDelete = async () => {
        confirm('Do you want to delete these item?', async () => {
            const {error} = await deleteJunctionItems(schema.name, id, column.field, existingItems)
            checkError(error, 'Delete Succeed')
            if (!error) {
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
            columns={listColumns}
            primaryKey={targetSchema.primaryKey}
            titleAttribute={targetSchema.titleAttribute}
            selectedItems={existingItems}
            setSelectedItems={setExistingItems}
            lazyState={lazyState}
            eventHandlers={eventHandlers}
            getFullAssetsURL={getFullAssetsURL}
            entityName={targetSchema.name}
            baseRouter={baseRouter}
        />
        <SaveDialog
            visible={visible}
            handleHide={handleHide}
            handleSave={handleSave}
            header={'Select ' + column.header}>
            <SelectDataTable
                entityName={targetSchema.name}
                getFullAssetsURL={getFullAssetsURL}
                data={excludedSubgridData}
                columns={listColumns}
                primaryKey={targetSchema.dataKey}
                titleAttribute={targetSchema.titleAttribute}
                selectedItems={toAddItems}
                setSelectedItems={setToAddItems}
                lazyState={excludedLazyState}
                eventHandlers={excludedEventHandlers}
                baseRouter={baseRouter}
            />
        </SaveDialog>
    </div>
}
