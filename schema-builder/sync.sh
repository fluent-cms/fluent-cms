rm -rf ../server/FluentCMS/wwwroot/schema-ui/
cp -a schema-ui ../server/FluentCMS/wwwroot

pushd ../server/FluentCMS/wwwroot || exit
git add .
popd || exit

rm -rf ../server/FluentCMS.Blog/wwwroot/schema-ui/
cp -a schema-ui ../server/FluentCMS.Blog/wwwroot
pushd ../server/FluentCMS.Blog/wwwroot || exit
git add .
popd || exit

rm -rf ../server/FluentCMS.App/wwwroot/schema-ui/
cp -a schema-ui ../server/FluentCMS.App/wwwroot