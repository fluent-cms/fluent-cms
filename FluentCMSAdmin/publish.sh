set -e
export VITE_REACT_APP_API_URL='/adapi'
export VITE_REACT_APP_ORY_URL=''
export VITE_REACT_APP_ORY_LOGIN_URL='/auth/login'
export VITE_REACT_APP_LOGS_API_URL='/adapi/versions'

pnpm build

SSH_HOST=bc-dev
ssh $SSH_HOST "mkdir -p ~/web"
ssh $SSH_HOST "rm -rf ~/web"

rsync -azv --progress dist/ $SSH_HOST:~/web;
ssh $SSH_HOST "sudo rsync -av --progress ~/web /data/"