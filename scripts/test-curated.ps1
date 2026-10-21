Add-Type -AssemblyName System.Net.Http
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]'Tls12,Tls13'

$csFile = "src\OmniPlayer.Core\LiveTv\LiveTvManager.cs"
$content = Get-Content $csFile -Raw

$pattern = '\(\s*"([^"]+)"\s*,\s*"(https?://[^"]+)"'
$matches = [regex]::Matches($content, $pattern)
Write-Host "Found $($matches.Count) channel entries..." -ForegroundColor Cyan

$client = [System.Net.Http.HttpClient]::new()
$client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36")
$client.Timeout = [TimeSpan]::FromSeconds(3)

$working = @()
$failed = @()

foreach ($m in $matches) {
    $name = $m.Groups[1].Value
    $url = $m.Groups[2].Value

    try {
        $res = $client.GetAsync($url).GetAwaiter().GetResult()
        $code = [int]$res.StatusCode
        if ($code -ge 200 -and $code -lt 400) {
            $working += [PSCustomObject]@{ Name = $name; Url = $url; Code = $code }
        } else {
            $failed += [PSCustomObject]@{ Name = $name; Url = $url; Code = $code }
        }
    } catch {
        $failed += [PSCustomObject]@{ Name = $name; Url = $url; Code = "ERR" }
    }
}

Write-Host "Total: $($matches.Count) | Working: $($working.Count) | Failed: $($failed.Count)" -ForegroundColor Yellow
Write-Host "`nFAILED CHANNELS:" -ForegroundColor Red
$failed | ForEach-Object { Write-Host " - $($_.Name): $($_.Code) -> $($_.Url)" }
