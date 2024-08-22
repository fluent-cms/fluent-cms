import {useState} from "react";


export function useDialogState() {
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