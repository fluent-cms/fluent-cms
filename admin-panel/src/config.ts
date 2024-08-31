
export const configs = {
    apiURL: import.meta.env.VITE_REACT_APP_API_URL,
    authAPIURL: import.meta.env.VITE_REACT_APP_AUTH_API_URL,
    assetURL: import.meta.env.VITE_REACT_APP_ASSET_URL,
    versionAPIURL: import.meta.env.VITE_REACT_APP_VERSION_API_URL,
    entityBaseRouter: '/admin/entities',
    authBaseRouter: '/admin/auth',
    adminBaseRouter: '/admin',
}
console.log({configs})