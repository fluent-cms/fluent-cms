import { useForm } from "react-hook-form"
import { DropDownInput } from "../../components/inputs/DropDownInput"
import { DatetimeInput } from "../../components/inputs/DatetimeInput"
import { DefaultAttributeNames } from "../types/defaultAttributeNames"
import { PublicationStatus } from "../types/schema"

export function PublicationSettings(
    {formId,data,  onSubmit}:
    {formId:string, data:any,onSubmit:any}
){
    const {
        register,
        handleSubmit,
        control
    } = useForm()
    
    const options = [
        PublicationStatus.Published, 
        PublicationStatus.Scheduled
    ];
   
    
    return <form  onSubmit={handleSubmit(onSubmit)} id={formId}>
        <DropDownInput className="col-12" data={data} column={{
            field: DefaultAttributeNames.PublicationStatus,
            header: "Publication status",
        }} options={options} control={control} register={register} 
                       id={DefaultAttributeNames.PublicationStatus}/>
        <DatetimeInput className="col-12" data={data} column={{
            field: DefaultAttributeNames.PublishedAt,
            header: "Published at",
        }} register={register} control={control} id={'publishedAt'}/>
    </form>
}