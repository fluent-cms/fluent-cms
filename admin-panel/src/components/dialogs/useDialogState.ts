import {useState} from "react";


export function useDialogState() {
    const [visible,setVisible ] = useState(false)
    return {
        visible,
        hideDialog: () => {
            setVisible(false)
        },
        showDialog: () => {
            setVisible(true)
        }
    }
}