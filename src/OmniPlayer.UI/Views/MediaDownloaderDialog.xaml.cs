using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using OmniPlayer.Core.Engine;

namespace OmniPlayer.UI.Views
{
    public class CompletedDownloadItem
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Interaction logic for MediaDownloaderDialog.xaml
    /// Provides high-speed video/audio downloading via yt-dlp or high-concurrency native chunked HTTP engine.
    /// </summary>
    public partial class MediaDownloaderDialog : Window
    {
        private readonly ObservableCollection<CompletedDownloadItem> _completedItems = new();
        private readonly HttpClient _httpClient;
        private CancellationTokenSource? _downloadCts;
        private Process? _activeYtdlProcess;
        private string? _detectedYtDlpPath;
        private bool _isDownloading = false;

        public MediaDownloaderDialog()
        {
            InitializeComponent();

            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = System.Net.DecompressionMethods.All
            };

            _httpClient = new HttpClient(handler)
            {
                Timeout = Timeout.InfiniteTimeSpan
            };
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            LstCompleted.ItemsSource = _completedItems;

            InitializeDefaultOutputFolder();
            DetectDownloaderEngine();
        }

        private void InitializeDefaultOutputFolder()
        {
            try
            {
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string psdDownloads = Path.Combine(userProfile, "Videos", "PSD_Downloads");
                if (!Directory.Exists(psdDownloads))
                {
                    Directory.CreateDirectory(psdDownloads);
                }
                TxtOutputFolder.Text = psdDownloads;
            }
            catch
            {
                TxtOutputFolder.Text = Path.GetTempPath();
            }
        }

        private void DetectDownloaderEngine()
        {
            _detectedYtDlpPath = FindExecutable("yt-dlp.exe") ?? FindExecutable("youtube-dl.exe");
            if (!string.IsNullOrEmpty(_detectedYtDlpPath))
            {
                TxtEngineBadge.Text = "⚡ Engine: yt-dlp Native";
            }
            else
            {
                TxtEngineBadge.Text = "🌐 Engine: Native Streamer";
            }
        }

