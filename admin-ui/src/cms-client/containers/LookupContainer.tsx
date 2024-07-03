import {LookupInput} from "../components/inputs/LookupInput";
import {useListData} from "../services/entity";

export function LookupContainer(props:any){
    const data = useListData(props.column.lookup.entityName,null);
    return <LookupInput items={data?.items??[]} {...props}/>
}