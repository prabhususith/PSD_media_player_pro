using System;
using System.Collections.Generic;

namespace OmniPlayer.Core.Audio
{
    public class BiquadFilter
    {
        private float _b0 = 1f, _b1, _b2, _a1, _a2;
        private float _x1, _x2, _y1, _y2;

        public void ConfigurePeakingEQ(float sampleRate, float centerFrequency, float gainDb, float q = 1.4142f)
        {
            if (centerFrequency >= sampleRate * 0.49f) centerFrequency = sampleRate * 0.49f;
            if (centerFrequency < 10f) centerFrequency = 10f;

            float w0 = (float)(2.0 * Math.PI * centerFrequency / sampleRate);
            float alpha = (float)(Math.Sin(w0) / (2.0 * q));
            float A = (float)Math.Pow(10.0, gainDb / 40.0);
            float cosW0 = (float)Math.Cos(w0);

            float b0 = 1.0f + alpha * A;
            float b1 = -2.0f * cosW0;
            float b2 = 1.0f - alpha * A;
            float a0 = 1.0f + alpha / A;
            float a1 = -2.0f * cosW0;
            float a2 = 1.0f - alpha / A;

            _b0 = b0 / a0;
            _b1 = b1 / a0;
            _b2 = b2 / a0;
            _a1 = a1 / a0;
            _a2 = a2 / a0;
        }

        public void ConfigureLowPass(float sampleRate, float cutoffFrequency, float q = 0.7071f)
        {
            if (cutoffFrequency >= sampleRate * 0.49f) cutoffFrequency = sampleRate * 0.49f;
            if (cutoffFrequency < 10f) cutoffFrequency = 10f;

            float w0 = (float)(2.0 * Math.PI * cutoffFrequency / sampleRate);
            float cosW0 = (float)Math.Cos(w0);
            float alpha = (float)(Math.Sin(w0) / (2.0 * q));

            float b0 = (1.0f - cosW0) * 0.5f;
            float b1 = 1.0f - cosW0;
            float b2 = (1.0f - cosW0) * 0.5f;
            float a0 = 1.0f + alpha;
            float a1 = -2.0f * cosW0;
            float a2 = 1.0f - alpha;

            _b0 = b0 / a0;
            _b1 = b1 / a0;
            _b2 = b2 / a0;
            _a1 = a1 / a0;
            _a2 = a2 / a0;
        }

        public void ConfigureHighPass(float sampleRate, float cutoffFrequency, float q = 0.7071f)
        {
            if (cutoffFrequency >= sampleRate * 0.49f) cutoffFrequency = sampleRate * 0.49f;
            if (cutoffFrequency < 10f) cutoffFrequency = 10f;

            float w0 = (float)(2.0 * Math.PI * cutoffFrequency / sampleRate);
            float cosW0 = (float)Math.Cos(w0);
            float alpha = (float)(Math.Sin(w0) / (2.0 * q));

            float b0 = (1.0f + cosW0) * 0.5f;
            float b1 = -(1.0f + cosW0);
            float b2 = (1.0f + cosW0) * 0.5f;
            float a0 = 1.0f + alpha;
            float a1 = -2.0f * cosW0;
            float a2 = 1.0f - alpha;

            _b0 = b0 / a0;
            _b1 = b1 / a0;
            _b2 = b2 / a0;
            _a1 = a1 / a0;
            _a2 = a2 / a0;
        }

        public void ConfigureHighShelf(float sampleRate, float cutoffFrequency, float gainDb, float shelfSlope = 1.0f)
        {
            if (cutoffFrequency >= sampleRate * 0.49f) cutoffFrequency = sampleRate * 0.49f;
            if (cutoffFrequency < 10f) cutoffFrequency = 10f;

            float A = (float)Math.Pow(10.0, gainDb / 40.0);
            float w0 = (float)(2.0 * Math.PI * cutoffFrequency / sampleRate);
            float cosW0 = (float)Math.Cos(w0);
            float sinW0 = (float)Math.Sin(w0);
            float alpha = sinW0 / 2.0f * (float)Math.Sqrt((A + 1.0f / A) * (1.0f / shelfSlope - 1.0f) + 2.0f);
            float sqrtA2Alpha = 2.0f * (float)Math.Sqrt(A) * alpha;

            float b0 = A * ((A + 1.0f) + (A - 1.0f) * cosW0 + sqrtA2Alpha);
            float b1 = -2.0f * A * ((A - 1.0f) + (A + 1.0f) * cosW0);
            float b2 = A * ((A + 1.0f) + (A - 1.0f) * cosW0 - sqrtA2Alpha);
            float a0 = (A + 1.0f) - (A - 1.0f) * cosW0 + sqrtA2Alpha;
            float a1 = 2.0f * ((A - 1.0f) - (A + 1.0f) * cosW0);
            float a2 = (A + 1.0f) - (A - 1.0f) * cosW0 - sqrtA2Alpha;

            _b0 = b0 / a0;
            _b1 = b1 / a0;
            _b2 = b2 / a0;
            _a1 = a1 / a0;
            _a2 = a2 / a0;
        }

        public void ConfigureBandPass(float sampleRate, float centerFrequency, float q = 1.0f)
        {
            if (centerFrequency >= sampleRate * 0.49f) centerFrequency = sampleRate * 0.49f;
            if (centerFrequency < 10f) centerFrequency = 10f;

            float w0 = (float)(2.0 * Math.PI * centerFrequency / sampleRate);
            float cosW0 = (float)Math.Cos(w0);
            float sinW0 = (float)Math.Sin(w0);
            float alpha = sinW0 / (2.0f * q);

            float b0 = alpha;
            float b1 = 0.0f;
            float b2 = -alpha;
            float a0 = 1.0f + alpha;
            float a1 = -2.0f * cosW0;
            float a2 = 1.0f - alpha;

            _b0 = b0 / a0;
            _b1 = b1 / a0;
            _b2 = b2 / a0;
            _a1 = a1 / a0;
            _a2 = a2 / a0;
        }

