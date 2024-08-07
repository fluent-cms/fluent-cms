import {Dialog} from "primereact/dialog";
import {Button} from "primereact/button";

export function ListDialog({visible, handleHide,handleSave, children,header}: {
    visible: any,
    handleHide: any,
    handleSave:any
    children:any
    header:string
}) {
    const productDialogFooter = (
        <>
            <Button label="Cancel" icon="pi pi-times" outlined onClick={handleHide}/>
            <Button label="Save" icon="pi pi-check" onClick={handleSave}/>
        </>
    );

    return <Dialog maximizable visible={visible} style={{width: '90%'}}
                   header={header} modal className="p-fluid" footer={productDialogFooter}
                   onHide={handleHide}>
        {children}
    </Dialog>
}
