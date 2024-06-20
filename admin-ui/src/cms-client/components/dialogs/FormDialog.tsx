import {Dialog} from "primereact/dialog";
import {Button} from "primereact/button";

export function FormDialog({visible, handleHide,formId, children,header}: {
    visible: any,
    handleHide: any,
    children:any
    header:string
    formId:any
}) {
    const productDialogFooter = (
        <>
            <Button label="Cancel" icon="pi pi-times" outlined onClick={handleHide}/>
            <Button label="Save" type={'submit'} icon="pi pi-check" form ={formId}/>
        </>
    );

    return <Dialog maximizable visible={visible} style={{width: '90%'}}
                   header={header} modal className="p-fluid" footer={productDialogFooter}
                   onHide={handleHide}>
        {children}
    </Dialog>
}
