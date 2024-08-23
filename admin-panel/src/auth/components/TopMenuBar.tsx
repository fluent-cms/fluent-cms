import {Menubar} from 'primereact/menubar';
import React from "react";
import {useTopMenuBar} from "../services/menu";
import { useNavigate} from "react-router-dom";
import {Profile} from "../types/Profile";


const entityPrefix = '/entities/'

export function TopMenuBar({start, end, profile}:{start:any, end:any, profile: Profile}) {
    const navigate = useNavigate();
    const items = useTopMenuBar().filter(x=>{
        if (profile.roles.includes('sa')){
            return true;
        }

        if (!x.url.startsWith(entityPrefix)){
            return true;
        }

        const entityName = x.url.substring(entityPrefix.length);
        return profile.readWriteEntities.includes(entityName)
            || profile.restrictedReadWriteEntities.includes(entityName)
            || profile.readonlyEntities.includes(entityName)
            || profile.restrictedReadonlyEntities.includes(entityName);
    })

    const links = items.map((x: any)=> x.isHref ? x :(
        {
            icon: 'pi ' + (x.icon === ''?'pi-bolt':x.icon),
            label:x.label,
            command: ()=>{
                navigate(x.url)
            }
        })
    )

    if (profile.roles.includes('admin') || profile.roles.includes('sa')) {
        links.push({
            icon: 'pi pi-sitemap',
            label: 'Roles',
            command: () => {
                navigate('/roles')
            }
        });
        links.push({
            icon: 'pi pi-users',
            label: 'Users',
            command: () => {
                navigate('/users')
            }
        });
        links.push({
            icon: 'pi pi-cog',
            label: 'Schema Builder',
            url: '/schema-ui/list.html'
        });
    }
    return (
        <Menubar model={links} start={start} end={end}/>
    )
}