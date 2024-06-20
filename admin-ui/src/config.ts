
export const configs = {
    apiURL: import.meta.env.VITE_REACT_APP_API_URL,
    assetURL: import.meta.env.VITE_REACT_APP_ASSET_URL,
    logsAPIURL: import.meta.env.VITE_REACT_APP_LOGS_API_URL,
    entityBaseRouter: '/entities',
    editorBaseRouter: '/editor'
}
console.log({configs})