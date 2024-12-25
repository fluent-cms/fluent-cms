import {useLookupData, getLookupData} from "../services/entity";
import {LookupInput} from "../../components/inputs/LookupInput";

export function LookupContainer(props:any){
    var lookup = props.column.lookup;
    var val = props.data[props.column.field][lookup.titleAttribute];
    const {data}= useLookupData(lookup.name, val);
    const search = async (q:string)=>{
        var {data} = await getLookupData(lookup.name, q);
        return data.items;
    };
    
    return <LookupInput search={search} hasMore={data?.hasMore} items={data?.items??[]} {...props}/>
}