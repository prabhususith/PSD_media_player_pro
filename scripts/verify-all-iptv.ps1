Add-Type -AssemblyName System.Net.Http
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]'Tls12,Tls13'

$csFile = "P:\prabhu\projects\git\omni-player\src\OmniPlayer.Core\LiveTv\LiveTvManager.cs"
$content = Get-Content $csFile -Raw

$matches = [regex]::Matches($content, '\("([^"]+)",\s*"(http[^"]+)",')

Write-Host "Found $($matches.Count) curated channel URLs to verify..." -ForegroundColor Cyan

$handler = [System.Net.Http.HttpClientHandler]::new()
$handler.ServerCertificateCustomValidationCallback = { $true }
$client = [System.Net.Http.HttpClient]::new($handler)
$client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36")
$client.Timeout = [TimeSpan]::FromSeconds(5)

$working = 0
$failed = 0
$results = @()

foreach ($m in $matches) {
    $name = $m.Groups[1].Value
    $url = $m.Groups[2].Value

    try {
        $res = $client.GetAsync($url).GetAwaiter().GetResult()
        $status = [int]$res.StatusCode
        if ($status -ge 200 -and $status -lt 400) {
            $working++
            $results += [PSCustomObject]@{ Status = "OK ($status)"; Name = $name; Url = $url }
            Write-Host " [OK $status] $name" -ForegroundColor Green
        } else {
            $failed++
            $results += [PSCustomObject]@{ Status = "FAIL ($status)"; Name = $name; Url = $url }
            Write-Host " [FAIL $status] $name -> $url" -ForegroundColor Red
        }
    } catch {
        $failed++
        $results += [PSCustomObject]@{ Status = "ERR ($($_.Exception.Message))"; Name = $name; Url = $url }
        Write-Host " [ERR] $name: $($_.Exception.Message)" -ForegroundColor DarkRed
    }
}

Write-Host "`nSummary: $working Working, $failed Failed out of $($matches.Count)" -ForegroundColor Cyan
