import {Button} from "primereact/button";
import { useDialogState } from "../../components/dialogs/useDialogState";
import { useEditTable } from "./useEditTable";
import {useLazyStateHandlers} from "./useLazyStateHandlers";
import {addCollectionItem, useCollectionData} from "../services/entity";
import { SaveDialog } from "../../components/dialogs/SaveDialog";
import { ItemForm } from "./ItemForm";
import { fileUploadURL } from "../services/configs";
import {Message} from "primereact/message";
import {Toast} from "primereact/toast";
import {useRef, useState} from "react";
import { LazyDataTable } from "../../components/dataTable/LazyDataTable";


export function EditTable({baseRouter,column, data, schema, getFullAssetsURL}: {
    data: any,
    column: { field: string, header: string, collection: any },
    schema: any
    getFullAssetsURL : (arg:string) =>string
    baseRouter:string
}) {
    const {visible, showDialog, hideDialog} = useDialogState()
    const { id, targetSchema, listColumns, inputColumns} = useEditTable(data, schema, column)
    const formId = "edit-table" + column.field;
    const toastRef = useRef<any>(null);
    const [error, setError] = useState('')
    
    const {lazyState ,eventHandlers}= useLazyStateHandlers(10, listColumns,"");
    const {data:collectionData,mutate} = useCollectionData(schema.name, id, column.field, lazyState);
    
    const onSubmit =async (formData: any) => {
        const {error} = await addCollectionItem(schema.name,id,column.field,formData)
        if (!error){
            hideDialog();
            toastRef.current.show({severity: 'info', summary:"success"})
            setError("");
            mutate();
        }else {
            setError(error);
        }
    }
    

    return <div className={'card col-12'}>
        <Toast ref={toastRef} position="top-right" />
        <label id={column.field} className="font-bold">
            {column.header}
        </label><br/>
        <Button outlined label={'Add ' + column.header} onClick={showDialog} size="small"/>
        {' '}
        <LazyDataTable columns={listColumns} schema={targetSchema} data={collectionData} {...{baseRouter,eventHandlers, lazyState,  getFullAssetsURL}}/>

        <SaveDialog
            formId={formId}
            visible={visible}
            handleHide={hideDialog}
            header={'Add ' + column.header}>
            <>
                {error && error.split('\n').map(e => (<><Message severity={'error'} text={e}/>&nbsp;&nbsp;</>))}
                <ItemForm
                    formId={formId}
                    uploadUrl={fileUploadURL()}
                    columns={inputColumns}
                    data={{}}
                    getFullAssetsURL={getFullAssetsURL}
                    onSubmit={onSubmit}/>
            </>
        </SaveDialog>
    </div>
}
