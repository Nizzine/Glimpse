#!/bin/sh

APPNAME="Glimpse"
SHAREDIR="$HOME/.local/share"
GLIMPSEDIR="$SHAREDIR/$APPNAME"
APPSDIR="$SHAREDIR/applications"
ICONSDIR="$SHAREDIR/icons/hicolor/512x512/apps"

INTERACTIVE=true
if [[ $1 == "-s" ]]; then
    INTERACTIVE=false
fi

prompt()
{
    if [ $INTERACTIVE = true ]; then
        kdialog --title "$APPNAME" --yesno "$1" || exit 0
    else
	read -p "$1 [Y/n] " yn
	if [[ ${yn,,} != "y" ]]; then
            exit 0
        fi
    fi
}

notify()
{
    if [ $INTERACTIVE = true ]; then
        kdialog --title $APPNAME --msgbox "$1"
    else
        echo "$1"
    fi
}

progress()
{
    if [ $INTERACTIVE = true ]; then
        dbusref=$(kdialog --title $APPNAME --progressbar "$1" $2)
    fi
}

updateprogress()
{
    if [ $INTERACTIVE = true ]; then
        qdbus $dbusref setLabelText "$1"
        qdbus $dbusref Set "" value $2
    else
        echo "$1"
    fi
}

closeprogress()
{
    if [ $INTERACTIVE = true ]; then
        qdbus $dbusref close
    fi
}
