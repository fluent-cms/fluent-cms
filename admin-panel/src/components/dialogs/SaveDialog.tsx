import {Dialog} from "primereact/dialog";
import {Button} from "primereact/button";

export function SaveDialog({visible, handleHide, formId, handleSave, children, header}: {
    visible: any,
    handleHide: any,
    children: any
    header: string
    formId?: string
    handleSave?: any
}) {
    const productDialogFooter = (
        <>
            <Button label="Cancel" icon="pi pi-times" outlined onClick={handleHide}/>
            {
                formId 
                ? <Button type={'submit'} label={"Save"} icon="pi pi-check" form={formId}/> 
                : <Button label="Save" icon="pi pi-check" onClick={handleSave}/>
            }
        </>
    );

    return <Dialog maximizable visible={visible} style={{width: '90%'}}
                   header={header} modal className="p-fluid" footer={productDialogFooter}
                   onHide={handleHide}>
        {children}
    </Dialog>
}
