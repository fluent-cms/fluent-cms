import {useListData} from "../services/entity";
import {LookupInput} from "../../components/inputs/LookupInput";

export function LookupContainer(props:any){
    const {data }= useListData(props.column.lookup.name,null);
    return <LookupInput items={data?.items??[]} {...props}/>
}