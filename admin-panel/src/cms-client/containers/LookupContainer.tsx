import {useLookupData, getLookupData} from "../services/entity";
import {LookupInput} from "../../components/inputs/LookupInput";

export function LookupContainer(props:any){
    const {column,data: item} = props;
    const lookup = props.column.lookup;
    
    let val 
    if (item[column.field]){
        val = item[column.field][lookup.titleAttribute];
    }
    
    const {data}= useLookupData(lookup.name, val);
    const search = async (q:string)=>{
        var {data} = await getLookupData(lookup.name, q);
        return data.items;
    };
    
    return <LookupInput search={search} hasMore={data?.hasMore} items={data?.items??[]} {...props}/>
}