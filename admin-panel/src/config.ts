
export const configs = {
    apiURL: import.meta.env.VITE_REACT_APP_API_URL,
    authAPIURL: import.meta.env.VITE_REACT_APP_AUTH_API_URL,
    assetURL: import.meta.env.VITE_REACT_APP_ASSET_URL,
    entityBaseRouter: '/_content/FormCMS/admin/entities',
    authBaseRouter: '/_content/FormCMS/admin/auth',
    adminBaseRouter: '/_content/FormCMS/admin',
}
console.log({configs})