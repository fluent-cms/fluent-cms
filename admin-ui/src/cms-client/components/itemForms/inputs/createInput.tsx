import {TextInput} from "./TextInput";
import {DropDownInput} from "./DropDownInput";
import {EditorInput} from "./EditorInput";
import {FileInput} from "./FileInput";
import {TextAreaInput} from "./TextAreaInput";
import {DatetimeInput} from "./DatetimeInput";
import {MultiSelectInput} from "./MultiSelectInput";
import {GalleryInput} from "./GalleryInput";
import {LookupDownInput} from "./LookupInput";

export function createInput(props :any) {
    const {type, field} = props.column
    switch (type) {
        case 'dropdown':
            return <DropDownInput className={'field col-12 md:col-4'} key={field}{...props}/>
        case 'lookup':
            return <LookupDownInput className={'field col-12 md:col-4'} key={field}{...props}/>
        case 'editor':
            return <EditorInput className={'field col-12'} key={field} {...props}/>
        case 'image':
            return <FileInput previewImage className={'field col-12  md:col-4'} key={field} {...props}/>
        case 'file':
            return <FileInput download className={'field col-12  md:col-4'} key={field} {...props}/>
        case 'textarea':
            return <TextAreaInput className={'field col-12  md:col-4'} key={field} {...props}/>
        case 'datetime':
            return <DatetimeInput className={'field col-12  md:col-4'} key={field} {...props}/>
        case 'multiselect':
            return <MultiSelectInput className={'field col-12  md:col-4'} key={field} {...props}/>
        case 'gallery':
            return <GalleryInput className={'field col-12  md:col-4'} key={field} {...props}/>
        default:
            return <TextInput className={'field col-12 md:col-4'} key={field} {...props}/>
    }
}

