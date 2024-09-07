import {useSchema} from "../services/schema";
import {FetchingStatus} from "../../components/FetchingStatus";
import React from "react";
import {Helmet} from "react-helmet";

interface PageLayoutProps {
    schemaName:string,
    page: React.FC<{schema:any}>;
}
export function PageLayout({schemaName, page:Page}: PageLayoutProps){
    let {data:schema, error, isLoading} = useSchema(schemaName)
    if (isLoading || error) {
        return <FetchingStatus isLoading={isLoading} error={error}/>
    }
    return <>
        <Helmet>
            <title>🚀{schema?.name} - Fluent CMS Admin Panel</title>
        </Helmet>
        <Page schema={schema}/>
    </>
}