import {useState} from "react";


export function useFormDialogState() {
    const [visible,setVisible ] = useState(false)
    return {
        visible,
        handleHide: () => {
            setVisible(false)
        },
        handleShow: () => {
            setVisible(true)
        }
    }
}