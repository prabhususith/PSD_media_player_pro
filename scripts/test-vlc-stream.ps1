$dllPath = "P:\prabhu\projects\git\omni-player\src\OmniPlayer.UI\bin\Release\net8.0-windows\LibVLCSharp.dll"
$libvlcDir = "P:\prabhu\projects\git\omni-player\src\OmniPlayer.UI\bin\Release\net8.0-windows\libvlc\win-x64"

Add-Type -Path $dllPath
[LibVLCSharp.Shared.Core]::Initialize($libvlcDir)

$options = @(
    "--http-user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
    "--http-referrer=https://www.google.com/"
)
$vlc = [LibVLCSharp.Shared.LibVLC]::new($options)
$vlc.add_Log({
    param($s, $e)
    if ($e.Level -eq [LibVLCSharp.Shared.LogLevel]::Error -or $e.Level -eq [LibVLCSharp.Shared.LogLevel]::Warning) {
        Write-Host "[$($e.Level)] $($e.Module): $($e.Message)" -ForegroundColor Yellow
    }
})

$mp = [LibVLCSharp.Shared.MediaPlayer]::new($vlc)
$url = "https://feeds.intoday.in/aajtak/api/aajtakhd/master.m3u8"
Write-Host "Testing Play on: $url"
$media = [LibVLCSharp.Shared.Media]::new($vlc, [Uri]::new($url))
$media.AddOption(":network-caching=3000")
$media.AddOption(":http-reconnect=true")

$ok = $mp.Play($media)
Write-Host "Play called result: $ok"

for ($i = 0; $i -lt 8; $i++) {
    Start-Sleep -Seconds 1
    Write-Host "Second $($i + 1) -> MediaPlayer State: $($mp.State), Time: $($mp.Time)ms"
}
$mp.Stop()
Write-Host "Test finished."
