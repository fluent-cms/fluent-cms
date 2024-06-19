import {useNavigate, useParams, useSearchParams} from "react-router-dom";
import {formatCompareData, saveVersion, useVersionCompare} from "../services/version";
import {Button} from "primereact/button";
import { MonacoDiffEditor } from 'react-monaco-editor';
import React, {useRef, useState} from "react";
import {Toast} from "primereact/toast";
import {compareParam} from "../types";
import qs from "qs";

function versionTitle({id, createdAt}  :{ id: number, createdAt:string}){
    const d = new Date(createdAt)
    return `Version: ${id} -- ${d.toDateString()} ${d.toTimeString()}`
}

export function Compare() {
    const navigate = useNavigate()
    const toastRef = useRef<any>(null);

    const {tableName, recordID} = useParams()
    const [searchParams, setSearchParams] = useSearchParams()
    const [newVal, setNewVal] = useState<string>()

    let data = useVersionCompare(getVersions(searchParams))
    const[old, current] = formatCompareData(data)

    async function save() {
        const newVersion = await saveVersion({tableName, recordID},newVal)
        const param: compareParam = {
            current:newVersion.id,
            old:current.id.toString()
        }
        toastRef.current.show({severity: 'info', summary:'saved new version'})
        navigate('?' + qs.stringify(param))
    }

    function getVersions(searchParam: any ) {
        const param = Object.fromEntries(searchParams) as compareParam;
        const ret = []
        if (param.old){
            ret.push(param.old)
        }
        ret.push(param.current)
        return ret
    }

    function changed(){
        return newVal && current && newVal != current.value
    }

    async function copy(){
        await navigator.clipboard.writeText(newVal??current.value)
        toastRef.current.show({severity: 'info', summary:'copied to clipboard'})
    }

    return data && <>
        <Toast ref={toastRef} position="top-right" />
        <div className="flex flex-wrap align-items-center justify-content-between">
            <div style={{textAlign: 'left'}}><h5>{versionTitle(old)}</h5></div>
            <div style={{marginLeft: 'auto', textAlign: 'right'}}>
                <h5>{versionTitle(current)}</h5>
                <Button onClick={copy}>Copy</Button>
                <Button onClick={save} disabled={!changed()}>Save </Button>
            </div>
        </div>
        <MonacoDiffEditor
            height={'90vh'}
            width={'100vw'}
            language="json"
            original={old.value}
            value={current.value}
            onChange={(e) => {
                setNewVal(e)
            }}
        />
    </>
}