; =========================================================================
; PSD Media Player Pro Edition - Inno Setup Compiler Manifest (.iss)
; Author & Architect: Prabhu Kannan (PSD Technologies)
; Targets: Windows 10 / Windows 11 (x64 / ARM64)
; =========================================================================

#define MyAppName "PSD Media Player Pro"
#define MyAppVersion "2.5.0"
#define MyAppPublisher "Prabhu Kannan (PSD Technologies)"
#define MyAppURL "https://github.com/psd-player/psd-media-player"
#define MyAppExeName "PSDPlayer.exe"
#define MyAppAssocName "PSD Media File"
#define MyAppAssocExt ".mkv"
#define MyAppAssocKey StringChange(MyAppAssocName, " ", "") + MyAppAssocExt

[Setup]
AppId={{D3D11VA-PSD-MEDIA-PLAYER-PRO-2026-X64}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} v{#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
AppCopyright=Copyright (C) 2026 Prabhu Kannan. All Rights Reserved.
DefaultDirName={autopf}\PSD Media Player
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=..\..\..\LICENSE.txt
OutputDir=..\..\..\dist\installer
OutputBaseFilename=PSDPlayer-Pro-Setup-v{#MyAppVersion}-x64
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64 arm64
PrivilegesRequired=lowest
DisableProgramGroupPage=auto
CloseApplications=yes
RestartApplications=no
SetupIconFile=..\..\OmniPlayer.UI\Assets\psd-player-icon.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "associate_video"; Description: "Associate with common video formats (MKV, MP4, AVI, WebM, TS, M3U)"; GroupDescription: "File Associations:"
Name: "associate_audio"; Description: "Associate with common audio formats (FLAC, MP3, WAV, AAC, OGG)"; GroupDescription: "File Associations:"

[Files]
; Main Executables & Binaries from Portable win-x64 distribution
Source: "..\..\..\dist\portable\win-x64\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\..\dist\portable\win-x64\OmniPlayer.exe"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\..\..\dist\portable\win-x64\OmniPlayer.Core.dll"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\..\..\dist\portable\win-x64\Themes\Packs\*.xaml"; DestDir: "{app}\Themes\Packs"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\..\..\dist\portable\win-x64\Assets\*"; DestDir: "{app}\Assets"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist
Source: "..\..\..\dist\portable\win-x64\libvlc\*"; DestDir: "{app}\libvlc"; Flags: ignoreversion recursesubdirs createallsubdirs skipifsourcedoesntexist

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; Context menu "Play with PSD Media Player" shell verb
Root: HKA; Subkey: "Software\Classes\*\shell\PlayWithPSDPlayer"; ValueType: string; ValueName: ""; ValueData: "Play with PSD Media Player Pro"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\*\shell\PlayWithPSDPlayer\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

; Video file associations (MKV, MP4, WebM, AVI, TS)
Root: HKA; Subkey: "Software\Classes\.mkv\OpenWithProgids"; ValueType: string; ValueName: "PSDPlayer.AssocFile.MKV"; ValueData: ""; Flags: uninsdeletevalue; Tasks: associate_video
Root: HKA; Subkey: "Software\Classes\.mp4\OpenWithProgids"; ValueType: string; ValueName: "PSDPlayer.AssocFile.MP4"; ValueData: ""; Flags: uninsdeletevalue; Tasks: associate_video
Root: HKA; Subkey: "Software\Classes\.webm\OpenWithProgids"; ValueType: string; ValueName: "PSDPlayer.AssocFile.WEBM"; ValueData: ""; Flags: uninsdeletevalue; Tasks: associate_video
Root: HKA; Subkey: "Software\Classes\.avi\OpenWithProgids"; ValueType: string; ValueName: "PSDPlayer.AssocFile.AVI"; ValueData: ""; Flags: uninsdeletevalue; Tasks: associate_video
Root: HKA; Subkey: "Software\Classes\.ts\OpenWithProgids"; ValueType: string; ValueName: "PSDPlayer.AssocFile.TS"; ValueData: ""; Flags: uninsdeletevalue; Tasks: associate_video

; Audio file associations (FLAC, MP3, WAV)
Root: HKA; Subkey: "Software\Classes\.flac\OpenWithProgids"; ValueType: string; ValueName: "PSDPlayer.AssocFile.FLAC"; ValueData: ""; Flags: uninsdeletevalue; Tasks: associate_audio
Root: HKA; Subkey: "Software\Classes\.mp3\OpenWithProgids"; ValueType: string; ValueName: "PSDPlayer.AssocFile.MP3"; ValueData: ""; Flags: uninsdeletevalue; Tasks: associate_audio
Root: HKA; Subkey: "Software\Classes\.wav\OpenWithProgids"; ValueType: string; ValueName: "PSDPlayer.AssocFile.WAV"; ValueData: ""; Flags: uninsdeletevalue; Tasks: associate_audio

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
