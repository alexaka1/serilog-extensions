#!/usr/bin/env bash

PACKAGE_JSON_PATH="$1"
CSPROJ_PATH="$2"

set -e
bun run changeset:version
VERSION=$(jq -r '.version' "$PACKAGE_JSON_PATH")
sed -i "s#<Version>.*</Version>#<Version>$VERSION</Version>#" "$CSPROJ_PATH"
