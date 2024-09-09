#!/bin/bash

rm readme.md
for file in $(ls doc/readme-parts/*.md | sort); do
  cat "$file" >> readme.md
done