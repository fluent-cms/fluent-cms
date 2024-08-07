import {useRef, useState} from "react";
import {Message} from "primereact/message";
import {Toast} from "primereact/toast";
import {confirmDialog, ConfirmDialog} from "primereact/confirmdialog";

export function useRequestStatus(id:any){
    const toastRef = useRef<any>(null);
    const [error, setError] = useState('')
    return {
        checkError :(error :string, succeedMessage:string) =>{
            if (error){
                setError(error)
            }else {
                toastRef.current.show({severity: 'info', summary:succeedMessage})
            }
        },
        confirm:(msg :any, accept:any) =>{
            confirmDialog({
                tagKey:id,
                message: msg,
                header: 'Confirmation',
                icon: 'pi pi-exclamation-triangle',
                accept,
            });
        },
        Status: ()=>{
            return <>
                {error&& <Message severity={'error'} text={error}/> }
                <Toast ref={toastRef} position="top-right" />
                <ConfirmDialog key={id} id={id} tagKey={id}/>
            </>
        }
    }


}