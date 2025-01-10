set -e
export VITE_REACT_APP_API_URL='/api'
export VITE_REACT_APP_ASSET_URL='/files'
export VITE_REACT_APP_VERSION_API_URL='/api/versions'
export VITE_REACT_APP_AUTH_API_URL='/api'
pnpm build

rm -rf ../server/FormCMS/wwwroot/admin
rsync -azv --progress dist/* ../server/FormCMS/wwwroot/admin
pushd ../server/FormCMS/wwwroot/admin
git add .
popd