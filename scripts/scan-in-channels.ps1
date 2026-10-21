Add-Type -AssemblyName System.Net.Http
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]'Tls12,Tls13'

$client = [System.Net.Http.HttpClient]::new()
$client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36")
$client.Timeout = [TimeSpan]::FromSeconds(5)

$m3u = $client.GetStringAsync("https://iptv-org.github.io/iptv/countries/in.m3u").GetAwaiter().GetResult()
$lines = $m3u -split "`r?`n"
Write-Host "Total lines in in.m3u: $($lines.Length)"

$tested = 0
$working = @()

for ($i = 0; $i -lt $lines.Length - 1; $i++) {
    if ($lines[$i].StartsWith("#EXTINF")) {
        $meta = $lines[$i]
        $url = $lines[$i+1].Trim()
        if ($url.StartsWith("http")) {
            $name = $meta.Substring($meta.LastIndexOf(",") + 1).Trim()
            $tested++
            if ($tested -gt 60) { break }

            try {
                $res = $client.GetAsync($url).GetAwaiter().GetResult()
                $code = [int]$res.StatusCode
                if ($code -ge 200 -and $code -lt 400) {
                    Write-Host " [OK $code] $name -> $url" -ForegroundColor Green
                    $working += [PSCustomObject]@{ Name = $name; Url = $url }
                }
            } catch {}
        }
    }
}

Write-Host "Tested: $tested | Working: $($working.Count)" -ForegroundColor Cyan
