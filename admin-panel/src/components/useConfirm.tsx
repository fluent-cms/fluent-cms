import {confirmDialog, ConfirmDialog} from "primereact/confirmdialog";

export function useConfirm(id:any){
    return {
        confirm:(msg :any, accept:any) =>{
            confirmDialog({
                tagKey:id,
                message: msg,
                header: 'Confirmation',
                icon: 'pi pi-exclamation-triangle',
                accept,
            });
        },
        Confirm: ()=>{
            return <>
                <ConfirmDialog key={id} id={id} tagKey={id}/>
            </>
        }
    }
}