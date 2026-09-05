!include Sections.nsh

Section "Desktop shortcut" SEC_DESKTOP
SectionEnd

Section "Start Menu entry" SEC_STARTMENU
SectionEnd

!macro customInstallMode
  StrCpy $isForceCurrentInstall "1"
!macroend

!macro preInit
  SetRegView 64
  ReadRegStr $0 HKCU "${INSTALL_REGISTRY_KEY}" InstallLocation
  ${If} $0 == ""
    WriteRegExpandStr HKCU "${INSTALL_REGISTRY_KEY}" InstallLocation "$LOCALAPPDATA\DioExtreme\Tunetoon"
  ${EndIf}
  SetRegView 32
  ReadRegStr $0 HKCU "${INSTALL_REGISTRY_KEY}" InstallLocation
  ${If} $0 == ""
    WriteRegExpandStr HKCU "${INSTALL_REGISTRY_KEY}" InstallLocation "$LOCALAPPDATA\DioExtreme\Tunetoon"
  ${EndIf}
!macroend

!macro customPageAfterChangeDir
  !insertmacro MUI_PAGE_COMPONENTS
!macroend

!macro customInit
  SectionSetText 2 ""
!macroend

!macro customInstall
  ${If} ${SectionIsSelected} ${SEC_DESKTOP}
    CreateShortCut "$newDesktopLink" "$appExe" "" "$appExe" 0 "" "" "${APP_DESCRIPTION}"
    ClearErrors
    WinShell::SetLnkAUMI "$newDesktopLink" "${APP_ID}"
  ${EndIf}
  ${If} ${SectionIsSelected} ${SEC_STARTMENU}
    CreateShortCut "$newStartMenuLink" "$appExe" "" "$appExe" 0 "" "" "${APP_DESCRIPTION}"
    ClearErrors
    WinShell::SetLnkAUMI "$newStartMenuLink" "${APP_ID}"
  ${EndIf}
  System::Call 'Shell32::SHChangeNotify(i 0x8000000, i 0, i 0, i 0)'
!macroend

!macro customUnInstall
  ${ifNot} ${isKeepShortcuts}
    WinShell::UninstShortcut "$oldDesktopLink"
    Delete "$oldDesktopLink"
    WinShell::UninstShortcut "$oldStartMenuLink"
    Delete "$oldStartMenuLink"
  ${endIf}
!macroend
