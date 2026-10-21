Add-Type -AssemblyName System.Net.Http
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]'Tls12,Tls13'

$client = [System.Net.Http.HttpClient]::new()
$client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36")
$client.Timeout = [TimeSpan]::FromSeconds(3)

$categories = @("news", "movies", "sports", "kids", "documentary")

foreach ($cat in $categories) {
    try {
        $url = "https://iptv-org.github.io/iptv/categories/$cat.m3u"
        $m3u = $client.GetStringAsync($url).GetAwaiter().GetResult()
        $lines = $m3u -split "`r?`n"
        Write-Host "Category: $cat (Lines: $($lines.Length))" -ForegroundColor Cyan

        $count = 0
        for ($i = 0; $i -lt $lines.Length - 1; $i++) {
            if ($lines[$i].StartsWith("#EXTINF")) {
                $meta = $lines[$i]
                $stream = $lines[$i+1].Trim()
                if ($stream.StartsWith("http") -and -not $stream.Contains("Geo-blocked")) {
                    $name = $meta.Substring($meta.LastIndexOf(",") + 1).Trim()
                    try {
                        $res = $client.GetAsync($stream).GetAwaiter().GetResult()
                        if ([int]$res.StatusCode -eq 200) {
                            Write-Host " [$cat OK] $name -> $stream" -ForegroundColor Green
                            $count++
                            if ($count -ge 10) { break }
                        }
                    } catch {}
                }
            }
        }
    } catch {
        Write-Host "Failed to load category $cat" -ForegroundColor Red
    }
}
