rm docs/index.md
for file in $(ls ../readme-parts/*.md | sort); do
  echo "Adding $file"
  grep -v -e '^<details>' -e '^</details>' -e '^<summary>' -e '^</summary>' -e '^# ' "$file" >> docs/index.md
done

mkdocs build
rm site/sitemap.xml
rm site/sitemap.xml.gz
rm -rf ../../server/fluentcms.blog/wwwroot/doc
cp -r site ../../server/fluentcms.blog/wwwroot/doc