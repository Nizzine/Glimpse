#!/bin/sh

source "$(dirname $0)/.Common.sh"

prompt "Uninstall Glimpse?"

progress "Uninstalling Glimpse." 3

updateprogress "Removing Glimpse directory..." 0
rm -r "$GLIMPSEDIR"
if [[ $? -ne 0 ]]; then
    closeprogress
    notify "Glimpse is not installed."
    exit 1
fi

updateprogress "Removing icon..." 1
rm "$ICONSDIR/Glimpse.png"

updateprogress "Removing desktop entry..." 2
rm "$APPSDIR/Glimpse.desktop"

updateprogress "Done!" 3
closeprogress

notify "Glimpse is now uninstalled. Sorry to see you go!"
