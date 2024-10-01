import {Menubar} from 'primereact/menubar';
import React from "react";
import {useTopMenuBar} from "../auth/services/menu";
import { useNavigate} from "react-router-dom";
import {Profile} from "../auth/types/Profile";
import {configs} from "../config";
import {RoleRoute, UserRoute} from "../auth/AccountRouter";


const entityPrefix = '/entities'
export const  MenuSchemaBuilder = "menu_schema_builder";
export const  MenuUsers = "menu_users";
export const  MenuRoles = "menu_roles";

export function TopMenuBar({start, end, profile}:{start:any, end:any, profile: Profile}) {
    const navigate = useNavigate();
    const items = useTopMenuBar().filter(x=>{
        if (profile.roles.includes('sa')){
            return true;
        }

        if (!x.url.startsWith(entityPrefix)){
            return true;
        }

        const entityName = x.url.substring(entityPrefix.length + 1);
        return profile?.readWriteEntities?.includes(entityName)
            || profile?.restrictedReadWriteEntities?.includes(entityName)
            || profile?.readonlyEntities?.includes(entityName)
            || profile?.restrictedReadonlyEntities?.includes(entityName);
    })

    const links = items.map((x: any)=> x.isHref ? x :(
        {
            url:  x.url.replaceAll(entityPrefix, configs.entityBaseRouter),
            icon: 'pi ' + (x.icon === ''?'pi-bolt':x.icon),
            label:x.label,
            command: ()=>{
                navigate(x.url)
            }
        })
    );

    [
        {
            key: MenuRoles,
            icon: 'pi pi-sitemap',
            label: 'Roles',
            command: () => {
                navigate(`${configs.authBaseRouter}${RoleRoute}`)
            }
        },
        {
            key: MenuUsers,
            icon: 'pi pi-users',
            label: 'Users',
            command: () => {
                navigate(`${configs.authBaseRouter}${UserRoute}`)
            }
        },
        {
            key: MenuSchemaBuilder,
            icon: 'pi pi-cog',
            label: 'Schema Builder',
            url: 'schema-ui/list.html'
        }
    ].forEach(x=>{
        if (profile?.allowedMenus?.includes(x.key)){
            links.push(x)
        }
    });
    return (
        <Menubar model={links} start={start} end={end}/>
    )
}