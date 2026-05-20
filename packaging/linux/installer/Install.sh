#!/bin/sh

source "$(dirname $0)/.Common.sh"
APPDIR="$(dirname $0)/bin"
SCRIPT_DIR="$(dirname $0)"

prompt "Install/Upgrade Glimpse to $GLIMPSEDIR?"
progress "Installing Glimpse" 5

updateprogress "Removing existing Glimpse directory (if it exists)." 0
rm -rf $GLIMPSEDIR || exit 1

updateprogress "Creating Glimpse directory." 1
mkdir -p $GLIMPSEDIR || exit 1

updateprogress "Copying files..." 2
cp -r "$APPDIR/." $GLIMPSEDIR || exit 1

updateprogress "Copying icon..." 3
mkdir -p $ICONSDIR || exit 1
cp "$SCRIPT_DIR/Glimpse.png" $ICONSDIR || exit 1

updateprogress "Creating desktop entry..." 4
mkdir -p $APPSDIR || exit 1
mv "$SCRIPT_DIR/Glimpse.desktop" "$APPSDIR/" || exit 1

updateprogress "Done!" 5
closeprogress

notify "Glimpse has been installed."
