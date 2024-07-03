import {TextInput} from "../components/itemForms/TextInput";
import {DropDownInput} from "../components/itemForms/DropDownInput";
import {EditorInput} from "../components/itemForms/EditorInput";
import {FileInput} from "../components/itemForms/FileInput";
import {TextAreaInput} from "../components/itemForms/TextAreaInput";
import {DatetimeInput} from "../components/itemForms/DatetimeInput";
import {MultiSelectInput} from "../components/itemForms/MultiSelectInput";
import {GalleryInput} from "../components/itemForms/GalleryInput";
import {LookupInput} from "../components/itemForms/LookupInput";
import {LookupContainer} from "./LookupContainer";

export function createInput(props :any) {
    const {type, field} = props.column
    switch (type) {
        case 'dropdown':
            return <DropDownInput className={'field col-12 md:col-4'} key={field}{...props}/>
        case 'lookup':
            return <LookupContainer className={'field col-12 md:col-4'} key={field}{...props}/>
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