        public float ProcessSample(float inSample)
        {
            float outSample = _b0 * inSample + _b1 * _x1 + _b2 * _x2 - _a1 * _y1 - _a2 * _y2;
            if (float.IsNaN(outSample) || float.IsInfinity(outSample))
            {
                outSample = 0.0f;
            }

            _x2 = _x1;
            _x1 = inSample;
            _y2 = _y1;
            _y1 = outSample;
            return outSample;
        }

        public void Reset()
        {
            _x1 = _x2 = _y1 = _y2 = 0;
        }
    }

    public class CircularDelayLine
    {
        private readonly float[] _buffer;
        private int _writeIndex;

        public CircularDelayLine(int maxDelaySamples)
        {
            _buffer = new float[Math.Max(maxDelaySamples + 1, 64)];
            _writeIndex = 0;
        }

        public void Write(float sample)
        {
            _buffer[_writeIndex] = sample;
            _writeIndex = (_writeIndex + 1) % _buffer.Length;
        }

        public float Read(int delaySamples)
        {
            if (delaySamples >= _buffer.Length) delaySamples = _buffer.Length - 1;
            if (delaySamples < 0) delaySamples = 0;
            int readIndex = (_writeIndex - delaySamples + _buffer.Length) % _buffer.Length;
            return _buffer[readIndex];
        }

