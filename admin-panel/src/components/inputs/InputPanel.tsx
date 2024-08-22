import {Controller} from "react-hook-form";

export function InputPanel({data, column,control, className,id, component}: {
    data: any,
    column: { field: string, header: string },
    control: any
    className: any
    register: any
    id:any
    component: any
}) {
    const defaultValue = data[column.field]
    //!id means this is in create mode, node need to wait for data; otherwise have to wait for data ready*/
    return (!id || Object.keys(data).length > 0) && <div className={className}>
        <label id={column.field} htmlFor={column.field}>
            {column.header}
        </label>
        <Controller
            defaultValue={defaultValue}
            name={column.field}
            control={control}
            render={({field}) => component(field, className)}
        />
    </div>
}