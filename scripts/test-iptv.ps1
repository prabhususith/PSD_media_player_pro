Add-Type -AssemblyName System.Net.Http

try {
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]'Tls12,Tls13'
} catch {}

$client = [System.Net.Http.HttpClient]::new()
$client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36")

try {
    Write-Host "Connecting to google.com..."
    $t = $client.GetStringAsync("https://www.google.com")
    $res = $t.GetAwaiter().GetResult()
    Write-Host "Google success! Length: $($res.Length)" -ForegroundColor Green
} catch {
    Write-Host "Google failed: $($_.Exception.ToString())" -ForegroundColor Red
}

try {
    Write-Host "Connecting to Aaj Tak stream..."
    $t2 = $client.GetStringAsync("https://feeds.intoday.in/aajtak/api/aajtakhd/master.m3u8")
    $res2 = $t2.GetAwaiter().GetResult()
    Write-Host "Aaj Tak success! Length: $($res2.Length)" -ForegroundColor Green
} catch {
    Write-Host "Aaj Tak failed: $($_.Exception.ToString())" -ForegroundColor Red
}
