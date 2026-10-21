param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$sw = [System.Diagnostics.Stopwatch]::StartNew()

Write-Host "==========================================================================" -ForegroundColor Cyan
Write-Host "   PSD MEDIA PLAYER PRO (ANDROID EDITION) - APK BUILD PIPELINE            " -ForegroundColor Cyan
Write-Host "==========================================================================" -ForegroundColor Cyan

$AndroidDir = if ($PSScriptRoot) { (Resolve-Path (Join-Path $PSScriptRoot "..")).Path } else { (Get-Location).Path }
$DistDir = Join-Path $AndroidDir "..\dist\apk"
New-Item -ItemType Directory -Force -Path $DistDir | Out-Null

$env:JAVA_HOME = "C:\Program Files\Microsoft\jdk-17.0.19.10-hotspot"
$env:ANDROID_HOME = "C:\Users\prabh\AppData\Local\Android\Sdk"
$env:ANDROID_SDK_ROOT = "C:\Users\prabh\AppData\Local\Android\Sdk"

Push-Location $AndroidDir

Write-Host "`n[STEP 1/3] Compiling Android Application with Gradle..." -ForegroundColor Yellow
$GradleBat = Join-Path $AndroidDir "gradlew.bat"
& $GradleBat assembleRelease --no-daemon

if ($LASTEXITCODE -ne 0) {
    Write-Error "Gradle build failed with exit code $LASTEXITCODE"
    Pop-Location
    exit 1
}

Write-Host "`n[STEP 2/3] Collecting and Staging Release APK Deliverables..." -ForegroundColor Yellow
$BuildApkDir = Join-Path $AndroidDir "app\build\outputs\apk\release"
$TargetApk = Join-Path $DistDir "PSDPlayer-Pro-v2.5.0-Release-universal.apk"

$ReleaseApk = Get-ChildItem -Path $BuildApkDir -Filter "*.apk" | Select-Object -First 1

if ($ReleaseApk) {
    Copy-Item -Path $ReleaseApk.FullName -Destination $TargetApk -Force
    $sizeMb = [Math]::Round((Get-Item $TargetApk).Length / 1MB, 2)
    $hash = (Get-FileHash -Path $TargetApk -Algorithm SHA256).Hash

    Write-Host ">>> Release APK Ready: $TargetApk" -ForegroundColor Green
    Write-Host ">>> File Size: $sizeMb MB ($((Get-Item $TargetApk).Length) bytes)" -ForegroundColor Green
    Write-Host ">>> SHA-256 Checksum: $hash" -ForegroundColor Green
} else {
    Write-Error "No APK was generated in $BuildApkDir"
}

Pop-Location
$sw.Stop()

Write-Host "`n==========================================================================" -ForegroundColor Cyan
Write-Host "   ANDROID BUILD COMPLETED IN $([Math]::Round($sw.Elapsed.TotalSeconds, 1)) SECONDS           " -ForegroundColor Cyan
Write-Host "==========================================================================" -ForegroundColor Cyan
