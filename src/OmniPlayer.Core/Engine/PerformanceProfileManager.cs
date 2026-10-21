using System;

namespace OmniPlayer.Core.Engine
{
    public enum PerformanceTier
    {
        Tier1_EcoBatterySaver = 1,
        Tier2_StandardLowEnd = 2,
        Tier3_BalancedDefault = 3,
        Tier4_HighPerformanceEnthusiast = 4,
        Tier5_MasterExtremeGpu = 5
    }

    public enum VideoScalingAlgorithm
    {
        FastBilinear,
        Bicubic,
        Spline36,
        Lanczos3,
        EWA_LanczosSharp_Jinc,
        FSRCNNX_Neural_8Pass,
        Anime4K_GLSL_Upscale
    }

    public enum DebandFilterLevel
    {
        Off,
        BasicFast,
        HighQualityMedium,
        ExtremePrecision
    }

    public enum DeinterlaceEngine
    {
        None,
        YADIF_Auto,
        BWDIF_DoubleRate
    }

    public enum HdrToneMappingCurve
    {
        None,
        Reinhard,
        Mobius,
        Hable,
        BT2390_EETF
    }

    public class PerformanceSettings
    {
        public PerformanceTier Tier { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public VideoScalingAlgorithm ScalingAlgorithm { get; set; }
        public bool HardwareDecodeCopyDisabled { get; set; }
        public int FrameDropThresholdFps { get; set; }
        public bool BackgroundThrottlingEnabled { get; set; }
        public int BackgroundTargetFps { get; set; }
        public int MemoryCacheSizeMb { get; set; }
        public DebandFilterLevel DebandMode { get; set; }
        public DeinterlaceEngine Deinterlacing { get; set; }
        public bool SmoothMotionInterpolation { get; set; }
        public HdrToneMappingCurve ToneMapping { get; set; }
        public bool Precision16BitFloatProcessing { get; set; }
        public bool Unlock8K60Pipeline { get; set; }
        public bool ShaderChainsActive { get; set; }
        public string ActiveShaderChainDescription { get; set; } = "None";
    }

    public class PerformanceProfileManager
    {
        private static readonly Lazy<PerformanceProfileManager> _instance = new(() => new PerformanceProfileManager());
        public static PerformanceProfileManager Instance => _instance.Value;

        private PerformanceTier _currentTier = PerformanceTier.Tier3_BalancedDefault;
        private PerformanceSettings _currentSettings;

        public event Action<PerformanceTier, PerformanceSettings>? ProfileChanged;

        public PerformanceTier CurrentTier
        {
            get => _currentTier;
            set
            {
                if (_currentTier != value)
                {
                    _currentTier = value;
                    _currentSettings = GenerateSettingsForTier(value);
                    ProfileChanged?.Invoke(_currentTier, _currentSettings);
                }
            }
        }

        public PerformanceSettings CurrentSettings => _currentSettings;

        public PerformanceProfileManager()
        {
            _currentSettings = GenerateSettingsForTier(_currentTier);
        }

