param(
    [string]$Configuration = "Release",
    [string]$OutputDir = ""
)

$ErrorActionPreference = "Stop"
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " PSD Media Player Pro: Building Single-File Portable Packages" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$RepoRoot = if ($PSScriptRoot) { (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path } else { (Get-Location).Path }
if (-not $OutputDir) {
    $OutputDir = Join-Path $RepoRoot "dist\portable"
}

$DotnetPath = "C:\Users\prabh\AppData\Local\Microsoft\dotnet\dotnet.exe"
if (-not (Test-Path $DotnetPath)) {
    $DotnetPath = (Get-Command dotnet -ErrorAction SilentlyContinue).Source
}

$ProjectFile = Join-Path $RepoRoot "src\OmniPlayer.UI\OmniPlayer.UI.csproj"
$Rids = @("win-x64", "win-arm64")

foreach ($rid in $Rids) {
    $TargetFolder = Join-Path $OutputDir $rid
    Write-Host "`n>>> Publishing Single-File Portable for [$rid] -> $TargetFolder" -ForegroundColor Yellow

    Get-Process | Where-Object { $_.ProcessName -like "*PSDPlayer*" -or $_.ProcessName -like "*OmniPlayer*" } | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 500

    if (Test-Path $TargetFolder) {
        try {
            Remove-Item -Recurse -Force $TargetFolder -ErrorAction Stop
        } catch {
            Write-Host "Warning: Could not completely remove $TargetFolder, cleaning contents..." -ForegroundColor Yellow
            Get-ChildItem -Path $TargetFolder -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -Recurse -ErrorAction SilentlyContinue
        }
    }
    New-Item -ItemType Directory -Force -Path $TargetFolder | Out-Null

    & $DotnetPath publish $ProjectFile `
        -c $Configuration `
        -r $rid `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -o $TargetFolder

    # Bundle theme packs
    $ThemesSource = Join-Path $RepoRoot "src\OmniPlayer.UI\Themes\Packs"
    $ThemesDest = Join-Path $TargetFolder "Themes\Packs"
    if (Test-Path $ThemesSource) {
        New-Item -ItemType Directory -Force -Path $ThemesDest | Out-Null
        Copy-Item -Path "$ThemesSource\*.xaml" -Destination $ThemesDest -Force
    }

    # Bundle Assets (logos, icons)
    $AssetsSource = Join-Path $RepoRoot "src\OmniPlayer.UI\Assets"
    $AssetsDest = Join-Path $TargetFolder "Assets"
    if (Test-Path $AssetsSource) {
        New-Item -ItemType Directory -Force -Path $AssetsDest | Out-Null
        Copy-Item -Path "$AssetsSource\*" -Destination $AssetsDest -Force
    }

    # Bundle libvlc native libraries & plugins
    $VlcArch = if ($rid -like "*arm64*") { "win-arm64" } else { "win-x64" }
    $VlcSource = Join-Path $RepoRoot "src\OmniPlayer.UI\bin\$Configuration\net8.0-windows\libvlc\$VlcArch"
    if (-not (Test-Path $VlcSource)) {
        $VlcSource = Join-Path $RepoRoot "src\OmniPlayer.UI\bin\Debug\net8.0-windows\libvlc\$VlcArch"
    }
    if (Test-Path $VlcSource) {
        $VlcDest = Join-Path $TargetFolder "libvlc\$VlcArch"
        New-Item -ItemType Directory -Force -Path $VlcDest | Out-Null
        Copy-Item -Recurse -Path "$VlcSource\*" -Destination $VlcDest -Force
    }

    $ExePath = Join-Path $TargetFolder "OmniPlayer.exe"
    $PsdExe = Join-Path $TargetFolder "PSDPlayer.exe"
    if (Test-Path $ExePath) {
        Copy-Item -Path $ExePath -Destination $PsdExe -Force
        $sizeMb = [Math]::Round((Get-Item $PsdExe).Length / 1MB, 2)
        Write-Host "[SUCCESS] Single-file portable executable ready: $PsdExe ($sizeMb MB)" -ForegroundColor Green
    }
}

Write-Host "`n[COMPLETE] All portable targets generated in $OutputDir" -ForegroundColor Green
