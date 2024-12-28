import {textColumn} from "./textColumn";
import {imageColumn} from "./imageColumn";
import {fileColumn} from "./fileColumn";

export function createColumn(props:any) {
    switch (props.column.displayType){
        case 'image':
        case 'gallery':
            return imageColumn(props)
        case 'file':
            return fileColumn(props)
        default:
            return textColumn(props)
    }
}