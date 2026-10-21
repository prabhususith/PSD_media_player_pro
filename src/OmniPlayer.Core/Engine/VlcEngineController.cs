using System;
using System.IO;
using LibVLCSharp.Shared;

namespace OmniPlayer.Core.Engine
{
    /// <summary>
    /// Master multimedia engine wrapper around LibVLC 3.0.24 LTS.
    /// Provides hardware acceleration (D3D11VA), universal codec decoding,
    /// 100% native live IPTV/HLS (.m3u8) streaming, and real-time audio equalizer control.
    /// </summary>
    public class VlcEngineController : IDisposable
    {
        private static readonly Lazy<VlcEngineController> _instance = new(() => new VlcEngineController());
        public static VlcEngineController Instance => _instance.Value;

        private LibVLC? _libVLC;
        private MediaPlayer? _mediaPlayer;
        private Equalizer? _vlcEqualizer;
        private bool _isInitialized = false;

        public bool IsAvailable => _isInitialized && _mediaPlayer != null;
        public bool IsPlaying => _mediaPlayer?.IsPlaying ?? false;

        public TimeSpan Time => TimeSpan.FromMilliseconds(_mediaPlayer?.Time ?? 0);
        public TimeSpan Length => TimeSpan.FromMilliseconds(_mediaPlayer?.Length ?? 0);
        public float Position
        {
            get => _mediaPlayer?.Position ?? 0f;
            set
            {
                if (_mediaPlayer != null)
                {
                    _mediaPlayer.Position = Math.Clamp(value, 0f, 1f);
                }
            }
        }

        public int Volume
        {
            get => _mediaPlayer?.Volume ?? 80;
            set
            {
                if (_mediaPlayer != null)
                {
                    _mediaPlayer.Volume = Math.Clamp(value, 0, 100);
                }
            }
        }

        public bool Mute
        {
            get => _mediaPlayer?.Mute ?? false;
            set
            {
                if (_mediaPlayer != null)
                {
                    _mediaPlayer.Mute = value;
                }
            }
        }

        public event Action<TimeSpan>? TimeChanged;
        public event Action<TimeSpan>? LengthChanged;
        public event Action? EndReached;
        public event Action? Playing;
        public event Action? Paused;
        public event Action? Stopped;
        public event Action<string>? ErrorOccurred;

        private VlcEngineController()
        {
            InitializeCore();
        }

        public void InitializeCore()
        {
            if (_isInitialized) return;

            try
            {
                // Dynamic multi-path discovery for LibVLC native binaries and plugins
                string baseDir = AppContext.BaseDirectory;
                string vlcArch = Environment.Is64BitProcess ? "win-x64" : "win-x86";
                string[] candidateDirs = new[]
                {
                    Path.Combine(baseDir, "libvlc", vlcArch),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libvlc", vlcArch),
                    Path.Combine(baseDir),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory),
                    @"C:\Program Files\VideoLAN\VLC"
                };

                bool coreInit = false;
                foreach (var dir in candidateDirs)
                {
                    if (Directory.Exists(dir) && File.Exists(Path.Combine(dir, "libvlc.dll")))
                    {
                        string pluginsDir = Path.Combine(dir, "plugins");
                        if (Directory.Exists(pluginsDir))
                        {
                            Environment.SetEnvironmentVariable("VLC_PLUGIN_PATH", pluginsDir);
                        }
                        LibVLCSharp.Shared.Core.Initialize(dir);
                        coreInit = true;
                        break;
                    }
                }

                if (!coreInit)
                {
                    LibVLCSharp.Shared.Core.Initialize();
                }

                // High-performance VLC engine configuration tailored for HLS/IPTV stability
                string[] options = new[]
                {
                    "--avcodec-hw=any",            // Zero-copy hardware acceleration with automatic safe fallback
                    "--no-mouse-events",           // Pass mouse events directly to host WPF window
                    "--no-keyboard-events",        // Pass keyboard navigation & shortcuts to host window
                    "--no-video-title-show",       // Suppress native VLC video title banner
                    "--network-caching=3000",      // 3s network caching for smooth IPTV HLS
                    "--live-caching=2000",         // 2s live broadcast caching
                    "--file-caching=1000",         // 1s local file caching
                    "--audio-resampler=soxr",      // High fidelity audio resampler
                    "--audio-time-stretch",        // YouTube-style audio pitch preservation during slow motion & speedup
                    "--http-user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                    "--http-referrer=https://www.google.com/",
                    "--no-sub-autodetect-fuzzy",   // Strict subtitle detection
                    "--drop-late-frames",          // Keep audio/video in perfect sync
                    "--skip-frames"                // Skip corrupt frames gracefully
                };

                _libVLC = new LibVLC(options);
                _mediaPlayer = new MediaPlayer(_libVLC);

                // Hook internal events
                _mediaPlayer.TimeChanged += (s, e) => TimeChanged?.Invoke(TimeSpan.FromMilliseconds(e.Time));
                _mediaPlayer.LengthChanged += (s, e) => LengthChanged?.Invoke(TimeSpan.FromMilliseconds(e.Length));
                _mediaPlayer.EndReached += (s, e) => EndReached?.Invoke();
                _mediaPlayer.Playing += (s, e) =>
                {
                    // Re-apply current EQ when playback starts
                    var dsp = OmniPlayer.Core.Audio.EqualizerDsp.Instance;
                    ApplyEqualizer(dsp.PreampDb, dsp.GetTenBandGainsCopy());
                    Playing?.Invoke();
                };
                _mediaPlayer.Paused += (s, e) => Paused?.Invoke();
                _mediaPlayer.Stopped += (s, e) => Stopped?.Invoke();
                _mediaPlayer.EncounteredError += (s, e) => ErrorOccurred?.Invoke("VLC playback engine encountered a media error.");

                // Auto-sync Equalizer from EqualizerDsp
                OmniPlayer.Core.Audio.EqualizerDsp.Instance.ConfigurationChanged += () =>
                {
                    if (_mediaPlayer != null)
                    {
                        var dsp = OmniPlayer.Core.Audio.EqualizerDsp.Instance;
                        ApplyEqualizer(dsp.PreampDb, dsp.GetTenBandGainsCopy());
                    }
                };

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                _isInitialized = false;
                ErrorOccurred?.Invoke($"VLC initialization failed: {ex.Message}");
            }
        }

