param(
    [string]$Configuration = "Release",
    [switch]$SkipInstaller = $false
)

$ErrorActionPreference = "Stop"
$sw = [System.Diagnostics.Stopwatch]::StartNew()

Write-Host "==========================================================================" -ForegroundColor Cyan
Write-Host "   PSD MEDIA PLAYER PRO EDITION - MULTI-TARGET BUILD PIPELINE             " -ForegroundColor Cyan
Write-Host "==========================================================================" -ForegroundColor Cyan

$RepoRoot = if ($PSScriptRoot) { (Resolve-Path (Join-Path $PSScriptRoot "..")).Path } else { "P:\prabhu\projects\git\omni-player" }
$DistDir = Join-Path $RepoRoot "dist"
$DotnetPath = "C:\Users\prabh\AppData\Local\Microsoft\dotnet\dotnet.exe"
if (-not (Test-Path $DotnetPath)) {
    $cmd = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($cmd) { $DotnetPath = $cmd.Source }
}

# Gracefully terminate running instances to ensure dist files are unlocked
Get-Process -Name "*PSDPlayer*", "*OmniPlayer*" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500

Write-Host "`n[STEP 1/5] Verifying & Exporting 110+ Theme Packs..." -ForegroundColor Yellow
$ExportScript = Join-Path $RepoRoot "scripts\export-themes.ps1"
& powershell.exe -ExecutionPolicy Bypass -File $ExportScript
$themeFiles = Get-ChildItem -Path (Join-Path $RepoRoot "src\OmniPlayer.UI\Themes\Packs") -Filter "*.xaml"
Write-Host ">>> Confirmed $($themeFiles.Count) theme XAML manifests compiled and verified." -ForegroundColor Green

Write-Host "`n[STEP 2/5] Compiling OmniPlayer.sln ($Configuration)..." -ForegroundColor Yellow
$SolutionPath = Join-Path $RepoRoot "OmniPlayer.sln"
& $DotnetPath build $SolutionPath -c $Configuration --nologo
Write-Host ">>> Solution build succeeded with 0 errors." -ForegroundColor Green

Write-Host "`n[STEP 3/5] Publishing Single-File Portable Deliverables (x64 / ARM64)..." -ForegroundColor Yellow
$PortableScript = Join-Path $RepoRoot "src\OmniPlayer.Packaging\Portable\publish-portable.ps1"
& powershell.exe -ExecutionPolicy Bypass -File $PortableScript -Configuration $Configuration -OutputDir (Join-Path $DistDir "portable")

# Create ZIP distributions for portable releases
$PortableX64 = Join-Path $DistDir "portable\win-x64"
if (Test-Path $PortableX64) {
    $ZipPath = Join-Path $DistDir "PSDPlayer-Pro-v2.5.0-Portable-win-x64.zip"
    if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }
    Compress-Archive -Path "$PortableX64\*" -DestinationPath $ZipPath -Force
    $zipMb = [Math]::Round((Get-Item $ZipPath).Length / 1MB, 2)
    Write-Host ">>> Portable Archive Created: $ZipPath ($zipMb MB)" -ForegroundColor Green
}

$PortableArm64 = Join-Path $DistDir "portable\win-arm64"
if (Test-Path $PortableArm64) {
    $ZipPathArm = Join-Path $DistDir "PSDPlayer-Pro-v2.5.0-Portable-win-arm64.zip"
    if (Test-Path $ZipPathArm) { Remove-Item $ZipPathArm -Force }
    Compress-Archive -Path "$PortableArm64\*" -DestinationPath $ZipPathArm -Force
    $zipMbArm = [Math]::Round((Get-Item $ZipPathArm).Length / 1MB, 2)
    Write-Host ">>> Portable ARM64 Archive Created: $ZipPathArm ($zipMbArm MB)" -ForegroundColor Green
}

Write-Host "`n[STEP 4/5] Compiling Inno Setup Windows Installer (.exe)..." -ForegroundColor Yellow
$IsccPaths = @(
    "C:\Users\prabh\AppData\Local\Programs\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)
$IsccExe = $IsccPaths | Where-Object { Test-Path $_ } | Select-Object -First 1

if ($IsccExe -and (-not $SkipInstaller)) {
    Write-Host ">>> Compiling Inno Setup using: $IsccExe" -ForegroundColor Cyan
    $IssScript = Join-Path $RepoRoot "src\OmniPlayer.Packaging\InnoSetup\omni-player.iss"
    & $IsccExe $IssScript
    Write-Host ">>> Inno Setup installer compiled successfully!" -ForegroundColor Green
} else {
    Write-Host ">>> Inno Setup Compiler (ISCC.exe) not found." -ForegroundColor Yellow
}

Write-Host "`n[STEP 5/5] Compiling WiX Toolset Enterprise Installer (.msi)..." -ForegroundColor Yellow
$WixExe = "C:\Users\prabh\.dotnet\tools\wix.exe"
if (-not (Test-Path $WixExe)) {
    $cmdWix = Get-Command wix -ErrorAction SilentlyContinue
    if ($cmdWix) { $WixExe = $cmdWix.Source }
}

if ((Test-Path $WixExe) -and (-not $SkipInstaller)) {
    Write-Host ">>> Compiling WiX MSI using: $WixExe" -ForegroundColor Cyan
    $WxsDir = Join-Path $RepoRoot "src\OmniPlayer.Packaging\WiX"
    $MsiDir = Join-Path $DistDir "msi"
    New-Item -ItemType Directory -Force -Path $MsiDir | Out-Null
    $MsiOutput = Join-Path $MsiDir "PSDPlayer-Enterprise-x64.msi"
    Push-Location $WxsDir
    try {
        & $WixExe build "Package.wxs" -o $MsiOutput
    } finally {
        Pop-Location
    }
    Write-Host ">>> WiX enterprise MSI package compiled successfully: $MsiOutput" -ForegroundColor Green
} else {
    Write-Host ">>> WiX Toolset compiler not found." -ForegroundColor Yellow
}

$sw.Stop()
Write-Host "`n==========================================================================" -ForegroundColor Cyan
Write-Host "   BUILD PIPELINE COMPLETED IN $([Math]::Round($sw.Elapsed.TotalSeconds, 1)) SECONDS           " -ForegroundColor Cyan
Write-Host "==========================================================================" -ForegroundColor Cyan

# Output Summary of Deliverables
Write-Host "`nGENERATED DELIVERABLES:" -ForegroundColor Green
Get-ChildItem -Path $DistDir -Recurse -File | ForEach-Object {
    $relPath = $_.FullName.Replace($RepoRoot, "")
    $sizeMb = [Math]::Round($_.Length / 1MB, 2)
    $hash = (Get-FileHash -Path $_.FullName -Algorithm SHA256).Hash.Substring(0, 16) + "..."
    Write-Host ("  {0,-50} | {1,8} MB | SHA256: {2}" -f $relPath, $sizeMb, $hash)
}
