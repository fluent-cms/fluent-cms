import {Column} from "primereact/column";

export function fileColumn({column}: {
    column: {
        field: any,
        header: any,
    },
}) {
    const bodyTemplate = (item: any) => {
        const fullURL = item[column.field]
        return <a href={ fullURL}>Download</a>
    };
    return <Column key={column.field} field={column.field} header={column.header} body={bodyTemplate}></Column>
}
