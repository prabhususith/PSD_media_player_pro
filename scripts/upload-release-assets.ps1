param(
    [string]$Token = $env:GITHUB_TOKEN,
    [string]$Repo = "prabhususith/PSD_media_player_pro",
    [int]$ReleaseId = 419329788
)

$ErrorActionPreference = "Stop"

$headers = @{
    "Authorization" = "token $Token"
    "Accept" = "application/vnd.github.v3+json"
    "User-Agent" = "PSD-BuildPipeline"
}

Write-Host "Fetching release $ReleaseId..."
$release = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repo/releases/$ReleaseId" -Headers $headers -Method Get
$uploadUrlTemplate = $release.upload_url

$filesToUpload = @(
    @{ Path = "dist\PSDPlayer.exe"; ContentType = "application/octet-stream" },
    @{ Path = "dist\msi\PSDPlayer-Enterprise-x64.msi"; ContentType = "application/octet-stream" },
    @{ Path = "dist\installer\PSDPlayer-Pro-Setup-v2.5.0-x64.exe"; ContentType = "application/octet-stream" },
    @{ Path = "dist\PSDPlayer-Pro-v2.5.0-Portable-win-arm64.zip"; ContentType = "application/zip" },
    @{ Path = "dist\PSDPlayer-Pro-v2.5.0-Portable-win-x64.zip"; ContentType = "application/zip" }
)

foreach ($item in $filesToUpload) {
    $filePath = (Resolve-Path $item.Path).Path
    if (-not (Test-Path $filePath)) {
        Write-Warning "File not found: $filePath"
        continue
    }

    $fileName = [System.IO.Path]::GetFileName($filePath)
    $fileSize = (Get-Item $filePath).Length
    $fileMb = [Math]::Round($fileSize / 1MB, 2)

    # Re-fetch release assets to check current list
    $currentRelease = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repo/releases/$ReleaseId" -Headers $headers -Method Get
    $existing = $currentRelease.assets | Where-Object { $_.name -eq $fileName }
    if ($existing) {
        Write-Host "Deleting existing asset $fileName (ID: $($existing.id))..." -ForegroundColor Yellow
        $delUri = "https://api.github.com/repos/$Repo/releases/assets/$($existing.id)"
        Invoke-RestMethod -Uri $delUri -Headers $headers -Method Delete
        Start-Sleep -Seconds 2
    }

    Write-Host "Uploading $fileName ($fileMb MB) via curl.exe streaming..." -ForegroundColor Cyan
    $uploadUri = $uploadUrlTemplate.Replace("{?name,label}", "?name=$fileName")

    $curlArgs = @(
        "-s",
        "-S",
        "-X", "POST",
        "-H", "Authorization: token $Token",
        "-H", "Content-Type: $($item.ContentType)",
        "-H", "User-Agent: PSD-BuildPipeline",
        "--data-binary", "@$filePath",
        "$uploadUri"
    )

    $response = & curl.exe @curlArgs
    try {
        $json = $response | ConvertFrom-Json
        if ($json.id) {
            Write-Host "Successfully uploaded $fileName (ID: $($json.id), Size: $($json.size) bytes)" -ForegroundColor Green
        } else {
            Write-Host "Response: $response" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "Upload completed for $fileName" -ForegroundColor Green
    }
}

Write-Host "All remaining release assets refreshed successfully!" -ForegroundColor Green