        public MediaPlayer? GetMediaPlayer() => _mediaPlayer;

        public bool Play(string pathOrUrl)
        {
            if (!_isInitialized || _libVLC == null || _mediaPlayer == null)
            {
                return false;
            }

            try
            {
                Media media;
                if (Uri.TryCreate(pathOrUrl, UriKind.Absolute, out var uri) && 
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == "rtsp" || uri.Scheme == "rtmp"))
                {
                    media = new Media(_libVLC, uri);
                    // Rock-solid HLS and Live TV network streaming options
                    media.AddOption(":network-caching=3000");
                    media.AddOption(":live-caching=2000");
                    media.AddOption(":http-reconnect");
                }
                else
                {
                    media = new Media(_libVLC, pathOrUrl, FromType.FromPath);
                    media.AddOption(":file-caching=1000");
                }

                // Apply initial equalizer profile
                var dsp = OmniPlayer.Core.Audio.EqualizerDsp.Instance;
                ApplyEqualizer(dsp.PreampDb, dsp.GetTenBandGainsCopy());

                return _mediaPlayer.Play(media);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex.Message);
                return false;
            }
        }

        public void Pause()
        {
            _mediaPlayer?.Pause();
        }

        public void Resume()
        {
            if (_mediaPlayer != null && !_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Play();
            }
        }

        public void Stop()
        {
            _mediaPlayer?.Stop();
        }

        public void SeekToTime(TimeSpan time)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Time = (long)time.TotalMilliseconds;
            }
        }

        public void SetRate(float rate)
        {
            _mediaPlayer?.SetRate(Math.Clamp(rate, 0.05f, 4.0f));
        }

        public void SetAspectRatio(string aspect)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.AspectRatio = (aspect == "Auto" || aspect == "Fill") ? null : aspect;
            }
        }

        public void SetDeinterlace(string mode)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.SetDeinterlace(mode == "None" ? null : mode.ToLowerInvariant());
            }
        }

        public void ApplyEqualizer(float preampDb, float[] tenBandGains)
        {
            if (_mediaPlayer == null || tenBandGains == null) return;

            try
            {
                _vlcEqualizer ??= new Equalizer();
                _vlcEqualizer.SetPreamp(preampDb);
                uint bands = Math.Min(_vlcEqualizer.BandCount, (uint)tenBandGains.Length);
                for (uint i = 0; i < bands; i++)
                {
                    _vlcEqualizer.SetAmp(tenBandGains[i], i);
                }
                _mediaPlayer.SetEqualizer(_vlcEqualizer);
            }
            catch
            {
                // Headless or stub fallback
            }
        }

        public bool TakeSnapshot(string outputPath, uint width = 0, uint height = 0)
        {
            if (_mediaPlayer != null)
            {
                return _mediaPlayer.TakeSnapshot(0, outputPath, width, height);
            }
            return false;
        }

        public void NextFrame()
        {
            _mediaPlayer?.NextFrame();
        }

        public void SetAudioChannel(int channel)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.SetChannel((AudioOutputChannel)channel);
            }
        }

        public void Dispose()
        {
            _vlcEqualizer?.Dispose();
            _vlcEqualizer = null;
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();
            _mediaPlayer = null;
            _libVLC = null;
            _isInitialized = false;
        }
    }
}
