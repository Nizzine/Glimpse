#!/bin/bash

if [ $# -ne 1 ]; then
  echo "Usage: build-macos <version>"
  exit 2
fi

VERSION=$1
GLIMPSE_ICON="../../src/Glimpse/Assets/Icons/Glimpse.png"
ICON_NAME="Glimpse"
ICON_DIR="$ICON_NAME.iconset"
OUT_DIR="Glimpse.app"

CURRENT_DIR=$(pwd -P)
pushd $(dirname $0)

mkdir $OUT_DIR
mkdir "$OUT_DIR/Contents"
mkdir "$OUT_DIR/Contents/MacOS"
mkdir "$OUT_DIR/Contents/Resources"
cp Info.plist "$OUT_DIR/Contents/"
sed -i '' "s/GLIMPSE_VERSION/$VERSION/" "$OUT_DIR/Contents/Info.plist"

mkdir $ICON_DIR
sips -z 16 16 $GLIMPSE_ICON --out $ICON_DIR/icon_16x16.png
sips -z 32 32 $GLIMPSE_ICON --out $ICON_DIR/icon_16x16@2.png
sips -z 32 32 $GLIMPSE_ICON --out $ICON_DIR/icon_32x32.png
sips -z 64 64 $GLIMPSE_ICON --out $ICON_DIR/icon_32x32@2.png
sips -z 64 64 $GLIMPSE_ICON --out $ICON_DIR/icon_64x64.png
sips -z 128 128 $GLIMPSE_ICON --out $ICON_DIR/icon_64x64@2.png
sips -z 128 128 $GLIMPSE_ICON --out $ICON_DIR/icon_128x128.png
sips -z 256 256 $GLIMPSE_ICON --out $ICON_DIR/icon_128x128@2.png
sips -z 256 256 $GLIMPSE_ICON --out $ICON_DIR/icon_256x256.png
sips -z 512 512 $GLIMPSE_ICON --out $ICON_DIR/icon_256x256@2.png
sips -z 512 512 $GLIMPSE_ICON --out $ICON_DIR/icon_512x512.png
iconutil -c icns $ICON_DIR
rm -r $ICON_DIR

mv "$ICON_NAME.icns" "$OUT_DIR/Contents/Resources"

popd