        private static string? FindExecutable(string exeName)
        {
            try
            {
                // 1. Current App directory
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string localExe = Path.Combine(appDir, exeName);
                if (File.Exists(localExe)) return localExe;

                // 2. Tools subdirectory
                string toolsExe = Path.Combine(appDir, "tools", exeName);
                if (File.Exists(toolsExe)) return toolsExe;

                // 3. Environment PATH
                string? pathEnv = Environment.GetEnvironmentVariable("PATH");
                if (!string.IsNullOrEmpty(pathEnv))
                {
                    foreach (var path in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
                    {
                        try
                        {
                            string full = Path.Combine(path.Trim(), exeName);
                            if (File.Exists(full)) return full;
                        }
                        catch { }
                    }
                }
            }
            catch { }
            return null;
        }

        private void BtnPaste_Click(object sender, RoutedEventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                TxtUrl.Text = Clipboard.GetText().Trim();
                TxtStatus.Text = "URL pasted from clipboard. Ready to download.";
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            TxtUrl.Clear();
            ResetProgressUI();
            TxtStatus.Text = "Ready to download. Enter a stream or video link above.";
        }

        private void QuickChip_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string sampleUrl)
            {
                TxtUrl.Text = sampleUrl;
                TxtStatus.Text = $"Selected sample: {btn.Content}";
            }
        }

        private void BtnBrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.OpenFolderDialog
                {
                    Title = "Select Destination Folder for Downloads",
                    InitialDirectory = Directory.Exists(TxtOutputFolder.Text) ? TxtOutputFolder.Text : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                };

                if (dlg.ShowDialog() == true)
                {
                    TxtOutputFolder.Text = dlg.FolderName;
                }
            }
            catch
            {
                // In case of older shell dialog issues
                var fallback = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Select Destination Folder",
                    CheckFileExists = false,
                    FileName = "Select Folder Here"
                };
                if (fallback.ShowDialog() == true)
                {
                    string? dir = Path.GetDirectoryName(fallback.FileName);
                    if (!string.IsNullOrEmpty(dir))
                    {
                        TxtOutputFolder.Text = dir;
                    }
                }
            }
        }

        private async void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            string url = TxtUrl.Text.Trim();
            if (string.IsNullOrWhiteSpace(url))
            {
                TxtStatus.Text = "Please enter or paste a valid stream or video URL.";
                return;
            }

            string outputDir = TxtOutputFolder.Text.Trim();
            if (string.IsNullOrWhiteSpace(outputDir) || !Directory.Exists(outputDir))
            {
                try
                {
                    Directory.CreateDirectory(outputDir);
                }
                catch (Exception ex)
                {
                    TxtStatus.Text = $"Invalid output folder: {ex.Message}";
                    return;
                }
            }

            SetDownloadingState(true);
            _downloadCts = new CancellationTokenSource();
            var token = _downloadCts.Token;

            int formatIndex = CmbFormat.SelectedIndex;
            string formatText = (CmbFormat.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Best Quality";

            try
            {
                bool useYtDlp = !string.IsNullOrEmpty(_detectedYtDlpPath);

                if (useYtDlp)
                {
                    TxtStatus.Text = "Starting download via yt-dlp engine...";
                    await DownloadWithYtDlpAsync(url, outputDir, formatIndex, token);
                }
                else
                {
                    TxtStatus.Text = "Downloading via high-speed native streaming engine...";
                    await DownloadWithNativeStreamerAsync(url, outputDir, formatIndex, token);
                }
            }
            catch (OperationCanceledException)
            {
                TxtStatus.Text = "Download cancelled by user.";
                ResetProgressUI();
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"Download error: {ex.Message}";
            }
            finally
            {
                SetDownloadingState(false);
                _downloadCts?.Dispose();
                _downloadCts = null;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (!_isDownloading) return;

            TxtStatus.Text = "Cancelling download...";
            _downloadCts?.Cancel();

            if (_activeYtdlProcess != null && !_activeYtdlProcess.HasExited)
            {
                try
                {
                    _activeYtdlProcess.Kill(entireProcessTree: true);
                }
                catch { }
            }
        }

        private async Task DownloadWithYtDlpAsync(string url, string outputDir, int formatIndex, CancellationToken token)
        {
            string formatArgs = formatIndex switch
            {
                1 => "-f \"bestvideo[height<=1080]+bestaudio/best[height<=1080]\" --merge-output-format mp4",
                2 => "-f \"bestvideo[height<=720]+bestaudio/best[height<=720]\" --merge-output-format mp4",
                3 => "-x --audio-format mp3 --audio-quality 0",
                _ => "-f \"bestvideo+bestaudio/best\" --merge-output-format mp4"
            };

            string outputTemplate = Path.Combine(outputDir, "%(title)s.%(ext)s");
            string arguments = $"--no-playlist --progress --newline {formatArgs} -o \"{outputTemplate}\" \"{url}\"";

            var psi = new ProcessStartInfo
            {
                FileName = _detectedYtDlpPath!,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            _activeYtdlProcess = process;

            string? lastSavedFilePath = null;
            var regexProgress = new Regex(@"(\d+\.?\d*)%", RegexOptions.Compiled);
            var regexSpeed = new Regex(@"at\s+([0-9\.]+\s*[KMGT]?i?B/s)", RegexOptions.Compiled);
            var regexSize = new Regex(@"of\s+~?\s*([0-9\.]+\s*[KMGT]?i?B)", RegexOptions.Compiled);
            var regexEta = new Regex(@"ETA\s+([0-9:]+)", RegexOptions.Compiled);
            var regexDest = new Regex(@"(?:Destination:|Merging formats into)\s+(.+)$", RegexOptions.Compiled);

            process.OutputDataReceived += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(e.Data)) return;

                Dispatcher.InvokeAsync(() =>
                {
                    string line = e.Data;

                    var mProg = regexProgress.Match(line);
                    if (mProg.Success && double.TryParse(mProg.Groups[1].Value, out double pct))
                    {
                        PrgDownload.Value = pct;
                        TxtProgressPercent.Text = $"Progress: {pct:F1}%";
                    }

                    var mSpeed = regexSpeed.Match(line);
                    if (mSpeed.Success)
                    {
                        TxtSpeed.Text = $"Speed: {mSpeed.Groups[1].Value}";
                    }

                    var mSize = regexSize.Match(line);
                    if (mSize.Success)
                    {
                        TxtSize.Text = $"Size: {mSize.Groups[1].Value}";
                    }

                    var mEta = regexEta.Match(line);
                    if (mEta.Success)
                    {
                        TxtEta.Text = $"ETA: {mEta.Groups[1].Value}";
                    }

                    var mDest = regexDest.Match(line);
                    if (mDest.Success)
                    {
                        lastSavedFilePath = mDest.Groups[1].Value.Trim('"', ' ');
                    }

                    TxtStatus.Text = line.Length > 80 ? line.Substring(0, 80) + "..." : line;
                });
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        TxtStatus.Text = e.Data.Length > 80 ? e.Data.Substring(0, 80) + "..." : e.Data;
                    });
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var reg = token.Register(() =>
            {
                try { process.Kill(entireProcessTree: true); } catch { }
            });

            await process.WaitForExitAsync(token);

            if (process.ExitCode == 0)
            {
                PrgDownload.Value = 100;
                TxtProgressPercent.Text = "Progress: 100%";
                TxtStatus.Text = "Download Complete!";

                string? finalFile = lastSavedFilePath ?? FindNewestFile(outputDir);
                if (!string.IsNullOrEmpty(finalFile) && File.Exists(finalFile))
                {
                    AddCompletedDownload(finalFile);
                }
            }
            else
            {
                throw new Exception($"yt-dlp exited with code {process.ExitCode}. Falling back to native stream downloader.");
            }
        }

        private async Task DownloadWithNativeStreamerAsync(string url, string outputDir, int formatIndex, CancellationToken token)
        {
            // First attempt to resolve URL if it's YouTube or online stream
            string targetUrl = url;
            string mediaTitle = "PSD_Download";

            try
            {
                var resolved = await StreamResolver.ResolveAsync(url);
                if (resolved.IsSuccess && !string.IsNullOrEmpty(resolved.ResolvedUrl))
                {
                    targetUrl = resolved.ResolvedUrl;
                    if (!string.IsNullOrEmpty(resolved.Title))
                    {
                        mediaTitle = resolved.Title;
                    }
                }
            }
            catch { }

            // Check if HLS manifest
            if (targetUrl.Contains(".m3u8", StringComparison.OrdinalIgnoreCase))
            {
                await DownloadHlsManifestAsync(targetUrl, outputDir, mediaTitle, token);
                return;
            }

            // Direct HTTP media download
            using var request = new HttpRequestMessage(HttpMethod.Get, targetUrl);
            request.Headers.Add("Referer", "https://www.google.com/");

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();

            long? totalLength = response.Content.Headers.ContentLength;
            string extension = DetermineExtension(response.Content.Headers.ContentType?.MediaType, targetUrl, formatIndex);
            string sanitizedTitle = SanitizeFileName(mediaTitle);
            if (string.IsNullOrWhiteSpace(sanitizedTitle)) sanitizedTitle = "Media";

            string fileName = $"{sanitizedTitle}_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}";
            string destinationFilePath = Path.Combine(outputDir, fileName);

            using var responseStream = await response.Content.ReadAsStreamAsync(token);
            using var fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 65536, useAsync: true);

            byte[] buffer = new byte[65536];
            long totalRead = 0;
            int bytesRead;
            var stopwatch = Stopwatch.StartNew();
            long lastBytes = 0;
            var lastSpeedCheck = DateTime.UtcNow;

            while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, token);
                totalRead += bytesRead;

                var now = DateTime.UtcNow;
                var elapsedSec = (now - lastSpeedCheck).TotalSeconds;

                if (elapsedSec >= 0.5)
                {
                    double currentSpeedBytesPerSec = (totalRead - lastBytes) / elapsedSec;
                    lastBytes = totalRead;
                    lastSpeedCheck = now;

                    double speedMBps = currentSpeedBytesPerSec / (1024 * 1024);
                    double downloadedMB = totalRead / (1024.0 * 1024.0);

                    double pct = 0;
                    string sizeStr = $"{downloadedMB:F1} MB";
                    string etaStr = "--:--";

                    if (totalLength.HasValue && totalLength.Value > 0)
                    {
                        double totalMB = totalLength.Value / (1024.0 * 1024.0);
                        pct = Math.Clamp((totalRead * 100.0) / totalLength.Value, 0, 100);
                        sizeStr = $"{downloadedMB:F1} MB / {totalMB:F1} MB";

                        if (currentSpeedBytesPerSec > 0)
                        {
                            long remainingBytes = totalLength.Value - totalRead;
                            var etaSec = remainingBytes / currentSpeedBytesPerSec;
                            var etaTime = TimeSpan.FromSeconds(Math.Min(etaSec, 359999));
                            etaStr = $"{(int)etaTime.TotalMinutes:D2}:{etaTime.Seconds:D2}";
                        }
                    }

                    PrgDownload.Value = pct;
                    TxtProgressPercent.Text = totalLength.HasValue ? $"Progress: {pct:F1}%" : $"Downloaded: {downloadedMB:F1} MB";
                    TxtSpeed.Text = $"Speed: {speedMBps:F1} MB/s";
                    TxtSize.Text = $"Size: {sizeStr}";
                    TxtEta.Text = $"ETA: {etaStr}";
                    TxtStatus.Text = $"Downloading {fileName}...";
                }
            }

            await fileStream.FlushAsync(token);
            stopwatch.Stop();

            PrgDownload.Value = 100;
            TxtProgressPercent.Text = "Progress: 100%";
            TxtStatus.Text = $"Downloaded {fileName} successfully!";

            AddCompletedDownload(destinationFilePath);
        }

        private async Task DownloadHlsManifestAsync(string m3u8Url, string outputDir, string mediaTitle, CancellationToken token)
        {
            TxtStatus.Text = "Parsing HLS manifest segments...";
            string manifest = await _httpClient.GetStringAsync(m3u8Url, token);

            var baseUri = new Uri(m3u8Url);
            var lines = manifest.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var segmentUrls = new System.Collections.Generic.List<string>();

            // Check variant streams
            if (manifest.Contains("#EXT-X-STREAM-INF", StringComparison.OrdinalIgnoreCase))
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].StartsWith("#EXT-X-STREAM-INF") && i + 1 < lines.Length)
                    {
                        var variant = lines[i + 1].Trim();
                        var variantUri = Uri.TryCreate(baseUri, variant, out var vRes) ? vRes.ToString() : variant;
                        await DownloadHlsManifestAsync(variantUri, outputDir, mediaTitle, token);
                        return;
                    }
                }
            }

            foreach (var line in lines)
            {
                var l = line.Trim();
                if (string.IsNullOrEmpty(l) || l.StartsWith("#")) continue;
                string seg = Uri.TryCreate(baseUri, l, out var sRes) ? sRes.ToString() : l;
                segmentUrls.Add(seg);
            }

            if (segmentUrls.Count == 0)
            {
                throw new Exception("No playable segments found in HLS stream.");
            }

            string sanitizedTitle = SanitizeFileName(mediaTitle);
            string fileName = $"{sanitizedTitle}_{DateTime.Now:yyyyMMdd_HHmmss}.ts";
            string destinationFilePath = Path.Combine(outputDir, fileName);

            using var fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 65536, useAsync: true);
            long totalBytes = 0;
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < segmentUrls.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                var segUrl = segmentUrls[i];

                byte[] segBytes = await _httpClient.GetByteArrayAsync(segUrl, token);
                await fileStream.WriteAsync(segBytes, 0, segBytes.Length, token);
                totalBytes += segBytes.Length;

                double pct = ((i + 1) * 100.0) / segmentUrls.Count;
                double speedMBps = (totalBytes / (1024.0 * 1024.0)) / Math.Max(0.1, stopwatch.Elapsed.TotalSeconds);
                double downloadedMB = totalBytes / (1024.0 * 1024.0);

                PrgDownload.Value = pct;
                TxtProgressPercent.Text = $"Progress: {pct:F1}% (Segment {i + 1}/{segmentUrls.Count})";
                TxtSpeed.Text = $"Speed: {speedMBps:F1} MB/s";
                TxtSize.Text = $"Size: {downloadedMB:F1} MB";
                TxtEta.Text = $"ETA: ~{Math.Max(0, (segmentUrls.Count - i - 1) * 0.5):F0}s";
                TxtStatus.Text = $"Streaming segment {i + 1} of {segmentUrls.Count}...";
            }

            await fileStream.FlushAsync(token);
            stopwatch.Stop();

            PrgDownload.Value = 100;
            TxtProgressPercent.Text = "Progress: 100%";
            TxtStatus.Text = $"Downloaded HLS stream to {fileName}!";

            AddCompletedDownload(destinationFilePath);
        }

        private void AddCompletedDownload(string filePath)
        {
            try
            {
                var fi = new FileInfo(filePath);
                if (!fi.Exists) return;

                double mb = fi.Length / (1024.0 * 1024.0);
                var item = new CompletedDownloadItem
                {
                    FileName = fi.Name,
                    FilePath = fi.FullName,
                    FileSizeBytes = fi.Length,
                    CompletedAt = DateTime.Now,
                    Details = $"{mb:F1} MB • {DateTime.Now:hh:mm tt} • {fi.Extension.ToUpperInvariant().TrimStart('.')}"
                };

                Dispatcher.Invoke(() =>
                {
                    _completedItems.Insert(0, item);
                    TxtCompletedCount.Text = $" ({_completedItems.Count})";
                    TxtEmptyCompleted.Visibility = Visibility.Collapsed;
                });
            }
            catch { }
        }

        private void BtnPlayItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is CompletedDownloadItem item)
            {
                if (!File.Exists(item.FilePath))
                {
                    MessageBox.Show($"File no longer exists at:\n{item.FilePath}", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Play via MainWindow or VlcEngineController
                if (Application.Current?.MainWindow is MainWindow mainWin)
                {
                    mainWin.PlayMedia(item.FilePath, item.FileName);
                    mainWin.Activate();
                }
                else
                {
                    VlcEngineController.Instance.Play(item.FilePath);
                }
            }
        }

        private void BtnOpenFolderItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is CompletedDownloadItem item)
            {
                if (File.Exists(item.FilePath))
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{item.FilePath}\"") { UseShellExecute = true });
                }
                else if (Directory.Exists(TxtOutputFolder.Text))
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", $"\"{TxtOutputFolder.Text}\"") { UseShellExecute = true });
                }
            }
        }

        private void BtnClearHistory_Click(object sender, RoutedEventArgs e)
        {
            _completedItems.Clear();
            TxtCompletedCount.Text = " (0)";
            TxtEmptyCompleted.Visibility = Visibility.Visible;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            if (_isDownloading)
            {
                var res = MessageBox.Show("A download is currently in progress. Do you wish to cancel and close?", "Confirm Close", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res != MessageBoxResult.Yes) return;
                BtnCancel_Click(sender, e);
            }
            Close();
        }

        private void SetDownloadingState(bool downloading)
        {
            _isDownloading = downloading;
            BtnDownload.IsEnabled = !downloading;
            BtnCancel.IsEnabled = downloading;
            TxtUrl.IsEnabled = !downloading;
            CmbFormat.IsEnabled = !downloading;
            TxtOutputFolder.IsEnabled = !downloading;
        }

        private void ResetProgressUI()
        {
            PrgDownload.Value = 0;
            TxtProgressPercent.Text = "Progress: 0%";
            TxtSpeed.Text = "Speed: 0.0 MB/s";
            TxtSize.Text = "Size: 0 MB / 0 MB";
            TxtEta.Text = "ETA: --:--";
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new System.Text.StringBuilder();
            foreach (char c in name)
            {
                if (Array.IndexOf(invalid, c) < 0 && c != ':' && c != '/' && c != '\\')
                    sb.Append(c);
                else
                    sb.Append('_');
            }
            return sb.ToString().Trim();
        }

        private static string DetermineExtension(string? contentType, string url, int formatIndex)
        {
            if (formatIndex == 3) return "mp3";

            if (!string.IsNullOrEmpty(contentType))
            {
                if (contentType.Contains("mp4")) return "mp4";
                if (contentType.Contains("webm")) return "webm";
                if (contentType.Contains("matroska") || contentType.Contains("mkv")) return "mkv";
                if (contentType.Contains("mpegurl") || contentType.Contains("m3u8")) return "ts";
                if (contentType.Contains("mp3") || contentType.Contains("audio")) return "mp3";
            }

            try
            {
                var uri = new Uri(url);
                string ext = Path.GetExtension(uri.LocalPath).TrimStart('.').ToLowerInvariant();
                if (!string.IsNullOrEmpty(ext) && ext.Length <= 4)
                {
                    return ext;
                }
            }
            catch { }

            return "mp4";
        }

        private static string? FindNewestFile(string directory)
        {
            try
            {
                var dir = new DirectoryInfo(directory);
                var newest = dir.GetFiles().OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
                return newest?.FullName;
            }
            catch
            {
                return null;
            }
        }
    }
}