        public PerformanceSettings GenerateSettingsForTier(PerformanceTier tier)
        {
            switch (tier)
            {
                case PerformanceTier.Tier1_EcoBatterySaver:
                    return new PerformanceSettings
                    {
                        Tier = PerformanceTier.Tier1_EcoBatterySaver,
                        DisplayName = "Tier 1: Eco / Battery Saver",
                        Subtitle = "Ultra-low power consumption for notebooks and on-battery playback",
                        ScalingAlgorithm = VideoScalingAlgorithm.FastBilinear,
                        HardwareDecodeCopyDisabled = true,
                        FrameDropThresholdFps = 30,
                        BackgroundThrottlingEnabled = true,
                        BackgroundTargetFps = 15,
                        MemoryCacheSizeMb = 32,
                        DebandMode = DebandFilterLevel.Off,
                        Deinterlacing = DeinterlaceEngine.None,
                        SmoothMotionInterpolation = false,
                        ToneMapping = HdrToneMappingCurve.None,
                        Precision16BitFloatProcessing = false,
                        Unlock8K60Pipeline = false,
                        ShaderChainsActive = false,
                        ActiveShaderChainDescription = "Zero-overhead bypass"
                    };

                case PerformanceTier.Tier2_StandardLowEnd:
                    return new PerformanceSettings
                    {
                        Tier = PerformanceTier.Tier2_StandardLowEnd,
                        DisplayName = "Tier 2: Standard / Low Performance PC",
                        Subtitle = "Smooth playback for entry-level GPUs and integrated graphics",
                        ScalingAlgorithm = VideoScalingAlgorithm.Spline36,
                        HardwareDecodeCopyDisabled = false,
                        FrameDropThresholdFps = 60,
                        BackgroundThrottlingEnabled = true,
                        BackgroundTargetFps = 30,
                        MemoryCacheSizeMb = 64,
                        DebandMode = DebandFilterLevel.Off,
                        Deinterlacing = DeinterlaceEngine.None,
                        SmoothMotionInterpolation = false,
                        ToneMapping = HdrToneMappingCurve.Reinhard,
                        Precision16BitFloatProcessing = false,
                        Unlock8K60Pipeline = false,
                        ShaderChainsActive = false,
                        ActiveShaderChainDescription = "DXVA2/D3D11 Native Surface Pipeline"
                    };

                case PerformanceTier.Tier3_BalancedDefault:
                default:
                    return new PerformanceSettings
                    {
                        Tier = PerformanceTier.Tier3_BalancedDefault,
                        DisplayName = "Tier 3: Balanced (Default)",
                        Subtitle = "Optimal visual fidelity and balanced thermals for everyday playback",
                        ScalingAlgorithm = VideoScalingAlgorithm.Lanczos3,
                        HardwareDecodeCopyDisabled = false,
                        FrameDropThresholdFps = 60,
                        BackgroundThrottlingEnabled = false,
                        BackgroundTargetFps = 60,
                        MemoryCacheSizeMb = 256,
                        DebandMode = DebandFilterLevel.BasicFast,
                        Deinterlacing = DeinterlaceEngine.YADIF_Auto,
                        SmoothMotionInterpolation = false,
                        ToneMapping = HdrToneMappingCurve.Mobius,
                        Precision16BitFloatProcessing = false,
                        Unlock8K60Pipeline = false,
                        ShaderChainsActive = false,
                        ActiveShaderChainDescription = "Lanczos3 + Basic Deband + Auto YADIF"
                    };

                case PerformanceTier.Tier4_HighPerformanceEnthusiast:
                    return new PerformanceSettings
                    {
                        Tier = PerformanceTier.Tier4_HighPerformanceEnthusiast,
                        DisplayName = "Tier 4: High Performance / Enthusiast",
                        Subtitle = "Cinematic post-processing, smooth motion, and BT.2390 HDR curve mapping",
                        ScalingAlgorithm = VideoScalingAlgorithm.EWA_LanczosSharp_Jinc,
                        HardwareDecodeCopyDisabled = false,
                        FrameDropThresholdFps = 144,
                        BackgroundThrottlingEnabled = false,
                        BackgroundTargetFps = 60,
                        MemoryCacheSizeMb = 512,
                        DebandMode = DebandFilterLevel.HighQualityMedium,
                        Deinterlacing = DeinterlaceEngine.BWDIF_DoubleRate,
                        SmoothMotionInterpolation = true,
                        ToneMapping = HdrToneMappingCurve.BT2390_EETF,
                        Precision16BitFloatProcessing = true,
                        Unlock8K60Pipeline = true,
                        ShaderChainsActive = true,
                        ActiveShaderChainDescription = "EWA-LanczosSharp + Jinc + Deband HQ + SmoothMotion"
                    };

                case PerformanceTier.Tier5_MasterExtremeGpu:
                    return new PerformanceSettings
                    {
                        Tier = PerformanceTier.Tier5_MasterExtremeGpu,
                        DisplayName = "Tier 5: Master / Extreme GPU",
                        Subtitle = "State-of-the-art neural upscaling (FSRCNNX 8-pass / Anime4K) & 16-bit HDR pipeline",
                        ScalingAlgorithm = VideoScalingAlgorithm.FSRCNNX_Neural_8Pass,
                        HardwareDecodeCopyDisabled = false,
                        FrameDropThresholdFps = 240,
                        BackgroundThrottlingEnabled = false,
                        BackgroundTargetFps = 120,
                        MemoryCacheSizeMb = 1024,
                        DebandMode = DebandFilterLevel.ExtremePrecision,
                        Deinterlacing = DeinterlaceEngine.BWDIF_DoubleRate,
                        SmoothMotionInterpolation = true,
                        ToneMapping = HdrToneMappingCurve.Hable,
                        Precision16BitFloatProcessing = true,
                        Unlock8K60Pipeline = true,
                        ShaderChainsActive = true,
                        ActiveShaderChainDescription = "FSRCNNX 8-Pass Tensor + Anime4K Hybrid + 16-bit Float Swapchain"
                    };
            }
        }
    }
}
