
export const configs = {
    apiURL: import.meta.env.VITE_REACT_APP_API_URL,
    assetURL: import.meta.env.VITE_REACT_APP_ASSET_URL,
    versionAPIURL: import.meta.env.VITE_REACT_APP_VERSION_API_URL,
    entityBaseRouter: '/entities',
    editorBaseRouter: '/editor'
}
console.log({configs})