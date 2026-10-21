using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OmniPlayer.Core.Engine
{
    public enum StreamType
    {
        DirectMedia,
        HlsLiveStream,
        DashStream,
        YouTubeStream,
        LiveTvStream,
        AudioStream,
        Unknown
    }

    public class StreamResolutionResult
    {
        public bool IsSuccess { get; set; } = true;
        public string Title { get; set; } = string.Empty;
        public string ResolvedUrl { get; set; } = string.Empty;
        public string OriginalUrl { get; set; } = string.Empty;
        public StreamType Type { get; set; } = StreamType.DirectMedia;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public static class StreamResolver
    {
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        private static readonly Regex _ytRegex = new(
            @"(?:youtube\.com\/(?:[^\/]+\/.+\/|(?:v|e(?:mbed)?)\/|.*[?&]v=)|youtu\.be\/|youtube\.com\/shorts\/)([a-zA-Z0-9_-]{11})",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static async Task<StreamResolutionResult> ResolveAsync(string inputUrl)
        {
            var result = new StreamResolutionResult
            {
                OriginalUrl = inputUrl.Trim(),
                ResolvedUrl = inputUrl.Trim(),
                Title = inputUrl.Trim()
            };

            if (string.IsNullOrWhiteSpace(inputUrl))
            {
                result.IsSuccess = false;
                result.ErrorMessage = "URL cannot be empty.";
                return result;
            }

            string cleanUrl = inputUrl.Trim();

            // Check if YouTube
            var ytMatch = _ytRegex.Match(cleanUrl);
            if (ytMatch.Success)
            {
                result.Type = StreamType.YouTubeStream;
                string videoId = ytMatch.Groups[1].Value;
                result.Title = $"YouTube Video [{videoId}]";

                // 1. Try local yt-dlp if installed in PATH or app directory
                string? ytdlpPath = FindExecutableOnPath("yt-dlp.exe") ?? FindExecutableOnPath("youtube-dl.exe");
                if (!string.IsNullOrEmpty(ytdlpPath))
                {
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = ytdlpPath,
                            Arguments = $"-g -f \"best[ext=mp4]/best\" \"{cleanUrl}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        using var proc = Process.Start(psi);
                        if (proc != null)
                        {
                            string output = await proc.StandardOutput.ReadToEndAsync();
                            await proc.WaitForExitAsync();
                            string directUrl = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                            if (!string.IsNullOrEmpty(directUrl) && directUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                            {
                                result.ResolvedUrl = directUrl;
                                result.IsSuccess = true;
                                return result;
                            }
                        }
                    }
                    catch
                    {
                        // Fall back to public resolver
                    }
                }

                // 2. Try Invidious / Piped public API
                string[] publicApis = new[]
                {
                    $"https://vid.puffyan.us/api/v1/videos/{videoId}",
                    $"https://inv.tux.pizza/api/v1/videos/{videoId}",
                    $"https://invidious.nerdvpn.de/api/v1/videos/{videoId}"
                };

                foreach (var apiUrl in publicApis)
                {
                    try
                    {
                        var resp = await _httpClient.GetAsync(apiUrl);
                        if (resp.IsSuccessStatusCode)
                        {
                            string json = await resp.Content.ReadAsStringAsync();
                            using var doc = JsonDocument.Parse(json);
                            var root = doc.RootElement;
                            if (root.TryGetProperty("title", out var titleProp))
                            {
                                result.Title = titleProp.GetString() ?? result.Title;
                            }

                            if (root.TryGetProperty("formatStreams", out var formatStreams) && formatStreams.GetArrayLength() > 0)
                            {
                                // Pick the highest resolution stream
                                string bestUrl = string.Empty;
                                int maxRes = 0;
                                foreach (var item in formatStreams.EnumerateArray())
                                {
                                    if (item.TryGetProperty("url", out var u) && item.TryGetProperty("resolution", out var resStr))
                                    {
                                        string r = resStr.GetString() ?? "";
                                        int.TryParse(Regex.Match(r, @"\d+").Value, out int resNum);
                                        if (resNum >= maxRes)
                                        {
                                            maxRes = resNum;
                                            bestUrl = u.GetString() ?? "";
                                        }
                                    }
                                }

                                if (!string.IsNullOrEmpty(bestUrl))
                                {
                                    result.ResolvedUrl = bestUrl;
                                    result.IsSuccess = true;
                                    return result;
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Try next instance
                    }
                }

                // If online resolver unavailable, keep cleanUrl so user can still attempt direct stream
                result.ResolvedUrl = cleanUrl;
                result.IsSuccess = true;
                return result;
            }

            // HLS Stream (.m3u8)
            if (cleanUrl.Contains(".m3u8", StringComparison.OrdinalIgnoreCase))
            {
                result.Type = StreamType.HlsLiveStream;
                result.Title = $"Live HLS Stream [{Path.GetFileName(cleanUrl.Split('?')[0])}]";
                return result;
            }

            // DASH Stream (.mpd)
            if (cleanUrl.Contains(".mpd", StringComparison.OrdinalIgnoreCase))
            {
                result.Type = StreamType.DashStream;
                result.Title = $"MPEG-DASH Stream [{Path.GetFileName(cleanUrl.Split('?')[0])}]";
                return result;
            }

            // Audio Stream (.mp3, .aac, .ogg, .flac)
            if (cleanUrl.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                cleanUrl.EndsWith(".aac", StringComparison.OrdinalIgnoreCase) ||
                cleanUrl.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) ||
                cleanUrl.EndsWith(".flac", StringComparison.OrdinalIgnoreCase))
            {
                result.Type = StreamType.AudioStream;
                result.Title = $"Audio Stream [{Path.GetFileName(cleanUrl.Split('?')[0])}]";
                return result;
            }

            // Default Direct Media
            result.Type = StreamType.DirectMedia;
            result.Title = $"Network Stream [{Path.GetFileName(cleanUrl.Split('?')[0])}]";
            return result;
        }

        private static string? FindExecutableOnPath(string exeName)
        {
            if (File.Exists(exeName)) return Path.GetFullPath(exeName);
            string? pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(pathEnv)) return null;

            foreach (var path in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    string full = Path.Combine(path.Trim(), exeName);
                    if (File.Exists(full)) return full;
                }
                catch { }
            }
            return null;
        }
    }
}
