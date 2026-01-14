!include "MUI2.nsh"
!include "FileFunc.nsh"

Name "Glimpse"
OutFile "InstallGlimpse-${VERSION}.exe"
Unicode true
RequestExecutionLevel admin

InstallDir "$PROGRAMFILES64\Glimpse"
InstallDirRegKey HKLM "SOFTWARE\Glimpse" "InstallDir"

!define MUI_WELCOMEPAGE_TEXT "Install or upgrade Glimpse ${VERSION} to your system."
!define MUI_FINISHPAGE_TEXT "Glimpse has been successfully installed!"
!define MUI_FINISHPAGE_RUN "Glimpse.exe"
!define MUI_ABORTWARNING
!define MUI_ICON "GlimpseInstaller.ico"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "..\..\LICENSE"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

Section "Install Glimpse"

    SectionIn RO
    SetOutPath $INSTDIR

    File /r "${PUBLISHDIR}\*.*"

    SetRegView 64
    WriteRegStr HKLM "SOFTWARE\Glimpse" "InstallDir" "$INSTDIR"

    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Glimpse" "DisplayName" "Glimpse"
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Glimpse" "DisplayIcon" "$INSTDIR\Glimpse.exe"
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Glimpse" "InstallLocation" "$INSTDIR"
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Glimpse" "UninstallString" '"$INSTDIR\uninstall.exe"'
    WriteRegDWORD HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Glimpse" "NoModify" 1
    WriteRegDWORD HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Glimpse" "NoRepair" 1
    WriteRegStr HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Glimpse" "URLInfoAbout" "https://glimpseaudio.co.uk"

    WriteUninstaller "$INSTDIR\uninstall.exe"

SectionEnd

Section "Start Menu Shortcuts"

    CreateDirectory "$SMPROGRAMS\Glimpse"
    CreateShortcut "$SMPROGRAMS\Glimpse\Glimpse.lnk" "$INSTDIR\Glimpse.exe"
    CreateShortcut "$SMPROGRAMS\Glimpse\Uninstall.lnk" "$INSTDIR\uninstall.exe"

SectionEnd

Section "Uninstall"

    SetRegView 64
    DeleteRegKey HKLM "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Glimpse"
    DeleteRegKey HKLM "SOFTWARE\Glimpse"

    Delete "$INSTDIR\*.*"
    Delete "$SMPROGRAMS\Glimpse\*.*"

    RMDir "$SMPROGRAMS\Glimpse"
    RMDir "$INSTDIR"

SectionEnd
