set -e
export VITE_REACT_APP_API_URL='/api'
export VITE_REACT_APP_ASSET_URL='/files'
export VITE_REACT_APP_VERSION_API_URL='/api/versions'
export VITE_REACT_APP_AUTH_API_URL='/api'

pnpm build
rm -rf ../server/FluentCMS/wwwroot/assets/
rsync -azv --progress dist/* ../server/FluentCMS/wwwroot
rm -rf ../server/FluentCMS.Blog/wwwroot/assets/
rsync -azv --progress dist/* ../server/FluentCMS.Blog/wwwroot
rm -rf ../server/FluentCMS.App/wwwroot/assets/
rsync -azv --progress dist/* ../server/FluentCMS.App/wwwroot
