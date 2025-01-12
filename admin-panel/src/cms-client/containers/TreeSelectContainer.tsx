import {TreeSelectInput} from "../../components/inputs/TreeSelectInput";
import {XAttr} from "../types/schemaExt";
import { useTree } from "./useTree";

export function TreeSelectContainer(
    {
        data: item, column, id, className, control, register
    }: {
        data: any, column: XAttr, id: any, control: any, register: any, className: string
    }) {

    const targetEntity = column.lookup!
    const options = useTree(targetEntity)


    return <TreeSelectInput
        options={options ?? []}
        data={item}
        targetEntity={targetEntity}
        column={column}
        control={control}
        className={className}
        register={register}
        id={id}
    />
}