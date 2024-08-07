import {Column} from "primereact/column";

export function fileColumn({column, getFullURL}: {
    column: {
        field: any,
        header: any,
    },
    getFullURL : (arg:string) =>string
}) {
    const bodyTemplate = (item: any) => {
        const fullURL = getFullURL(item[column.field]);
        return <a href={ fullURL}>Download</a>;
    };
    return <Column key={column.field} field={column.field} header={column.header} body={bodyTemplate}></Column>
}
