set -e
export VITE_REACT_APP_API_URL='/api'
export VITE_REACT_APP_ASSET_URL='/files'
export VITE_REACT_APP_VERSION_API_URL='/api/versions'

pnpm build

rsync -azv --progress dist/* ../server/FluentCMS/wwwroot