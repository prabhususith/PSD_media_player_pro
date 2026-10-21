param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$sw = [System.Diagnostics.Stopwatch]::StartNew()

Write-Host "==========================================================================" -ForegroundColor Cyan
Write-Host "   PSD MEDIA PLAYER PRO (ANDROID EDITION) - APK BUILD PIPELINE            " -ForegroundColor Cyan
Write-Host "   Target: Xiaomi Redmi Note 4 / Android 6.0 (API 23) through Android 15   " -ForegroundColor Cyan
Write-Host "==========================================================================" -ForegroundColor Cyan

$AndroidDir = if ($PSScriptRoot) { (Resolve-Path (Join-Path $PSScriptRoot "..")).Path } else { (Get-Location).Path }
$DistDir = Join-Path $AndroidDir "..\dist\apk"
New-Item -ItemType Directory -Force -Path $DistDir | Out-Null

$env:JAVA_HOME = "C:\Program Files\Microsoft\jdk-17.0.19.10-hotspot"
$env:ANDROID_HOME = "C:\Users\prabh\AppData\Local\Android\Sdk"
$env:ANDROID_SDK_ROOT = "C:\Users\prabh\AppData\Local\Android\Sdk"

Push-Location $AndroidDir

Write-Host "`n[STEP 1/4] Cleaning previous build artifacts..." -ForegroundColor Yellow
$GradleBat = Join-Path $AndroidDir "gradlew.bat"
& $GradleBat clean --no-daemon

Write-Host "`n[STEP 2/4] Compiling Android Application with Gradle (assembleRelease & assembleDebug)..." -ForegroundColor Yellow
& $GradleBat assembleRelease assembleDebug --no-daemon

if ($LASTEXITCODE -ne 0) {
    Write-Error "Gradle build failed with exit code $LASTEXITCODE"
    Pop-Location
    exit 1
}

Write-Host "`n[STEP 3/4] Validating APK Signature Schemes (v1 & v2)..." -ForegroundColor Yellow
$BuildReleaseDir = Join-Path $AndroidDir "app\build\outputs\apk\release"
$BuildDebugDir = Join-Path $AndroidDir "app\build\outputs\apk\debug"

$ReleaseApk = Get-ChildItem -Path $BuildReleaseDir -Filter "*.apk" | Select-Object -First 1
$DebugApk = Get-ChildItem -Path $BuildDebugDir -Filter "*.apk" | Select-Object -First 1

if (-not $ReleaseApk) {
    Write-Error "No Release APK was generated in $BuildReleaseDir"
    Pop-Location
    exit 1
}

$ApkSigner = "C:\Users\prabh\AppData\Local\Android\Sdk\build-tools\34.0.0\apksigner.bat"
if (Test-Path $ApkSigner) {
    Write-Host ">>> Running apksigner verification (Release)..." -ForegroundColor Cyan
    & $ApkSigner verify --verbose $ReleaseApk.FullName
    if ($DebugApk) {
        Write-Host ">>> Running apksigner verification (Debug)..." -ForegroundColor Cyan
        & $ApkSigner verify --verbose $DebugApk.FullName
    }
} else {
    Write-Host ">>> apksigner not found at $ApkSigner" -ForegroundColor Yellow
}

Write-Host "`n[STEP 4/4] Staging Release Deliverables to dist/apk/..." -ForegroundColor Yellow
$RedmiApk = Join-Path $DistDir "PSDPlayer-Pro-v2.5.0-RedmiNote4-Ready.apk"
$UniversalApk = Join-Path $DistDir "PSDPlayer-Pro-v2.5.0-Release-universal.apk"
$RedmiDebugApk = Join-Path $DistDir "PSDPlayer-Pro-v2.5.0-RedmiNote4-Debug.apk"

Copy-Item -Path $ReleaseApk.FullName -Destination $RedmiApk -Force
Copy-Item -Path $ReleaseApk.FullName -Destination $UniversalApk -Force
if ($DebugApk) {
    Copy-Item -Path $DebugApk.FullName -Destination $RedmiDebugApk -Force
}

$sizeMb = [Math]::Round((Get-Item $RedmiApk).Length / 1MB, 2)
$hash = (Get-FileHash -Path $RedmiApk -Algorithm SHA256).Hash

Write-Host ">>> Redmi Note 4 Ready APK: $RedmiApk" -ForegroundColor Green
Write-Host ">>> Universal Release APK:   $UniversalApk" -ForegroundColor Green
if (Test-Path $RedmiDebugApk) {
    $debugHash = (Get-FileHash -Path $RedmiDebugApk -Algorithm SHA256).Hash
    Write-Host ">>> Redmi Note 4 Debug APK: $RedmiDebugApk" -ForegroundColor Green
    Write-Host ">>> Debug APK SHA-256:      $debugHash" -ForegroundColor Green
}
Write-Host ">>> File Size:               $sizeMb MB ($((Get-Item $RedmiApk).Length) bytes)" -ForegroundColor Green
Write-Host ">>> SHA-256 Checksum:        $hash" -ForegroundColor Green

Pop-Location
$sw.Stop()

Write-Host "`n==========================================================================" -ForegroundColor Cyan
Write-Host "   BUILD & VERIFICATION COMPLETED IN $([Math]::Round($sw.Elapsed.TotalSeconds, 1)) SECONDS" -ForegroundColor Cyan
Write-Host "==========================================================================" -ForegroundColor Cyan
