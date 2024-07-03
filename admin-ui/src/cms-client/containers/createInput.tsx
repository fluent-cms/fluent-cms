import {TextInput} from "../components/inputs/TextInput";
import {DropDownInput} from "../components/inputs/DropDownInput";
import {EditorInput} from "../components/inputs/EditorInput";
import {FileInput} from "../components/inputs/FileInput";
import {TextAreaInput} from "../components/inputs/TextAreaInput";
import {DatetimeInput} from "../components/inputs/DatetimeInput";
import {MultiSelectInput} from "../components/inputs/MultiSelectInput";
import {GalleryInput} from "../components/inputs/GalleryInput";
import {LookupContainer} from "./LookupContainer";
import {DateInput} from "../components/inputs/DateInput";
import {NumberInput} from "../components/inputs/NumberInput";

export function createInput(props :any) {
    const {type, field} = props.column
    switch (type) {
        case 'text':
            return <TextInput className={'field col-12 md:col-4'} key={field} {...props}/>
        case 'textarea':
            return <TextAreaInput className={'field col-12  md:col-4'} key={field} {...props}/>
        case 'editor':
            return <EditorInput className={'field col-12'} key={field} {...props}/>

        case 'number':
            return <NumberInput className={'field col-12 md:col-4'} key={field} {...props}/>

        case 'datetime':
            return <DatetimeInput className={'field col-12  md:col-4'} key={field} {...props}/>
        case 'date':
            return <DateInput className={'field col-12  md:col-4'} key={field} {...props}/>

        case 'image':
            return <FileInput previewImage className={'field col-12  md:col-4'} key={field} {...props}/>
        case 'gallery':
            return <GalleryInput className={'field col-12  md:col-4'} key={field} {...props}/>
        case 'file':
            return <FileInput download className={'field col-12  md:col-4'} key={field} {...props}/>

        case 'dropdown':
            return <DropDownInput className={'field col-12 md:col-4'} key={field}{...props}/>
        case 'lookup':
            return <LookupContainer className={'field col-12 md:col-4'} key={field}{...props}/>
        case 'multiselect':
            return <MultiSelectInput className={'field col-12  md:col-4'} key={field} {...props}/>
        default:
            return <TextInput className={'field col-12 md:col-4'} key={field} {...props}/>
    }
}

