import { XAttr, XEntity } from "../../cms-client/types/schemaExt";
import { InputPanel } from "./InputPanel"
import { TreeSelect } from 'primereact/treeselect';

export function TreeSelectInput(
    props: {
        data: any,
        options: any[],
        column: XAttr,
        targetEntity: XEntity,
        register: any
        className: any
        control: any
        id: any
    }) {
    const {column,options,targetEntity} = props
    return <InputPanel  {...props} component={(field: any) => {
        console.log({field})
        return <TreeSelect
            display="chip"
            value={field.value[targetEntity.primaryKey]}
            onChange={(e) => {
                field.onChange(e.value)
            }}
            options={options}
            placeholder={"Select " + column.header} className="w-full"/>
    }}/>
}