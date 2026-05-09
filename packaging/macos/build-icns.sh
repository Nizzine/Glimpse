#!/bin/bash

if [ $# -ne 1 ]; then
    echo "Usage: ./build-icns <filename>"
    exit 2
fi

ICON_FILE="$1"
ICON_NAME="${ICON_FILE##*/}"
ICON_NAME="${ICON_NAME%.*}"
ICON_DIR="$ICON_NAME.iconset"

mkdir $ICON_DIR

sips -z 16 16 $ICON_FILE --out $ICON_DIR/icon_16x16.png
sips -z 32 32 $ICON_FILE --out $ICON_DIR/icon_16x16@2.png
sips -z 32 32 $ICON_FILE --out $ICON_DIR/icon_32x32.png
sips -z 64 64 $ICON_FILE --out $ICON_DIR/icon_32x32@2.png
sips -z 64 64 $ICON_FILE --out $ICON_DIR/icon_64x64.png
sips -z 128 128 $ICON_FILE --out $ICON_DIR/icon_64x64@2.png
sips -z 128 128 $ICON_FILE --out $ICON_DIR/icon_128x128.png
sips -z 256 256 $ICON_FILE --out $ICON_DIR/icon_128x128@2.png
sips -z 256 256 $ICON_FILE --out $ICON_DIR/icon_256x256.png
sips -z 512 512 $ICON_FILE --out $ICON_DIR/icon_256x256@2.png
sips -z 512 512 $ICON_FILE --out $ICON_DIR/icon_512x512.png
iconutil -c icns $ICON_DIR

rm -r $ICON_DIR
