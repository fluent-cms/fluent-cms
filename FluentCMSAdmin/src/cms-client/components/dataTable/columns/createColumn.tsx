import {textColumn} from "./textColumn";
import {linkColumn} from "./linkColumn";
import {imageColumn} from "./imageColumn";
import {fileColumn} from "./fileColumn";

export function createColumn(props:any) {
    switch (props.column.type){
        case 'link':
            return linkColumn(props)
        case 'image':
        case 'gallery':
            return imageColumn(props)
        case 'file':
            return fileColumn(props)
        default:
            return textColumn(props)
    }
}