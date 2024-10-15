
export const configs = {
    apiURL: import.meta.env.VITE_REACT_APP_API_URL,
    authAPIURL: import.meta.env.VITE_REACT_APP_AUTH_API_URL,
    assetURL: import.meta.env.VITE_REACT_APP_ASSET_URL,
    entityBaseRouter: '/_content/FluentCMS/admin/entities',
    authBaseRouter: '/_content/FluentCMS/admin/auth',
    adminBaseRouter: '/_content/FluentCMS/admin',
}
console.log({configs})