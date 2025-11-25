!include "MUI2.nsh"
!include "FileFunc.nsh"

Name "Glimpse"
OutFile "InstallGlimpse.exe"
Unicode true
RequestExecutionLevel admin

InstallDir "$PROGRAMFILES64\Glimpse"
InstallDirRegKey HKLM "SOFTWARE\Glimpse" "InstallDir"

!define MUI_WELCOMEPAGE_TEXT "Install Glimpse <version>"
!define MUI_FINISHPAGE_TEXT "Glimpse is installed."
!define MUI_FINISHPAGE_RUN "Glimpse.exe"
!define MUI_ABORTWARNING

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "..\..\LICENSE"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_LANGUAGE "English"

Section "Install Glimpse"

    SectionIn RO
    SetOutPath $INSTDIR

    File /r "..\..\Publish\*.*"

SectionEnd