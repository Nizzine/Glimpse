#!/bin/bash

if [ $# -ne 1 ]; then
  echo "Usage: build-macos <version>"
  exit 2
fi

VERSION=$1
ICON_NAME="Glimpse"
OUT_DIR="Glimpse.app"

CURRENT_DIR=$(pwd -P)
pushd $(dirname $0)

mkdir $OUT_DIR
mkdir "$OUT_DIR/Contents"
mkdir "$OUT_DIR/Contents/MacOS"
mkdir "$OUT_DIR/Contents/Resources"
cp Info.plist "$OUT_DIR/Contents/"
sed -i '' "s/GLIMPSE_VERSION/$VERSION/" "$OUT_DIR/Contents/Info.plist"

mv "$ICON_NAME.icns" "$OUT_DIR/Contents/Resources"

popd
