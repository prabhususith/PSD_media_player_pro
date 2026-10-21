param(
    [string]$DeviceSerial = ""
)

Write-Host "==========================================================================" -ForegroundColor Cyan
Write-Host "   PSD MEDIA PLAYER PRO - REDMI NOTE 4 / ANDROID ADB INSTALLER             " -ForegroundColor Cyan
Write-Host "==========================================================================" -ForegroundColor Cyan

$AdbPath = "C:\Users\prabh\AppData\Local\Android\Sdk\platform-tools\adb.exe"
if (-not (Test-Path $AdbPath)) {
    $AdbPath = (Get-Command adb -ErrorAction SilentlyContinue).Source
}

if (-not $AdbPath) {
    Write-Error "ADB executable not found. Ensure Android SDK platform-tools are installed."
    exit 1
}

$AndroidDir = if ($PSScriptRoot) { (Resolve-Path (Join-Path $PSScriptRoot "..")).Path } else { (Get-Location).Path }
$ApkPath = Join-Path $AndroidDir "..\dist\apk\PSDPlayer-Pro-v2.5.0-Release-universal.apk"

if (-not (Test-Path $ApkPath)) {
    $ApkPath = Join-Path $AndroidDir "app\build\outputs\apk\release\app-release.apk"
}

if (-not (Test-Path $ApkPath)) {
    Write-Error "Release APK not found. Please run build-android-apk.ps1 first."
    exit 1
}

Write-Host "Checking connected Android devices via ADB..." -ForegroundColor Yellow
$devices = & $AdbPath devices
Write-Host $devices

$serialArg = if ($DeviceSerial) { @("-s", $DeviceSerial) } else { @() }

Write-Host "`nInstalling $ApkPath to connected device..." -ForegroundColor Cyan
& $AdbPath @serialArg install -r -d $ApkPath

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n>>> Installation successful! Launching PSD Media Player Pro..." -ForegroundColor Green
    & $AdbPath @serialArg shell monkey -p com.psdtechnologies.psdplayer -c android.intent.category.LAUNCHER 1
} else {
    Write-Host "`n[NOTICE] Ensure USB Debugging is enabled on your device (Settings -> Developer Options -> USB Debugging)." -ForegroundColor Yellow
}
