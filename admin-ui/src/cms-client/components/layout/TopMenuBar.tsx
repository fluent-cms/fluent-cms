import {Menubar} from 'primereact/menubar';
import React from "react";
import {useTopMenuBar} from "../../services/menu";
import { useNavigate} from "react-router-dom";


export function TopMenuBar({start, end}:{start:any, end:any}) {
    const navigate = useNavigate();
    const items = useTopMenuBar()
    const links = items.map((x: any)=> x.isHref ? x :(
        {
            icon: 'pi ' + x.icon,
            label:x.label,
            command: ()=>{
                navigate(x.url)
            }
        })
    )
    return (
        <Menubar model={links} start={start} end={end}/>
    )
}