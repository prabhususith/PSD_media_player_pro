using System;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace OmniPlayer.Core.Audio
{
    /// <summary>
    /// Custom ISampleProvider that applies EqualizerDsp (31-band EQ, Preamp, DRC, Haas Surround, Limiter) in real-time.
    /// </summary>
    public class OmniEqualizerSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly float[] _leftBuffer = new float[4096];
        private readonly float[] _rightBuffer = new float[4096];

        public WaveFormat WaveFormat => _source.WaveFormat;

        public float PeakLevelLeft { get; private set; }
        public float PeakLevelRight { get; private set; }

        public OmniEqualizerSampleProvider(ISampleProvider source)
        {
            _source = source;
            if (source?.WaveFormat != null)
            {
                EqualizerDsp.Instance.SetSampleRate(source.WaveFormat.SampleRate);
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = _source.Read(buffer, offset, count);
            if (samplesRead <= 0)
            {
                PeakLevelLeft = 0;
                PeakLevelRight = 0;
                return 0;
            }

            var dsp = EqualizerDsp.Instance;
            int channels = WaveFormat.Channels;

            if (channels == 2)
            {
                int frameCount = samplesRead / 2;
                int processed = 0;

                while (processed < frameCount)
                {
                    int chunk = Math.Min(frameCount - processed, _leftBuffer.Length);

                    // De-interleave
                    for (int i = 0; i < chunk; i++)
                    {
                        int srcIdx = offset + (processed + i) * 2;
                        _leftBuffer[i] = buffer[srcIdx];
                        _rightBuffer[i] = buffer[srcIdx + 1];
                    }

                    // Process through 31-band EQ, Preamp, Haas delay, DRC, and Limiter
                    dsp.ProcessStereoFrame(_leftBuffer, _rightBuffer, chunk);

                    // Calculate peak levels & re-interleave
                    float maxL = 0f, maxR = 0f;
                    for (int i = 0; i < chunk; i++)
                    {
                        int dstIdx = offset + (processed + i) * 2;
                        buffer[dstIdx] = _leftBuffer[i];
                        buffer[dstIdx + 1] = _rightBuffer[i];

                        float absL = Math.Abs(_leftBuffer[i]);
                        float absR = Math.Abs(_rightBuffer[i]);
                        if (absL > maxL) maxL = absL;
                        if (absR > maxR) maxR = absR;
                    }

                    PeakLevelLeft = maxL;
                    PeakLevelRight = maxR;

                    processed += chunk;
                }
            }
            else if (channels == 1)
            {
                // Mono: process as identical stereo channels
                int processed = 0;
                while (processed < samplesRead)
                {
                    int chunk = Math.Min(samplesRead - processed, _leftBuffer.Length);
                    for (int i = 0; i < chunk; i++)
                    {
                        _leftBuffer[i] = buffer[offset + processed + i];
                        _rightBuffer[i] = _leftBuffer[i];
                    }

                    dsp.ProcessStereoFrame(_leftBuffer, _rightBuffer, chunk);

                    for (int i = 0; i < chunk; i++)
                    {
                        buffer[offset + processed + i] = (_leftBuffer[i] + _rightBuffer[i]) * 0.5f;
                    }
                    processed += chunk;
                }
            }

            return samplesRead;
        }
    }

    /// <summary>
    /// Master Audio Engine driving high-definition playback through WASAPI with real-time DSP.
    /// </summary>
    public class OmniAudioEngine : IDisposable
    {
        private static readonly Lazy<OmniAudioEngine> _instance = new(() => new OmniAudioEngine());
        public static OmniAudioEngine Instance => _instance.Value;

        private IWavePlayer? _wavePlayer;
        private WaveStream? _waveStream;
        private VolumeSampleProvider? _volumeProvider;
        private OmniEqualizerSampleProvider? _eqProvider;

        public bool IsActive => _wavePlayer != null && _waveStream != null;
        public bool IsPlaying => _wavePlayer?.PlaybackState == PlaybackState.Playing;
        public bool WasapiExclusiveMode { get; set; } = false;

        public float PeakLeft => _eqProvider?.PeakLevelLeft ?? 0f;
        public float PeakRight => _eqProvider?.PeakLevelRight ?? 0f;

        public TimeSpan Position
        {
            get => _waveStream?.CurrentTime ?? TimeSpan.Zero;
            set
            {
                if (_waveStream != null)
                {
                    try
                    {
                        _waveStream.CurrentTime = value;
                    }
                    catch
                    {
                        // Some streams might have unseekable bounds
                    }
                }
            }
        }

        public TimeSpan Duration => _waveStream?.TotalTime ?? TimeSpan.Zero;

        public float Volume
        {
            get => _volumeProvider?.Volume ?? 1.0f;
            set
            {
                if (_volumeProvider != null)
                {
                    _volumeProvider.Volume = Math.Clamp(value, 0.0f, 1.0f);
                }
            }
        }

        public event Action? PlaybackEnded;

        private OmniAudioEngine()
        {
        }

        public bool OpenMedia(string pathOrUrl)
        {
            try
            {
                Stop();
                DisposePlayback();

                if (string.IsNullOrWhiteSpace(pathOrUrl)) return false;

                // MediaFoundationReader can handle local audio, local video audio tracks, and HTTP/HTTPS streams
                _waveStream = new MediaFoundationReader(pathOrUrl);

                // Convert to IEEE 32-bit float sample provider
                var sampleProvider = _waveStream.ToSampleProvider();

                // Hook up Equalizer DSP sample provider
                _eqProvider = new OmniEqualizerSampleProvider(sampleProvider);

                // Hook up Master Volume provider
                _volumeProvider = new VolumeSampleProvider(_eqProvider);

                // Output via WASAPI or DirectSound
                try
                {
                    var shareMode = WasapiExclusiveMode 
                        ? NAudio.CoreAudioApi.AudioClientShareMode.Exclusive 
                        : NAudio.CoreAudioApi.AudioClientShareMode.Shared;
                    _wavePlayer = new WasapiOut(shareMode, 100);
                }
                catch
                {
                    // Fallback to WaveOutEvent if WASAPI device is busy or in low-capability sandbox
                    _wavePlayer = new WaveOutEvent { DesiredLatency = 100 };
                }

                _wavePlayer.Init(_volumeProvider);
                _wavePlayer.PlaybackStopped += OnPlaybackStopped;
                return true;
            }
            catch
            {
                DisposePlayback();
                return false;
            }
        }

        private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            PlaybackEnded?.Invoke();
        }

        public void Play()
        {
            _wavePlayer?.Play();
        }

        public void Pause()
        {
            _wavePlayer?.Pause();
        }

        public void Stop()
        {
            _wavePlayer?.Stop();
            if (_waveStream != null)
            {
                try { _waveStream.Position = 0; } catch { }
            }
        }

        private void DisposePlayback()
        {
            if (_wavePlayer != null)
            {
                _wavePlayer.PlaybackStopped -= OnPlaybackStopped;
                _wavePlayer.Dispose();
                _wavePlayer = null;
            }
            if (_waveStream != null)
            {
                _waveStream.Dispose();
                _waveStream = null;
            }
            _volumeProvider = null;
            _eqProvider = null;
        }

        public void Dispose()
        {
            DisposePlayback();
        }
    }
}
