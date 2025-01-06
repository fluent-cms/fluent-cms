import {useRef, useState} from "react";
import {Message} from "primereact/message";
import {Toast} from "primereact/toast";

export function useCheckError(){
    const toast = useRef<any>(null);
    const [error, setError] = useState('')
    return {
        checkError :(error :string, succeedMessage:string) =>{
            if (error){
                setError(error)
            }else {
                toast.current.show({severity: 'success', summary:succeedMessage})
            }
        },
        CheckErrorStatus: ()=>{
            return <>
                <Toast ref={toast} />
                {error&& error.split('\n').map(e =>(<><Message severity={'error'} text={e}/>&nbsp;&nbsp;</>))}
            </>
        }
    }


}