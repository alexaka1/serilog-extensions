#!/usr/bin/env bash

PACKAGE_JSON_PATH="$1"
CSPROJ_PATH="$2"
PACKAGE_NAME="$3"

set -e
yarn run changeset:version
VERSION=$(jq -r '.version' "$PACKAGE_JSON_PATH")
sed -i "s#<Version>.*</Version>#<Version>$VERSION</Version>#" "$CSPROJ_PATH"
AVAILABLE_VERSIONS=$(curl -s "https://api.nuget.org/v3-flatcontainer/$PACKAGE_NAME/index.json" | jq -r '.versions[]')
if echo "$AVAILABLE_VERSIONS" | grep -q "^$VERSION$"; then
  echo "versionExists=true" >> "$GITHUB_OUTPUT"
else
  echo "versionExists=false" >> "$GITHUB_OUTPUT"
fi
