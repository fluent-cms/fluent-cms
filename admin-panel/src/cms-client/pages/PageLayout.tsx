import {useSchema} from "../services/schema";
import {FetchingStatus} from "../../components/FetchingStatus";
import React from "react";
import {Helmet} from "react-helmet";

interface PageLayoutProps {
    baseRouter:string,
    schemaName:string,
    page: React.FC<{baseRouter:string,schema:any}>;
}
export function PageLayout({baseRouter,schemaName, page:Page}: PageLayoutProps){
    let {data:schema, error, isLoading} = useSchema(schemaName)
    if (isLoading || error) {
        return <FetchingStatus isLoading={isLoading} error={error}/>
    }
    return <>
        <Helmet>
            <title>🚀{schema?.name} - Fluent CMS Admin Panel</title>
        </Helmet>
        <Page baseRouter={baseRouter} schema={schema}/>
    </>
}