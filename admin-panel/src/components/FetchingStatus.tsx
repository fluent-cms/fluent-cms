import {ProgressSpinner} from "primereact/progressspinner";
import {Message} from "primereact/message";

export function FetchingStatus({isLoading, error}:{isLoading:boolean, error:string}){
    if (isLoading ) {
        return <ProgressSpinner />
    }
    if (error ){
        return <Message severity={'error'} text={error}/>
    }
    return null
}