        public void Reset()
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            _writeIndex = 0;
        }
    }

    public class SchroederReverb
    {
        private class CombFilter
        {
            private readonly float[] _buffer;
            private int _index;
            private float _filterStore;
            public float Feedback { get; set; } = 0.7f;
            public float Damping { get; set; } = 0.2f;

            public CombFilter(int size)
            {
                _buffer = new float[Math.Max(size, 8)];
                _index = 0;
            }

            public float Process(float input)
            {
                float output = _buffer[_index];
                _filterStore = (output * (1.0f - Damping)) + (_filterStore * Damping);
                _buffer[_index] = input + (_filterStore * Feedback);
                _index = (_index + 1) % _buffer.Length;
                return output;
            }

            public void Reset()
            {
                Array.Clear(_buffer, 0, _buffer.Length);
                _index = 0;
                _filterStore = 0f;
            }
        }

        private class AllpassFilter
        {
            private readonly float[] _buffer;
            private int _index;
            public float Feedback { get; set; } = 0.5f;

            public AllpassFilter(int size)
            {
                _buffer = new float[Math.Max(size, 8)];
                _index = 0;
            }

            public float Process(float input)
            {
                float bufOut = _buffer[_index];
                float temp = input + (bufOut * Feedback);
                float output = -temp * Feedback + bufOut;
                _buffer[_index] = temp;
                _index = (_index + 1) % _buffer.Length;
                return output;
            }

            public void Reset()
            {
                Array.Clear(_buffer, 0, _buffer.Length);
                _index = 0;
            }
        }

        private CombFilter[] _combsL = Array.Empty<CombFilter>();
        private CombFilter[] _combsR = Array.Empty<CombFilter>();
        private AllpassFilter[] _allpassesL = Array.Empty<AllpassFilter>();
        private AllpassFilter[] _allpassesR = Array.Empty<AllpassFilter>();
        private float _sampleRate = 48000f;
        private RoomEchoPreset _preset = RoomEchoPreset.None;

        public SchroederReverb(float sampleRate)
        {
            SetSampleRate(sampleRate);
        }

        public void SetSampleRate(float sampleRate)
        {
            _sampleRate = sampleRate;
            float s = sampleRate / 44100.0f;

            _combsL = new CombFilter[]
            {
                new CombFilter((int)(1116 * s)),
                new CombFilter((int)(1188 * s)),
                new CombFilter((int)(1277 * s)),
                new CombFilter((int)(1356 * s))
            };
            _combsR = new CombFilter[]
            {
                new CombFilter((int)(1139 * s)),
                new CombFilter((int)(1211 * s)),
                new CombFilter((int)(1299 * s)),
                new CombFilter((int)(1378 * s))
            };

            _allpassesL = new AllpassFilter[]
            {
                new AllpassFilter((int)(225 * s)),
                new AllpassFilter((int)(556 * s))
            };
            _allpassesR = new AllpassFilter[]
            {
                new AllpassFilter((int)(248 * s)),
                new AllpassFilter((int)(579 * s))
            };

            ApplyPresetParams();
        }

        public void SetPreset(RoomEchoPreset preset)
        {
            _preset = preset;
            ApplyPresetParams();
        }

        private void ApplyPresetParams()
        {
            float feedback = 0.7f;
            float damping = 0.2f;

            switch (_preset)
            {
                case RoomEchoPreset.None:
                    feedback = 0.0f;
                    damping = 0.0f;
                    break;
                case RoomEchoPreset.LivingRoom:
                    feedback = 0.65f;
                    damping = 0.45f;
                    break;
                case RoomEchoPreset.StudioMonitor:
                    feedback = 0.45f;
                    damping = 0.30f;
                    break;
                case RoomEchoPreset.ConcertHall:
                    feedback = 0.82f;
                    damping = 0.25f;
                    break;
                case RoomEchoPreset.Cathedral:
                    feedback = 0.90f;
                    damping = 0.15f;
                    break;
                case RoomEchoPreset.Cavern:
                    feedback = 0.94f;
                    damping = 0.08f;
                    break;
            }

            foreach (var c in _combsL) { c.Feedback = feedback; c.Damping = damping; }
            foreach (var c in _combsR) { c.Feedback = feedback; c.Damping = damping; }
        }

        public void Process(float inL, float inR, out float outL, out float outR)
        {
            if (_preset == RoomEchoPreset.None)
            {
                outL = 0f;
                outR = 0f;
                return;
            }

            float sumL = 0f;
            float sumR = 0f;
            for (int i = 0; i < 4; i++)
            {
                sumL += _combsL[i].Process(inL);
                sumR += _combsR[i].Process(inR);
            }

            sumL *= 0.25f;
            sumR *= 0.25f;

            for (int i = 0; i < 2; i++)
            {
                sumL = _allpassesL[i].Process(sumL);
                sumR = _allpassesR[i].Process(sumR);
            }

            outL = sumL;
            outR = sumR;
        }

        public void Reset()
        {
            foreach (var c in _combsL) c.Reset();
            foreach (var c in _combsR) c.Reset();
            foreach (var a in _allpassesL) a.Reset();
            foreach (var a in _allpassesR) a.Reset();
        }
    }

    public class TimeDomainPitchShifter
    {
        private const int BufferSize = 16384;
        private const int WindowSize = 4096;
        private const float HalfWindow = WindowSize / 2.0f;

        private readonly float[] _bufferL = new float[BufferSize];
        private readonly float[] _bufferR = new float[BufferSize];
        private int _writeIndex = 0;
        private double _phase = 0.0;

        public void Process(float inL, float inR, float pitchRatio, out float outL, out float outR)
        {
            if (Math.Abs(pitchRatio - 1.0f) < 0.001f)
            {
                outL = inL;
                outR = inR;
                return;
            }

            _bufferL[_writeIndex] = inL;
            _bufferR[_writeIndex] = inR;

            _phase += (1.0 - pitchRatio);
            while (_phase >= WindowSize) _phase -= WindowSize;
            while (_phase < 0) _phase += WindowSize;

            double offset1 = _phase;
            double offset2 = _phase + HalfWindow;
            if (offset2 >= WindowSize) offset2 -= WindowSize;

            float w1 = 0.5f * (1.0f - (float)Math.Cos(2.0 * Math.PI * offset1 / WindowSize));
            float w2 = 1.0f - w1;

            double readPos1 = (_writeIndex - offset1 + BufferSize) % BufferSize;
            double readPos2 = (_writeIndex - offset2 + BufferSize) % BufferSize;

            int idx1A = (int)readPos1;
            int idx1B = (idx1A + 1) % BufferSize;
            float frac1 = (float)(readPos1 - idx1A);

            int idx2A = (int)readPos2;
            int idx2B = (idx2A + 1) % BufferSize;
            float frac2 = (float)(readPos2 - idx2A);

            float s1L = _bufferL[idx1A] * (1.0f - frac1) + _bufferL[idx1B] * frac1;
            float s1R = _bufferR[idx1A] * (1.0f - frac1) + _bufferR[idx1B] * frac1;

            float s2L = _bufferL[idx2A] * (1.0f - frac2) + _bufferL[idx2B] * frac2;
            float s2R = _bufferR[idx2A] * (1.0f - frac2) + _bufferR[idx2B] * frac2;

            outL = s1L * w1 + s2L * w2;
            outR = s1R * w1 + s2R * w2;

            _writeIndex = (_writeIndex + 1) % BufferSize;
        }

        public void Reset()
        {
            Array.Clear(_bufferL, 0, _bufferL.Length);
            Array.Clear(_bufferR, 0, _bufferR.Length);
            _writeIndex = 0;
            _phase = 0.0;
        }
    }

    public enum BitstreamPassthroughFormat
    {
        DisabledPcm,
        DolbyDigital_AC3,
        DolbyDigitalPlus_EAC3,
        DolbyTrueHD_Atmos,
        DTS_Core,
        DTS_HD_MasterAudio,
        DTS_X
    }

    public enum SoundTechnologyMode
    {
        StandardStereo,
        DolbyAtmos3D,
        DtsX,
        Surround51,
        Surround71,
        HeadphoneHRTF
    }

    public enum RoomEchoPreset
    {
        None,
        LivingRoom,
        StudioMonitor,
        ConcertHall,
        Cathedral,
        Cavern
    }

    public class EqualizerPreset
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public float[] Gains { get; set; } = new float[31];
        public float PreampDb { get; set; } = 0.0f;
    }

    public class EqualizerDsp
    {
        public static readonly float[] Standard31Frequencies = new float[31]
        {
            20f, 25f, 31.5f, 40f, 50f, 63f, 80f, 100f, 125f, 160f,
            200f, 250f, 315f, 400f, 500f, 630f, 800f, 1000f, 1250f, 1600f,
            2000f, 2500f, 3150f, 4000f, 5000f, 6300f, 8000f, 10000f, 12500f, 16000f, 20000f
        };

        public static readonly int[] TenBandIndices = new int[10]
        {
            2,  // 31.5 Hz
            5,  // 63 Hz
            8,  // 125 Hz
            11, // 250 Hz
            14, // 500 Hz
            17, // 1000 Hz
            20, // 2000 Hz
            23, // 4000 Hz
            26, // 8000 Hz
            29  // 16000 Hz
        };

        private readonly BiquadFilter[,] _channelFilters = new BiquadFilter[2, 31];
        private readonly float[] _bandGains = new float[31];
        private float _preampDb = 0.0f;
        private float _preampLinear = 1.0f;
        private float _sampleRate = 48000.0f;

        // Core Equalizer & Legacy Settings
        public bool IsEqualizerEnabled { get; set; } = true;
        public bool Is31BandMode { get; set; } = false; // false = 10-band, true = 31-band
        public bool VoiceNormalizationEnabled { get; set; } = false;
        public bool NightModeDrcEnabled { get; set; } = false;
        public bool SpatialSurroundEnabled { get; set; } = false;
        public bool PitchPreserverEnabled { get; set; } = true;
        public BitstreamPassthroughFormat WasapiPassthrough { get; set; } = BitstreamPassthroughFormat.DisabledPcm;

        // Next-Gen Sound Technologies & Audio Processing
        private SoundTechnologyMode _activeSoundTechnology = SoundTechnologyMode.StandardStereo;
        public SoundTechnologyMode ActiveSoundTechnology
        {
            get => _activeSoundTechnology;
            set
            {
                _activeSoundTechnology = value;
                NotifyConfigurationChanged();
            }
        }

        private RoomEchoPreset _activeRoomEcho = RoomEchoPreset.None;
        public RoomEchoPreset ActiveRoomEcho
        {
            get => _activeRoomEcho;
            set
            {
                _activeRoomEcho = value;
                _reverb.SetPreset(value);
                NotifyConfigurationChanged();
            }
        }

        private float _reverbWetMix = 0.25f;
        public float ReverbWetMix
        {
            get => _reverbWetMix;
            set
            {
                _reverbWetMix = Math.Clamp(value, 0.0f, 1.0f);
                NotifyConfigurationChanged();
            }
        }

        private int _pitchSemitones = 0;
        public int PitchSemitones
        {
            get => _pitchSemitones;
            set
            {
                _pitchSemitones = Math.Clamp(value, -12, 12);
                UpdatePitchRatio();
                NotifyConfigurationChanged();
            }
        }

        private float _pitchCents = 0.0f;
        public float PitchCents
        {
            get => _pitchCents;
            set
            {
                _pitchCents = Math.Clamp(value, -100.0f, 100.0f);
                UpdatePitchRatio();
                NotifyConfigurationChanged();
            }
        }

        private bool _karaokeVocalRemoverEnabled = false;
        public bool KaraokeVocalRemoverEnabled
        {
            get => _karaokeVocalRemoverEnabled;
            set
            {
                _karaokeVocalRemoverEnabled = value;
                NotifyConfigurationChanged();
            }
        }

        private bool _dialogueClarityEnabled = false;
        public bool DialogueClarityEnabled
        {
            get => _dialogueClarityEnabled;
            set
            {
                _dialogueClarityEnabled = value;
                NotifyConfigurationChanged();
            }
        }

        private float _pitchRatio = 1.0f;

        // DRC state
        private float _drcEnvelope = 0.0f;
        private const float DrcThresholdDb = -22.0f;
        private const float DrcRatio = 4.0f;
        private const float DrcAttackCoeff = 0.01f;
        private const float DrcReleaseCoeff = 0.001f;

        // Legacy Haas delay line
        private readonly float[] _surroundDelayBufferL = new float[4800];
        private readonly float[] _surroundDelayBufferR = new float[4800];
        private int _delayIndex = 0;

        // DSP Subsystems
        private readonly SchroederReverb _reverb;
        private readonly TimeDomainPitchShifter _pitchShifter = new();

        // Dolby Atmos 3D Simulation components
        private readonly BiquadFilter _atmosHeightFilterL = new();
        private readonly BiquadFilter _atmosHeightFilterR = new();
        private readonly BiquadFilter _atmosWarmthFilter = new();
        private readonly CircularDelayLine _atmosDelayL = new(1920);
        private readonly CircularDelayLine _atmosDelayR = new(1920);

        // DTS:X Spatializer components
        private readonly BiquadFilter _dtsDialogueFilter = new();
        private readonly CircularDelayLine _dtsDecorrelatorDelay = new(1920);

        // 5.1 & 7.1 Virtual Surround components
        private readonly CircularDelayLine _surround51DelayL = new(2400);
        private readonly CircularDelayLine _surround51DelayR = new(2400);
        private readonly BiquadFilter _surround51RollOffL = new();
        private readonly BiquadFilter _surround51RollOffR = new();

        private readonly CircularDelayLine _surround71SideDelayL = new(2400);
        private readonly CircularDelayLine _surround71SideDelayR = new(2400);
        private readonly CircularDelayLine _surround71BackDelayL = new(2400);
        private readonly CircularDelayLine _surround71BackDelayR = new(2400);
        private readonly BiquadFilter _surround71RollOffL = new();
        private readonly BiquadFilter _surround71RollOffR = new();

        // Headphone HRTF (Bauer Crossfeed) components
        private readonly CircularDelayLine _hrtfDelayL = new(240);
        private readonly CircularDelayLine _hrtfDelayR = new(240);
        private readonly BiquadFilter _hrtfFilterL = new();
        private readonly BiquadFilter _hrtfFilterR = new();

        // Karaoke Vocal Remover components
        private readonly BiquadFilter _karaokeBassLpf = new();
        private readonly BiquadFilter _karaokeVocalNotch = new();

        // Dialogue Clarity component
        private readonly BiquadFilter _dialogueBoostFilter = new();

        public Dictionary<string, EqualizerPreset> FactoryPresets { get; } = new(StringComparer.OrdinalIgnoreCase);

        public event Action? ConfigurationChanged;

        public float PreampDb
        {
            get => _preampDb;
            set
            {
                _preampDb = Math.Clamp(value, -20.0f, 20.0f);
                _preampLinear = (float)Math.Pow(10.0, _preampDb / 20.0);
                NotifyConfigurationChanged();
            }
        }

        public static EqualizerDsp Instance { get; } = new EqualizerDsp();

        public EqualizerDsp(float sampleRate = 48000.0f)
        {
            _sampleRate = sampleRate;
            _reverb = new SchroederReverb(sampleRate);

            for (int ch = 0; ch < 2; ch++)
            {
                for (int b = 0; b < 31; b++)
                {
                    _channelFilters[ch, b] = new BiquadFilter();
                }
            }

            ConfigureDspFilters();
            InitializePresets();
            ApplyPreset("Flat");
        }

        public void SetSampleRate(float sampleRate)
        {
            if (sampleRate < 8000f || sampleRate > 384000f) return;
            if (Math.Abs(_sampleRate - sampleRate) < 1.0f) return;

            _sampleRate = sampleRate;
            for (int b = 0; b < 31; b++)
            {
                for (int ch = 0; ch < 2; ch++)
                {
                    _channelFilters[ch, b].ConfigurePeakingEQ(_sampleRate, Standard31Frequencies[b], _bandGains[b]);
                }
            }

            ConfigureDspFilters();
            _reverb.SetSampleRate(sampleRate);
        }

        private void ConfigureDspFilters()
        {
            // Dolby Atmos: High-shelf >7kHz (+3.5dB) for height elevation, center warmth peaking at 320Hz (+1.8dB)
            _atmosHeightFilterL.ConfigureHighShelf(_sampleRate, 7500f, 3.5f);
            _atmosHeightFilterR.ConfigureHighShelf(_sampleRate, 7500f, 3.5f);
            _atmosWarmthFilter.ConfigurePeakingEQ(_sampleRate, 320f, 1.8f, 1.0f);

            // DTS:X: Dialogue peaking boost (+4dB at 2500Hz)
            _dtsDialogueFilter.ConfigurePeakingEQ(_sampleRate, 2500f, 4.0f, 1.2f);

            // 5.1 & 7.1 Virtual Surround: Lowpass roll-off at 7kHz
            _surround51RollOffL.ConfigureLowPass(_sampleRate, 7000f, 0.7071f);
            _surround51RollOffR.ConfigureLowPass(_sampleRate, 7000f, 0.7071f);
            _surround71RollOffL.ConfigureLowPass(_sampleRate, 7000f, 0.7071f);
            _surround71RollOffR.ConfigureLowPass(_sampleRate, 7000f, 0.7071f);

            // Headphone HRTF: Bauer interaural level difference lowpass filter at 700Hz
            _hrtfFilterL.ConfigureLowPass(_sampleRate, 700f, 0.7071f);
            _hrtfFilterR.ConfigureLowPass(_sampleRate, 700f, 0.7071f);

            // Karaoke Vocal Remover: Bass preservation LPF (<140Hz) and vocal formant bandpass
            _karaokeBassLpf.ConfigureLowPass(_sampleRate, 140f, 0.7071f);
            _karaokeVocalNotch.ConfigureBandPass(_sampleRate, 1500f, 0.8f);

            // Smart Dialogue Enhancer: Peaking boost at 2500Hz (+4.5dB)
            _dialogueBoostFilter.ConfigurePeakingEQ(_sampleRate, 2500f, 4.5f, 1.1f);
        }

        private void UpdatePitchRatio()
        {
            float totalSemitones = _pitchSemitones + (_pitchCents / 100.0f);
            _pitchRatio = (float)Math.Pow(2.0, totalSemitones / 12.0);
        }

        public void NotifyConfigurationChanged()
        {
            ConfigurationChanged?.Invoke();
        }

        public float[] GetTenBandGainsCopy()
        {
            float[] gains = new float[10];
            for (int i = 0; i < 10; i++)
            {
                gains[i] = GetTenBandGain(i);
            }
            return gains;
        }

        public void SetBandGain(int bandIndex, float gainDb)
        {
            if (bandIndex < 0 || bandIndex >= 31) return;
            gainDb = Math.Clamp(gainDb, -20.0f, 20.0f);
            _bandGains[bandIndex] = gainDb;

            for (int ch = 0; ch < 2; ch++)
            {
                _channelFilters[ch, bandIndex].ConfigurePeakingEQ(_sampleRate, Standard31Frequencies[bandIndex], gainDb);
            }
            NotifyConfigurationChanged();
        }

        public float GetBandGain(int bandIndex)
        {
            if (bandIndex < 0 || bandIndex >= 31) return 0.0f;
            return _bandGains[bandIndex];
        }

        public void SetTenBandGain(int tenBandIndex, float gainDb)
        {
            if (tenBandIndex < 0 || tenBandIndex >= 10) return;
            int target31Index = TenBandIndices[tenBandIndex];
            SetBandGain(target31Index, gainDb);

            // Interpolate neighboring bands for smooth 10-band response
            if (target31Index > 0) SetBandGain(target31Index - 1, gainDb * 0.75f);
            if (target31Index < 30) SetBandGain(target31Index + 1, gainDb * 0.75f);
            NotifyConfigurationChanged();
        }

        public float GetTenBandGain(int tenBandIndex)
        {
            if (tenBandIndex < 0 || tenBandIndex >= 10) return 0.0f;
            return GetBandGain(TenBandIndices[tenBandIndex]);
        }

        public void ApplyPreset(string presetName)
        {
            if (FactoryPresets.TryGetValue(presetName, out var preset))
            {
                _preampDb = preset.PreampDb;
                _preampLinear = (float)Math.Pow(10.0, _preampDb / 20.0);
                for (int i = 0; i < 31; i++)
                {
                    _bandGains[i] = preset.Gains[i];
                    for (int ch = 0; ch < 2; ch++)
                    {
                        _channelFilters[ch, i].ConfigurePeakingEQ(_sampleRate, Standard31Frequencies[i], preset.Gains[i]);
                    }
                }
                NotifyConfigurationChanged();
            }
        }

        private void InitializePresets()
        {
            float[] From10(float b0, float b1, float b2, float b3, float b4, float b5, float b6, float b7, float b8, float b9)
            {
                float[] r = new float[31];
                float[] ten = new[] { b0, b1, b2, b3, b4, b5, b6, b7, b8, b9 };
                for (int i = 0; i < 10; i++)
                {
                    int idx = TenBandIndices[i];
                    r[idx] = ten[i];
                    if (idx > 0) r[idx - 1] = ten[i] * 0.8f;
                    if (idx < 30) r[idx + 1] = ten[i] * 0.8f;
                }
                return r;
            }

            FactoryPresets["Flat"] = new EqualizerPreset { Name = "Flat", Category = "General", Gains = new float[31], PreampDb = 0 };
            FactoryPresets["Rock"] = new EqualizerPreset { Name = "Rock", Category = "Genre", Gains = From10(4.5f, 3.5f, 2.0f, -0.5f, -1.5f, -1.0f, 1.0f, 3.0f, 4.0f, 4.5f), PreampDb = -2.0f };
            FactoryPresets["Pop"] = new EqualizerPreset { Name = "Pop", Category = "Genre", Gains = From10(-1.5f, -0.5f, 1.5f, 3.0f, 4.0f, 3.5f, 1.5f, 0.0f, 1.5f, 2.0f), PreampDb = -1.5f };
            FactoryPresets["Bass Boost"] = new EqualizerPreset { Name = "Bass Boost", Category = "Acoustic", Gains = From10(7.0f, 6.0f, 5.0f, 3.5f, 1.5f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f), PreampDb = -4.0f };
            FactoryPresets["Vocal"] = new EqualizerPreset { Name = "Vocal", Category = "Dialogue", Gains = From10(-3.0f, -2.0f, -1.0f, 1.0f, 4.0f, 5.0f, 4.0f, 2.0f, 0.0f, -1.0f), PreampDb = -1.0f };
            FactoryPresets["Techno"] = new EqualizerPreset { Name = "Techno", Category = "Electronic", Gains = From10(5.0f, 4.5f, 2.0f, 0.0f, -2.0f, 0.0f, 2.5f, 4.5f, 5.0f, 5.0f), PreampDb = -2.5f };
            FactoryPresets["Classical"] = new EqualizerPreset { Name = "Classical", Category = "Acoustic", Gains = From10(4.0f, 3.0f, 2.5f, 2.0f, -1.0f, -1.0f, 0.0f, 2.0f, 3.0f, 3.5f), PreampDb = -1.5f };
            FactoryPresets["Theater"] = new EqualizerPreset { Name = "Theater", Category = "Cinema", Gains = From10(4.0f, 3.0f, 1.0f, -1.0f, -1.5f, 1.0f, 3.5f, 4.0f, 3.0f, 2.0f), PreampDb = -2.0f };
            FactoryPresets["Acoustic"] = new EqualizerPreset { Name = "Acoustic", Category = "Acoustic", Gains = From10(3.5f, 3.0f, 2.0f, 1.0f, 1.5f, 2.0f, 3.0f, 3.5f, 2.5f, 2.0f), PreampDb = -1.5f };
            FactoryPresets["Electronic"] = new EqualizerPreset { Name = "Electronic", Category = "Electronic", Gains = From10(4.5f, 3.5f, 1.0f, 0.0f, -2.0f, 1.5f, 0.5f, 1.5f, 4.0f, 4.5f), PreampDb = -2.0f };
            FactoryPresets["Dance"] = new EqualizerPreset { Name = "Dance", Category = "Electronic", Gains = From10(6.0f, 5.0f, 2.5f, 0.0f, 1.0f, 2.5f, 3.5f, 2.5f, 1.0f, 0.0f), PreampDb = -3.0f };
            FactoryPresets["Deep Bass"] = new EqualizerPreset { Name = "Deep Bass", Category = "Bass", Gains = From10(9.0f, 7.5f, 5.0f, 2.0f, 0.0f, -1.0f, -2.0f, -2.0f, -1.0f, 0.0f), PreampDb = -5.0f };
            FactoryPresets["Hip-Hop"] = new EqualizerPreset { Name = "Hip-Hop", Category = "Genre", Gains = From10(6.0f, 5.0f, 2.0f, 1.0f, -1.0f, -1.0f, 1.0f, -0.5f, 2.0f, 3.0f), PreampDb = -3.0f };
            FactoryPresets["Jazz"] = new EqualizerPreset { Name = "Jazz", Category = "Genre", Gains = From10(3.0f, 2.0f, 1.0f, 1.5f, -1.5f, -1.5f, 0.0f, 1.5f, 2.5f, 3.0f), PreampDb = -1.5f };
            FactoryPresets["Latin"] = new EqualizerPreset { Name = "Latin", Category = "Genre", Gains = From10(3.5f, 2.0f, 0.0f, 0.0f, -1.0f, -1.0f, -1.0f, 1.0f, 3.0f, 4.0f), PreampDb = -1.5f };
            FactoryPresets["Metal"] = new EqualizerPreset { Name = "Metal", Category = "Genre", Gains = From10(5.0f, 3.5f, 1.5f, 0.0f, -3.0f, -1.0f, 2.0f, 4.5f, 5.5f, 6.0f), PreampDb = -3.0f };
            FactoryPresets["Piano"] = new EqualizerPreset { Name = "Piano", Category = "Acoustic", Gains = From10(2.5f, 1.5f, 0.0f, 1.5f, 2.0f, 1.5f, 2.5f, 3.0f, 2.0f, 1.5f), PreampDb = -1.0f };
            FactoryPresets["R&B"] = new EqualizerPreset { Name = "R&B", Category = "Genre", Gains = From10(3.0f, 6.0f, 4.5f, 1.5f, -1.5f, -1.0f, 1.5f, 2.5f, 3.0f, 3.5f), PreampDb = -3.0f };
            FactoryPresets["Treble Boost"] = new EqualizerPreset { Name = "Treble Boost", Category = "Highs", Gains = From10(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.5f, 3.5f, 6.0f, 7.5f, 8.5f), PreampDb = -4.0f };
            FactoryPresets["Live"] = new EqualizerPreset { Name = "Live", Category = "Ambiance", Gains = From10(-2.0f, 0.0f, 2.0f, 3.0f, 3.5f, 3.5f, 2.5f, 2.0f, 1.5f, 1.0f), PreampDb = -1.5f };
            FactoryPresets["Club"] = new EqualizerPreset { Name = "Club", Category = "Electronic", Gains = From10(0.0f, 0.0f, 4.0f, 3.5f, 3.5f, 3.5f, 2.0f, 0.0f, 0.0f, 0.0f), PreampDb = -2.0f };
            FactoryPresets["Party"] = new EqualizerPreset { Name = "Party", Category = "Genre", Gains = From10(4.5f, 4.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 4.0f, 4.5f), PreampDb = -2.5f };
            FactoryPresets["Soft"] = new EqualizerPreset { Name = "Soft", Category = "Acoustic", Gains = From10(2.5f, 1.0f, 0.0f, -1.0f, -1.0f, 0.0f, 1.5f, 3.0f, 4.0f, 5.0f), PreampDb = -2.0f };
            FactoryPresets["Reggae"] = new EqualizerPreset { Name = "Reggae", Category = "Genre", Gains = From10(0.0f, 0.0f, -0.5f, -3.0f, 0.0f, 3.5f, 3.5f, 1.5f, 0.5f, 0.0f), PreampDb = -1.5f };
            FactoryPresets["Lounge"] = new EqualizerPreset { Name = "Lounge", Category = "Ambiance", Gains = From10(-2.0f, -1.0f, 0.0f, 2.0f, 3.0f, 1.5f, 0.0f, -1.0f, 1.5f, 1.0f), PreampDb = -1.0f };
            FactoryPresets["Spoken Word"] = new EqualizerPreset { Name = "Spoken Word", Category = "Dialogue", Gains = From10(-6.0f, -4.0f, -2.0f, 1.0f, 4.5f, 5.5f, 3.5f, 1.0f, -2.0f, -4.0f), PreampDb = 0.0f };
            FactoryPresets["Heavy Metal"] = new EqualizerPreset { Name = "Heavy Metal", Category = "Genre", Gains = From10(6.0f, 4.5f, 2.0f, 0.0f, -2.5f, 0.0f, 2.5f, 5.0f, 6.0f, 6.5f), PreampDb = -3.5f };
            FactoryPresets["Voice Clarity"] = new EqualizerPreset { Name = "Voice Clarity", Category = "Dialogue", Gains = From10(-4.0f, -2.0f, 0.0f, 2.0f, 5.0f, 6.0f, 4.0f, 2.0f, 0.0f, -2.0f), PreampDb = -1.0f };
            FactoryPresets["Arena"] = new EqualizerPreset { Name = "Arena", Category = "Ambiance", Gains = From10(5.0f, 4.0f, 1.5f, 0.0f, 0.0f, 1.0f, 2.5f, 4.0f, 5.0f, 4.0f), PreampDb = -2.5f };
            FactoryPresets["Gaming"] = new EqualizerPreset { Name = "Gaming", Category = "Gaming", Gains = From10(5.5f, 4.0f, 1.0f, -1.0f, 0.5f, 2.0f, 4.0f, 5.0f, 4.5f, 3.5f), PreampDb = -2.5f };
            FactoryPresets["Night Mode"] = new EqualizerPreset { Name = "Night Mode", Category = "Utility", Gains = From10(-5.0f, -3.5f, -2.0f, 0.0f, 2.5f, 3.0f, 1.5f, -1.0f, -3.0f, -5.0f), PreampDb = 1.0f };
        }

        public void ProcessStereoFrame(float[] leftChannel, float[] rightChannel, int frameCount)
        {
            ProcessAudioBlock(leftChannel, rightChannel, frameCount);
        }

        public void ProcessAudioBlock(float[] leftChannel, float[] rightChannel, int sampleCount = -1)
        {
            if (sampleCount <= 0 || sampleCount > leftChannel.Length)
            {
                sampleCount = leftChannel.Length;
            }

            for (int i = 0; i < sampleCount; i++)
            {
                float left = leftChannel[i] * _preampLinear;
                float right = rightChannel[i] * _preampLinear;

                // 1. Karaoke Vocal Remover / Suppressor
                if (KaraokeVocalRemoverEnabled)
                {
                    float mono = (left + right) * 0.5f;
                    float bass = _karaokeBassLpf.ProcessSample(mono);
                    float vocalBand = _karaokeVocalNotch.ProcessSample(mono);

                    // Center vocal cancellation with punchy bass preservation (<140Hz)
                    float diff = (left - right) * 0.7071f;
                    left = diff + bass + (left - mono) * 0.15f;
                    right = -diff + bass + (right - mono) * 0.15f;
                }

                // 2. Dialogue Clarity Boost (Smart Dialogue Enhancer)
                if (DialogueClarityEnabled)
                {
                    float center = (left + right) * 0.5f;
                    float boostedCenter = _dialogueBoostFilter.ProcessSample(center);
                    float speechDiff = (boostedCenter - center) * 0.85f;
                    left += speechDiff;
                    right += speechDiff;
                }

                // 3. Equalizer Filter Bank
                if (IsEqualizerEnabled)
                {
                    if (Is31BandMode)
                    {
                        for (int b = 0; b < 31; b++)
                        {
                            left = _channelFilters[0, b].ProcessSample(left);
                            right = _channelFilters[1, b].ProcessSample(right);
                        }
                    }
                    else
                    {
                        for (int b = 0; b < 10; b++)
                        {
                            int idx = TenBandIndices[b];
                            left = _channelFilters[0, idx].ProcessSample(left);
                            right = _channelFilters[1, idx].ProcessSample(right);
                        }
                    }
                }

                // 4. Next-Gen Sound Technology Modes
                switch (ActiveSoundTechnology)
                {
                    case SoundTechnologyMode.DolbyAtmos3D:
                    {
                        // Height spatializer (+3.5dB > 7.5kHz for perceived vertical elevation)
                        left = _atmosHeightFilterL.ProcessSample(left);
                        right = _atmosHeightFilterR.ProcessSample(right);

                        // Subtle center warmth (320Hz)
                        float center = (left + right) * 0.5f;
                        float warmCenter = _atmosWarmthFilter.ProcessSample(center);
                        float warmDiff = (warmCenter - center) * 0.25f;
                        left += warmDiff;
                        right += warmDiff;

                        // Haas delay crossfeed (18ms)
                        int delay18ms = (int)(_sampleRate * 0.018f);
                        float delayedL = _atmosDelayL.Read(delay18ms);
                        float delayedR = _atmosDelayR.Read(delay18ms);
                        _atmosDelayL.Write(left);
                        _atmosDelayR.Write(right);

                        left = left * 0.82f + delayedR * 0.28f;
                        right = right * 0.82f + delayedL * 0.28f;
                        break;
                    }

                    case SoundTechnologyMode.DtsX:
                    {
                        // Dialogue extraction & boost (+4dB at 1.5-3.5kHz)
                        float center = (left + right) * 0.5f;
                        float dialogue = _dtsDialogueFilter.ProcessSample(center);
                        float side = (left - right) * 0.5f;

                        // Wide stereo de-correlation (12ms)
                        int delay12ms = (int)(_sampleRate * 0.012f);
                        float delayedSide = _dtsDecorrelatorDelay.Read(delay12ms);
                        _dtsDecorrelatorDelay.Write(side);

                        float decorrelatedSide = side * 1.35f - delayedSide * 0.35f;
                        left = dialogue + decorrelatedSide;
                        right = dialogue - decorrelatedSide;
                        break;
                    }

                    case SoundTechnologyMode.Surround51:
                    {
                        float center = (left + right) * 0.7071f;
                        float surroundL = left - 0.5f * right;
                        float surroundR = right - 0.5f * left;

                        int delay25ms = (int)(_sampleRate * 0.025f);
                        float dSurroundL = _surround51DelayL.Read(delay25ms);
                        float dSurroundR = _surround51DelayR.Read(delay25ms);
                        _surround51DelayL.Write(surroundL);
                        _surround51DelayR.Write(surroundR);

                        // High frequency roll-off for realistic rear perception
                        dSurroundL = _surround51RollOffL.ProcessSample(dSurroundL);
                        dSurroundR = _surround51RollOffR.ProcessSample(dSurroundR);

                        left = 0.72f * left + 0.35f * center + 0.40f * dSurroundL - 0.22f * dSurroundR;
                        right = 0.72f * right + 0.35f * center + 0.40f * dSurroundR - 0.22f * dSurroundL;
                        break;
                    }

                    case SoundTechnologyMode.Surround71:
                    {
                        float center = (left + right) * 0.7071f;
                        float sideL = left - 0.5f * right;
                        float sideR = right - 0.5f * left;

                        int delay14ms = (int)(_sampleRate * 0.014f);
                        int delay28ms = (int)(_sampleRate * 0.028f);

                        float dSideL = _surround71SideDelayL.Read(delay14ms);
                        float dSideR = _surround71SideDelayR.Read(delay14ms);
                        _surround71SideDelayL.Write(sideL);
                        _surround71SideDelayR.Write(sideR);

                        float dBackL = _surround71BackDelayL.Read(delay28ms);
                        float dBackR = _surround71BackDelayR.Read(delay28ms);
                        _surround71BackDelayL.Write(sideL);
                        _surround71BackDelayR.Write(sideR);

                        dBackL = _surround71RollOffL.ProcessSample(dBackL);
                        dBackR = _surround71RollOffR.ProcessSample(dBackR);

                        left = 0.68f * left + 0.32f * center + 0.32f * dSideL + 0.38f * dBackL - 0.20f * dBackR;
                        right = 0.68f * right + 0.32f * center + 0.32f * dSideR + 0.38f * dBackR - 0.20f * dBackL;
                        break;
                    }

                    case SoundTechnologyMode.HeadphoneHRTF:
                    {
                        // Bauer stereophonic-to-binaural crossfeed (ITD ~0.3ms + ILD lowpass ~700Hz)
                        int delayHrtf = Math.Max(1, (int)(_sampleRate * 0.0003f));
                        float delayedL = _hrtfDelayL.Read(delayHrtf);
                        float delayedR = _hrtfDelayR.Read(delayHrtf);
                        _hrtfDelayL.Write(left);
                        _hrtfDelayR.Write(right);

                        float crossL = _hrtfFilterL.ProcessSample(delayedR) * 0.42f;
                        float crossR = _hrtfFilterR.ProcessSample(delayedL) * 0.42f;

                        left = left * 0.82f + crossL;
                        right = right * 0.82f + crossR;
                        break;
                    }

                    case SoundTechnologyMode.StandardStereo:
                    default:
                    {
                        // Legacy Haas Spatial Surround if enabled
                        if (SpatialSurroundEnabled)
                        {
                            float diffL = (left - right) * 0.35f;
                            float diffR = (right - left) * 0.35f;

                            int delaySamples = (int)(_sampleRate * 0.016f);
                            int readIndex = (_delayIndex - delaySamples + _surroundDelayBufferL.Length) % _surroundDelayBufferL.Length;

                            float delayedDiffL = _surroundDelayBufferL[readIndex];
                            float delayedDiffR = _surroundDelayBufferR[readIndex];

                            _surroundDelayBufferL[_delayIndex] = diffL;
                            _surroundDelayBufferR[_delayIndex] = diffR;
                            _delayIndex = (_delayIndex + 1) % _surroundDelayBufferL.Length;

                            left = left * 0.85f + delayedDiffL;
                            right = right * 0.85f + delayedDiffR;
                        }
                        break;
                    }
                }

                // 5. Room Echo & Reverb (Schroeder Architecture with Wet/Dry mix)
                if (ActiveRoomEcho != RoomEchoPreset.None && ReverbWetMix > 0.001f)
                {
                    _reverb.Process(left, right, out float revL, out float revR);
                    left = left * (1.0f - ReverbWetMix) + revL * ReverbWetMix;
                    right = right * (1.0f - ReverbWetMix) + revR * ReverbWetMix;
                }

                // 6. Pitch Shifter / Tuning (-12 to +12 semitones, cents)
                if (Math.Abs(_pitchRatio - 1.0f) > 0.001f)
                {
                    _pitchShifter.Process(left, right, _pitchRatio, out left, out right);
                }

                // 7. Dynamic Range Compressor (Night Mode)
                if (NightModeDrcEnabled)
                {
                    float peak = Math.Max(Math.Abs(left), Math.Abs(right));
                    float peakDb = (peak > 0.00001f) ? (float)(20.0 * Math.Log10(peak)) : -100.0f;

                    float targetGainDb = 0.0f;
                    if (peakDb > DrcThresholdDb)
                    {
                        targetGainDb = (DrcThresholdDb - peakDb) * (1.0f - (1.0f / DrcRatio));
                    }

                    float targetGainLinear = (float)Math.Pow(10.0, targetGainDb / 20.0);
                    float coeff = (targetGainLinear < _drcEnvelope) ? DrcAttackCoeff : DrcReleaseCoeff;
                    _drcEnvelope += coeff * (targetGainLinear - _drcEnvelope);

                    left *= _drcEnvelope;
                    right *= _drcEnvelope;
                }

                // 8. Voice Normalization & Master Limiter
                if (VoiceNormalizationEnabled)
                {
                    left = (float)Math.Tanh(left * 1.25f);
                    right = (float)Math.Tanh(right * 1.25f);
                }
                else
                {
                    left = Math.Clamp(left, -1.0f, 1.0f);
                    right = Math.Clamp(right, -1.0f, 1.0f);
                }

                leftChannel[i] = left;
                rightChannel[i] = right;
            }
        }
    }
}
