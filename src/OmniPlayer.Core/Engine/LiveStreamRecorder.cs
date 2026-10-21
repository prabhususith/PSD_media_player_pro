using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OmniPlayer.Core.Engine
{
    /// <summary>
    /// Background Live Stream & IPTV Recorder engine.
    /// Captures direct HTTP/HTTPS transport streams and HLS (.m3u8) broadcasts
    /// into native .ts files with real-time statistics and graceful flush.
    /// </summary>
    public class LiveStreamRecorder
    {
        private static readonly Lazy<LiveStreamRecorder> _instance = new(() => new LiveStreamRecorder());
        public static LiveStreamRecorder Instance => _instance.Value;

        private readonly object _syncLock = new();
        private readonly HttpClient _httpClient;
        private CancellationTokenSource? _cts;
        private Task? _recordingWorker;
        private readonly Stopwatch _stopwatch = new();

        public bool IsRecording { get; private set; }
        public string CurrentRecordingFilePath { get; private set; } = string.Empty;
        public TimeSpan RecordingDuration => _stopwatch.Elapsed;
        public long RecordedBytes { get; private set; }

        public event Action<TimeSpan, long>? RecordingProgress;
        public event Action<string>? RecordingFinished;
        public event Action<string>? RecordingError;

        private LiveStreamRecorder()
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.All
            };

            _httpClient = new HttpClient(handler)
            {
                Timeout = Timeout.InfiniteTimeSpan
            };
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        public void StartRecording(string streamUrl, string channelOrMediaTitle)
        {
            if (string.IsNullOrWhiteSpace(streamUrl) ||
                (!streamUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                 !streamUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                 !streamUrl.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase) &&
                 !streamUrl.StartsWith("rtmp://", StringComparison.OrdinalIgnoreCase) &&
                 !streamUrl.StartsWith("mms://", StringComparison.OrdinalIgnoreCase)))
            {
                RecordingError?.Invoke("Invalid or unsupported stream URL protocol.");
                return;
            }

            lock (_syncLock)
            {
                if (IsRecording)
                {
                    StopRecording();
                }

                try
                {
                    string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    string targetDirectory = Path.Combine(userProfile, "Videos", "PSD_Recordings");
                    if (!Directory.Exists(targetDirectory))
                    {
                        Directory.CreateDirectory(targetDirectory);
                    }

                    string sanitizedTitle = SanitizeFileName(channelOrMediaTitle);
                    if (string.IsNullOrWhiteSpace(sanitizedTitle))
                    {
                        sanitizedTitle = "LiveStream";
                    }

                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string fileName = $"PSD_Rec_{sanitizedTitle}_{timestamp}.ts";
                    CurrentRecordingFilePath = Path.Combine(targetDirectory, fileName);

                    RecordedBytes = 0;
                    _stopwatch.Restart();
                    _cts = new CancellationTokenSource();
                    IsRecording = true;

                    var token = _cts.Token;
                    _recordingWorker = Task.Run(() => CaptureStreamLoopAsync(streamUrl, CurrentRecordingFilePath, token), token);
                }
                catch (Exception ex)
                {
                    IsRecording = false;
                    _stopwatch.Stop();
                    RecordingError?.Invoke($"Failed to initiate recording: {ex.Message}");
                }
            }
        }

        public void StopRecording()
        {
            lock (_syncLock)
            {
                if (!IsRecording) return;
                try
                {
                    _cts?.Cancel();
                }
                catch
                {
                    // Ignore cancellation request issues
                }
            }
        }

        private async Task CaptureStreamLoopAsync(string streamUrl, string outputFilePath, CancellationToken token)
        {
            FileStream? fileStream = null;
            try
            {
                fileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 65536, useAsync: true);

                if (streamUrl.Contains(".m3u8", StringComparison.OrdinalIgnoreCase))
                {
                    await CaptureHlsAsync(streamUrl, fileStream, token).ConfigureAwait(false);
                }
                else
                {
                    await CaptureDirectStreamAsync(streamUrl, fileStream, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal user-initiated stop
            }
            catch (Exception ex)
            {
                RecordingError?.Invoke($"Recording error: {ex.Message}");
            }
            finally
            {
                if (fileStream != null)
                {
                    try
                    {
                        await fileStream.FlushAsync(CancellationToken.None).ConfigureAwait(false);
                        fileStream.Dispose();
                    }
                    catch
                    {
                        // Best effort stream flush
                    }
                }

                lock (_syncLock)
                {
                    IsRecording = false;
                    _stopwatch.Stop();
                    _cts?.Dispose();
                    _cts = null;
                }

                RecordingProgress?.Invoke(_stopwatch.Elapsed, RecordedBytes);
                RecordingFinished?.Invoke(outputFilePath);
            }
        }

        private async Task CaptureDirectStreamAsync(string streamUrl, FileStream fileStream, CancellationToken token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, streamUrl);
            request.Headers.Add("Referer", "https://www.google.com/");

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var contentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            if (contentType.Contains("mpegurl", StringComparison.OrdinalIgnoreCase) ||
                contentType.Contains("m3u8", StringComparison.OrdinalIgnoreCase))
            {
                // Server returned HLS manifest despite URL not explicitly ending in .m3u8
                await CaptureHlsAsync(streamUrl, fileStream, token).ConfigureAwait(false);
                return;
            }

            using var stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
            byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(65536);
            try
            {
                int bytesRead;
                var lastUpdate = DateTime.UtcNow;

                while (!token.IsCancellationRequested && (bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), token).ConfigureAwait(false)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), token).ConfigureAwait(false);
                    RecordedBytes += bytesRead;

                    if ((DateTime.UtcNow - lastUpdate).TotalMilliseconds >= 500)
                    {
                        lastUpdate = DateTime.UtcNow;
                        RecordingProgress?.Invoke(_stopwatch.Elapsed, RecordedBytes);
                    }
                }
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private async Task CaptureHlsAsync(string playlistUrl, FileStream fileStream, CancellationToken token)
        {
            var downloadedSegments = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<string>();
            var lastUpdate = DateTime.UtcNow;
            string currentPlaylistUrl = playlistUrl;

            while (!token.IsCancellationRequested)
            {
                string manifest;
                try
                {
                    using var req = new HttpRequestMessage(HttpMethod.Get, currentPlaylistUrl);
                    req.Headers.Add("Referer", "https://www.google.com/");
                    using var res = await _httpClient.SendAsync(req, token).ConfigureAwait(false);
                    res.EnsureSuccessStatusCode();
                    manifest = await res.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                }
                catch (Exception) when (!token.IsCancellationRequested)
                {
                    await Task.Delay(2000, token).ConfigureAwait(false);
                    continue;
                }

                // If master playlist with multiple qualities, pick the first or best stream
                if (manifest.Contains("#EXT-X-STREAM-INF", StringComparison.OrdinalIgnoreCase))
                {
                    string? mediaVariantUrl = ExtractVariantPlaylistUrl(manifest, currentPlaylistUrl);
                    if (!string.IsNullOrEmpty(mediaVariantUrl))
                    {
                        currentPlaylistUrl = mediaVariantUrl;
                        continue;
                    }
                }

                var baseUri = new Uri(currentPlaylistUrl);
                var lines = manifest.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                bool hasEndList = false;

                foreach (var rawLine in lines)
                {
                    var line = rawLine.Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    if (line.StartsWith("#EXT-X-ENDLIST", StringComparison.OrdinalIgnoreCase))
                    {
                        hasEndList = true;
                        continue;
                    }

                    if (line.StartsWith("#")) continue;

                    // Resolve absolute URL
                    string segmentUrl = Uri.TryCreate(baseUri, line, out var resolvedUri) ? resolvedUri.ToString() : line;

                    if (!downloadedSegments.Contains(segmentUrl))
                    {
                        downloadedSegments.Add(segmentUrl);
                        queue.Enqueue(segmentUrl);
                        if (queue.Count > 500)
                        {
                            var old = queue.Dequeue();
                            downloadedSegments.Remove(old);
                        }

                        // Download segment and append with pooled streaming buffer
                        try
                        {
                            using var segReq = new HttpRequestMessage(HttpMethod.Get, segmentUrl);
                            segReq.Headers.Add("Referer", "https://www.google.com/");
                            using var segRes = await _httpClient.SendAsync(segReq, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
                            if (segRes.IsSuccessStatusCode)
                            {
                                using var segStream = await segRes.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
                                byte[] segBuf = System.Buffers.ArrayPool<byte>.Shared.Rent(32768);
                                try
                                {
                                    int segRead;
                                    while ((segRead = await segStream.ReadAsync(segBuf.AsMemory(0, segBuf.Length), token).ConfigureAwait(false)) > 0)
                                    {
                                        await fileStream.WriteAsync(segBuf.AsMemory(0, segRead), token).ConfigureAwait(false);
                                        RecordedBytes += segRead;
                                    }
                                }
                                finally
                                {
                                    System.Buffers.ArrayPool<byte>.Shared.Return(segBuf);
                                }

                                if ((DateTime.UtcNow - lastUpdate).TotalMilliseconds >= 500)
                                {
                                    lastUpdate = DateTime.UtcNow;
                                    RecordingProgress?.Invoke(_stopwatch.Elapsed, RecordedBytes);
                                }
                            }
                        }
                        catch (Exception) when (!token.IsCancellationRequested)
                        {
                            // Skip broken segment gracefully
                        }
                    }
                }

                if (hasEndList)
                {
                    break;
                }

                await Task.Delay(2000, token).ConfigureAwait(false);
            }
        }

        private static string? ExtractVariantPlaylistUrl(string masterManifest, string baseUrl)
        {
            var lines = masterManifest.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var baseUri = new Uri(baseUrl);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.StartsWith("#EXT-X-STREAM-INF", StringComparison.OrdinalIgnoreCase) && i + 1 < lines.Length)
                {
                    var nextLine = lines[i + 1].Trim();
                    if (!nextLine.StartsWith("#") && !string.IsNullOrEmpty(nextLine))
                    {
                        return Uri.TryCreate(baseUri, nextLine, out var resolved) ? resolved.ToString() : nextLine;
                    }
                }
            }
            return null;
        }

        private static string SanitizeFileName(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            var invalidChars = Path.GetInvalidFileNameChars();
            var result = new System.Text.StringBuilder(input.Length);

            foreach (char c in input)
            {
                if (Array.IndexOf(invalidChars, c) >= 0 || c == ':' || c == '/' || c == '\\' || c == '?' || c == '*' || c == '"' || c == '<' || c == '>' || c == '|')
                {
                    result.Append('_');
                }
                else if (c == ' ')
                {
                    result.Append('_');
                }
                else
                {
                    result.Append(c);
                }
            }

            return result.ToString().Trim('_');
        }
    }
}
