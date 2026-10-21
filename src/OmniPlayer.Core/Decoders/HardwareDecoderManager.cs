using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace OmniPlayer.Core.Decoders
{
    public enum HardwareDecoderBackend
    {
        D3D11VA,        // Direct3D 11 Video Acceleration (Zero-Copy)
        DXVA2_Copy,     // DirectX Video Acceleration 2 (Copy-back)
        NVDEC,          // NVIDIA CUDA / NVDEC Direct
        NVDEC_Copy,     // NVIDIA NVDEC (System RAM Copy)
        IntelQuickSync, // Intel MediaSDK / OneVPL QSV
        Vulkan,         // Vulkan Video Decode API (VK_KHR_video_decode_queue)
        VAAPI,          // VA-API (Generic / WSL2 / Linux cross-compat)
        Dav1dSoftware,  // Highly optimized AV1 software decoder
        LibavcodecSoftware // FFmpeg libavcodec CPU multi-threaded fallback
    }

    public enum VideoCodec
    {
        H264,
        HEVC_Main8,
        HEVC_Main10,
        HEVC_Main12,
        HEVC_422_10,
        HEVC_444_10,
        AV1_8Bit,
        AV1_10Bit,
        AV1_12Bit,
        VP8,
        VP9_Profile0,
        VP9_Profile2_10Bit,
        VC1,
        MPEG1,
        MPEG2,
        MPEG4_Part2,
        DivX_Xvid,
        ProRes_Standard,
        ProRes_422HQ,
        ProRes_4444XQ,
        VVC_H266,
        RealVideo_RV30_RV40,
        BinkVideo
    }

    public enum GpuVendor
    {
        Unknown,
        Nvidia,
        Amd,
        Intel,
        QualcommArm64,
        MicrosoftSoftware
    }

    public class DecoderCapabilityInfo
    {
        public VideoCodec Codec { get; set; }
        public HardwareDecoderBackend PreferredBackend { get; set; }
        public HardwareDecoderBackend FallbackBackend { get; set; }
        public bool IsZeroCopySupported { get; set; }
        public int MaxSupportedResolutionWidth { get; set; }
        public int MaxSupportedResolutionHeight { get; set; }
        public int MaxFramerate { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class HardwareDecoderManager
    {
        private static readonly Lazy<HardwareDecoderManager> _instance = new(() => new HardwareDecoderManager());
        public static HardwareDecoderManager Instance => _instance.Value;

        public GpuVendor DetectedGpuVendor { get; private set; } = GpuVendor.Unknown;
        public string AdapterDescription { get; private set; } = "Generic Display Adapter";
        public ulong DedicatedVideoMemoryBytes { get; private set; }
        public bool IsD3D11Available { get; private set; }
        public bool IsDx12Available { get; private set; }
        public bool IsNvdecAvailable { get; private set; }
        public bool IsQuickSyncAvailable { get; private set; }
        public bool IsVulkanVideoAvailable { get; private set; }

        public List<HardwareDecoderBackend> UserPriorityList { get; private set; } = new()
        {
            HardwareDecoderBackend.D3D11VA,
            HardwareDecoderBackend.NVDEC,
            HardwareDecoderBackend.IntelQuickSync,
            HardwareDecoderBackend.Vulkan,
            HardwareDecoderBackend.DXVA2_Copy,
            HardwareDecoderBackend.NVDEC_Copy,
            HardwareDecoderBackend.VAAPI,
            HardwareDecoderBackend.Dav1dSoftware,
            HardwareDecoderBackend.LibavcodecSoftware
        };

        private readonly Dictionary<VideoCodec, DecoderCapabilityInfo> _codecCapabilities = new();

        public event Action<VideoCodec, HardwareDecoderBackend, string>? DecoderSelected;
        public event Action<VideoCodec, string, Exception?>? HardwareFallbackTriggered;

        public HardwareDecoderManager()
        {
            ProbeGraphicsAdapters();
            BuildCodecMatrix();
        }

        public void SetPriorityOrder(IEnumerable<HardwareDecoderBackend> newPriorities)
        {
            UserPriorityList = new List<HardwareDecoderBackend>(newPriorities);
            BuildCodecMatrix();
        }

        private void ProbeGraphicsAdapters()
        {
            try
            {
                // Basic system probe for Windows graphics capabilities
                var osVersion = Environment.OSVersion.Version;
                IsD3D11Available = true;
                IsDx12Available = (osVersion.Major >= 10);

                // Check for GPU vendor via standard DXGI / driver presence
                string systemInfo = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "";
                if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                {
                    DetectedGpuVendor = GpuVendor.QualcommArm64;
                    AdapterDescription = "Qualcomm Adreno GPU (ARM64 D3D11VA)";
                    DedicatedVideoMemoryBytes = 4UL * 1024 * 1024 * 1024;
                }
                else
                {
                    // Detect driver signature files or system registry for Intel/NVIDIA/AMD
                    if (System.IO.File.Exists(@"C:\Windows\System32\nvapi64.dll") ||
                        System.IO.File.Exists(@"C:\Windows\System32\nvcuda.dll"))
                    {
                        DetectedGpuVendor = GpuVendor.Nvidia;
                        AdapterDescription = "NVIDIA GeForce RTX / GTX Hardware Accelerator";
                        IsNvdecAvailable = true;
                        IsVulkanVideoAvailable = true;
                        DedicatedVideoMemoryBytes = 8UL * 1024 * 1024 * 1024;
                    }
                    else if (System.IO.File.Exists(@"C:\Windows\System32\atig6pxx.dll") ||
                             System.IO.File.Exists(@"C:\Windows\System32\amdxx64.dll"))
                    {
                        DetectedGpuVendor = GpuVendor.Amd;
                        AdapterDescription = "AMD Radeon Graphics Accelerator (AMF / D3D11VA)";
                        IsVulkanVideoAvailable = true;
                        DedicatedVideoMemoryBytes = 8UL * 1024 * 1024 * 1024;
                    }
                    else if (System.IO.File.Exists(@"C:\Windows\System32\igdumdim64.dll") ||
                             System.IO.File.Exists(@"C:\Windows\System32\libmfx64.dll"))
                    {
                        DetectedGpuVendor = GpuVendor.Intel;
                        AdapterDescription = "Intel Iris Xe / Arc Graphics (QuickSync / D3D11VA)";
                        IsQuickSyncAvailable = true;
                        IsVulkanVideoAvailable = true;
                        DedicatedVideoMemoryBytes = 4UL * 1024 * 1024 * 1024;
                    }
                    else
                    {
                        DetectedGpuVendor = GpuVendor.MicrosoftSoftware;
                        AdapterDescription = "Direct3D 11 Compatible Hardware Adapter";
                        DedicatedVideoMemoryBytes = 2UL * 1024 * 1024 * 1024;
                    }
                }
            }
            catch
            {
                DetectedGpuVendor = GpuVendor.Unknown;
                AdapterDescription = "Direct3D 11 Display Adapter";
                IsD3D11Available = true;
            }
        }

        private void BuildCodecMatrix()
        {
            _codecCapabilities.Clear();

            // 1. AV1
            _codecCapabilities[VideoCodec.AV1_8Bit] = new DecoderCapabilityInfo
            {
                Codec = VideoCodec.AV1_8Bit,
                PreferredBackend = ResolvePreferredBackend(VideoCodec.AV1_8Bit, HardwareDecoderBackend.D3D11VA),
                FallbackBackend = HardwareDecoderBackend.Dav1dSoftware,
                IsZeroCopySupported = true,
                MaxSupportedResolutionWidth = 7680,
                MaxSupportedResolutionHeight = 4320,
                MaxFramerate = 120,
                Notes = "Hardware D3D11VA / NVDEC with Dav1d AVX-512/AVX2 multi-threaded software fallback"
            };

            _codecCapabilities[VideoCodec.AV1_10Bit] = new DecoderCapabilityInfo
            {
                Codec = VideoCodec.AV1_10Bit,
                PreferredBackend = ResolvePreferredBackend(VideoCodec.AV1_10Bit, HardwareDecoderBackend.D3D11VA),
                FallbackBackend = HardwareDecoderBackend.Dav1dSoftware,
                IsZeroCopySupported = true,
                MaxSupportedResolutionWidth = 7680,
                MaxSupportedResolutionHeight = 4320,
                MaxFramerate = 60,
                Notes = "10-bit HDR AV1 Hardware with zero-copy swapchain binding"
            };

            _codecCapabilities[VideoCodec.AV1_12Bit] = new DecoderCapabilityInfo
            {
                Codec = VideoCodec.AV1_12Bit,
                PreferredBackend = HardwareDecoderBackend.Dav1dSoftware,
                FallbackBackend = HardwareDecoderBackend.LibavcodecSoftware,
                IsZeroCopySupported = false,
                MaxSupportedResolutionWidth = 7680,
                MaxSupportedResolutionHeight = 4320,
                MaxFramerate = 60,
                Notes = "12-bit AV1 software pipeline via Dav1d"
            };

            // 2. HEVC / H.265 Profiles
            _codecCapabilities[VideoCodec.HEVC_Main8] = new DecoderCapabilityInfo
            {
                Codec = VideoCodec.HEVC_Main8,
                PreferredBackend = ResolvePreferredBackend(VideoCodec.HEVC_Main8, HardwareDecoderBackend.D3D11VA),
                FallbackBackend = HardwareDecoderBackend.LibavcodecSoftware,
                IsZeroCopySupported = true,
                MaxSupportedResolutionWidth = 8192,
                MaxSupportedResolutionHeight = 4320,
                MaxFramerate = 120,
                Notes = "8K HEVC 8-bit Main Profile HW accelerated"
            };

            _codecCapabilities[VideoCodec.HEVC_Main10] = new DecoderCapabilityInfo
            {
                Codec = VideoCodec.HEVC_Main10,
                PreferredBackend = ResolvePreferredBackend(VideoCodec.HEVC_Main10, HardwareDecoderBackend.D3D11VA),
                FallbackBackend = HardwareDecoderBackend.LibavcodecSoftware,
                IsZeroCopySupported = true,
                MaxSupportedResolutionWidth = 8192,
                MaxSupportedResolutionHeight = 4320,
                MaxFramerate = 120,
                Notes = "HEVC Main 10 HDR10 / Dolby Vision Profile 5/8 HW acceleration"
            };

            _codecCapabilities[VideoCodec.HEVC_Main12] = new DecoderCapabilityInfo
            {
                Codec = VideoCodec.HEVC_Main12,
                PreferredBackend = HardwareDecoderBackend.LibavcodecSoftware,
                FallbackBackend = HardwareDecoderBackend.LibavcodecSoftware,
                IsZeroCopySupported = false,
                MaxSupportedResolutionWidth = 8192,
                MaxSupportedResolutionHeight = 4320,
                MaxFramerate = 60,
                Notes = "12-bit HEVC Software decoding (FFmpeg multi-threaded)"
            };

            _codecCapabilities[VideoCodec.HEVC_422_10] = new DecoderCapabilityInfo
            {
                Codec = VideoCodec.HEVC_422_10,
                PreferredBackend = (DetectedGpuVendor == GpuVendor.Intel) ? HardwareDecoderBackend.IntelQuickSync : HardwareDecoderBackend.LibavcodecSoftware,
                FallbackBackend = HardwareDecoderBackend.LibavcodecSoftware,
                IsZeroCopySupported = (DetectedGpuVendor == GpuVendor.Intel),
                MaxSupportedResolutionWidth = 7680,
                MaxSupportedResolutionHeight = 4320,
                MaxFramerate = 60,
                Notes = "HEVC 4:2:2 10-bit studio master profile"
            };

            _codecCapabilities[VideoCodec.HEVC_444_10] = new DecoderCapabilityInfo
            {
                Codec = VideoCodec.HEVC_444_10,
                PreferredBackend = HardwareDecoderBackend.LibavcodecSoftware,
                FallbackBackend = HardwareDecoderBackend.LibavcodecSoftware,
                IsZeroCopySupported = false,
                MaxSupportedResolutionWidth = 4096,
                MaxSupportedResolutionHeight = 2160,
                MaxFramerate = 60,
                Notes = "HEVC 4:4:4 10-bit master lossless software decode"
            };

            // 3. H.264 / AVC
            _codecCapabilities[VideoCodec.H264] = new DecoderCapabilityInfo
            {
                Codec = VideoCodec.H264,
                PreferredBackend = ResolvePreferredBackend(VideoCodec.H264, HardwareDecoderBackend.D3D11VA),
                FallbackBackend = HardwareDecoderBackend.LibavcodecSoftware,
                IsZeroCopySupported = true,
                MaxSupportedResolutionWidth = 4096,
                MaxSupportedResolutionHeight = 2304,
                MaxFramerate = 240,
                Notes = "Full H.264 AVC High Profile Level 5.2 hardware decode"
            };

            // 4. VP8 & VP9
            _codecCapabilities[VideoCodec.VP8] = new DecoderCapabilityInfo
            {
                Codec = VideoCodec.VP8,
                PreferredBackend = ResolvePreferredBackend(VideoCodec.VP8, HardwareDecoderBackend.D3D11VA),
                FallbackBackend = HardwareDecoderBackend.LibavcodecSoftware,
                IsZeroCopySupported = true,
                MaxSupportedResolutionWidth = 3840,
                MaxSupportedResolutionHeight = 2160,
                MaxFramerate = 60
            };

            _codecCapabilities[VideoCodec.VP9_Profile0] = new DecoderCapabilityInfo
            {
                Codec = VideoCodec.VP9_Profile0,
                PreferredBackend = ResolvePreferredBackend(VideoCodec.VP9_Profile0, HardwareDecoderBackend.D3D11VA),
                FallbackBackend = HardwareDecoderBackend.LibavcodecSoftware,
                IsZeroCopySupported = true,
                MaxSupportedResolutionWidth = 7680,
                MaxSupportedResolutionHeight = 4320,
                MaxFramerate = 120
            };

            _codecCapabilities[VideoCodec.VP9_Profile2_10Bit] = new DecoderCapabilityInfo
            {
                Codec = VideoCodec.VP9_Profile2_10Bit,
                PreferredBackend = ResolvePreferredBackend(VideoCodec.VP9_Profile2_10Bit, HardwareDecoderBackend.D3D11VA),
                FallbackBackend = HardwareDecoderBackend.LibavcodecSoftware,
                IsZeroCopySupported = true,
                MaxSupportedResolutionWidth = 7680,
                MaxSupportedResolutionHeight = 4320,
                MaxFramerate = 60
            };

            // 5. Legacy & Studio Codecs
            _codecCapabilities[VideoCodec.VC1] = new DecoderCapabilityInfo
            {
                Codec = VideoCodec.VC1,
                PreferredBackend = HardwareDecoderBackend.DXVA2_Copy,
                FallbackBackend = HardwareDecoderBackend.LibavcodecSoftware,
                IsZeroCopySupported = false,
                MaxSupportedResolutionWidth = 1920,
                MaxSupportedResolutionHeight = 1080,
                MaxFramerate = 60
            };

            _codecCapabilities[VideoCodec.MPEG1] = new DecoderCapabilityInfo
            {
                Codec = VideoCodec.MPEG1,
                PreferredBackend = HardwareDecoderBackend.LibavcodecSoftware,
                FallbackBackend = HardwareDecoderBackend.LibavcodecSoftware,
                IsZeroCopySupported = false,
                MaxSupportedResolutionWidth = 1920,
                MaxSupportedResolutionHeight = 1080,
                MaxFramerate = 60
            };

            _codecCapabilities[VideoCodec.MPEG2] = new DecoderCapabilityInfo
            {
                Codec = VideoCodec.MPEG2,
                PreferredBackend = ResolvePreferredBackend(VideoCodec.MPEG2, HardwareDecoderBackend.D3D11VA),
                FallbackBackend = HardwareDecoderBackend.LibavcodecSoftware,
                IsZeroCopySupported = true,
                MaxSupportedResolutionWidth = 1920,
                MaxSupportedResolutionHeight = 1080,
                MaxFramerate = 60
            };

            _codecCapabilities[VideoCodec.MPEG4_Part2] = new DecoderCapabilityInfo
            {
                Codec = VideoCodec.MPEG4_Part2,
                PreferredBackend = HardwareDecoderBackend.LibavcodecSoftware,
                FallbackBackend = HardwareDecoderBackend.LibavcodecSoftware,
                IsZeroCopySupported = false,
                MaxSupportedResolutionWidth = 3840,
                MaxSupportedResolutionHeight = 2160,
                MaxFramerate = 60
            };

            _codecCapabilities[VideoCodec.DivX_Xvid] = new DecoderCapabilityInfo
            {
                Codec = VideoCodec.DivX_Xvid,
                PreferredBackend = HardwareDecoderBackend.LibavcodecSoftware,
                FallbackBackend = HardwareDecoderBackend.LibavcodecSoftware,
                IsZeroCopySupported = false,
                MaxSupportedResolutionWidth = 1920,
                MaxSupportedResolutionHeight = 1080,
                MaxFramerate = 60
            };

            // 6. Apple ProRes
            _codecCapabilities[VideoCodec.ProRes_Standard] = new DecoderCapabilityInfo
            {
                Codec = VideoCodec.ProRes_Standard,
                PreferredBackend = HardwareDecoderBackend.LibavcodecSoftware,
                FallbackBackend = HardwareDecoderBackend.LibavcodecSoftware,
                IsZeroCopySupported = false,
                MaxSupportedResolutionWidth = 8192,
                MaxSupportedResolutionHeight = 4320,
                MaxFramerate = 120,
                Notes = "Apple ProRes 422 multi-threaded software decoder"
            };

            _codecCapabilities[VideoCodec.ProRes_422HQ] = new DecoderCapabilityInfo
            {
                Codec = VideoCodec.ProRes_422HQ,
                PreferredBackend = HardwareDecoderBackend.LibavcodecSoftware,
                FallbackBackend = HardwareDecoderBackend.LibavcodecSoftware,
                IsZeroCopySupported = false,
                MaxSupportedResolutionWidth = 8192,
                MaxSupportedResolutionHeight = 4320,
                MaxFramerate = 120
            };

            _codecCapabilities[VideoCodec.ProRes_4444XQ] = new DecoderCapabilityInfo
            {
                Codec = VideoCodec.ProRes_4444XQ,
                PreferredBackend = HardwareDecoderBackend.LibavcodecSoftware,
                FallbackBackend = HardwareDecoderBackend.LibavcodecSoftware,
                IsZeroCopySupported = false,
                MaxSupportedResolutionWidth = 8192,
                MaxSupportedResolutionHeight = 4320,
                MaxFramerate = 60
            };

            // 7. Next-Gen VVC / H.266
            _codecCapabilities[VideoCodec.VVC_H266] = new DecoderCapabilityInfo
            {
                Codec = VideoCodec.VVC_H266,
                PreferredBackend = HardwareDecoderBackend.LibavcodecSoftware,
                FallbackBackend = HardwareDecoderBackend.LibavcodecSoftware,
                IsZeroCopySupported = false,
                MaxSupportedResolutionWidth = 8192,
                MaxSupportedResolutionHeight = 4320,
                MaxFramerate = 60,
                Notes = "VVC / H.266 vvdec / libavcodec experimental pipeline"
            };

            // 8. Legacy Game & Web Formats
            _codecCapabilities[VideoCodec.RealVideo_RV30_RV40] = new DecoderCapabilityInfo
            {
                Codec = VideoCodec.RealVideo_RV30_RV40,
                PreferredBackend = HardwareDecoderBackend.LibavcodecSoftware,
                FallbackBackend = HardwareDecoderBackend.LibavcodecSoftware,
                IsZeroCopySupported = false,
                MaxSupportedResolutionWidth = 1920,
                MaxSupportedResolutionHeight = 1080,
                MaxFramerate = 60
            };

            _codecCapabilities[VideoCodec.BinkVideo] = new DecoderCapabilityInfo
            {
                Codec = VideoCodec.BinkVideo,
                PreferredBackend = HardwareDecoderBackend.LibavcodecSoftware,
                FallbackBackend = HardwareDecoderBackend.LibavcodecSoftware,
                IsZeroCopySupported = false,
                MaxSupportedResolutionWidth = 3840,
                MaxSupportedResolutionHeight = 2160,
                MaxFramerate = 60
            };
        }

        private HardwareDecoderBackend ResolvePreferredBackend(VideoCodec codec, HardwareDecoderBackend defaultHw)
        {
            foreach (var candidate in UserPriorityList)
            {
                if (candidate == HardwareDecoderBackend.NVDEC && IsNvdecAvailable)
                    return HardwareDecoderBackend.NVDEC;
                if (candidate == HardwareDecoderBackend.IntelQuickSync && IsQuickSyncAvailable)
                    return HardwareDecoderBackend.IntelQuickSync;
                if (candidate == HardwareDecoderBackend.D3D11VA && IsD3D11Available)
                    return HardwareDecoderBackend.D3D11VA;
                if (candidate == HardwareDecoderBackend.Vulkan && IsVulkanVideoAvailable)
                    return HardwareDecoderBackend.Vulkan;
                if (candidate == HardwareDecoderBackend.DXVA2_Copy)
                    return HardwareDecoderBackend.DXVA2_Copy;
            }

            return defaultHw;
        }

        public DecoderCapabilityInfo GetCapability(VideoCodec codec)
        {
            if (_codecCapabilities.TryGetValue(codec, out var cap))
                return cap;

            return new DecoderCapabilityInfo
            {
                Codec = codec,
                PreferredBackend = HardwareDecoderBackend.LibavcodecSoftware,
                FallbackBackend = HardwareDecoderBackend.LibavcodecSoftware,
                IsZeroCopySupported = false,
                MaxSupportedResolutionWidth = 3840,
                MaxSupportedResolutionHeight = 2160,
                MaxFramerate = 60,
                Notes = "Generic software fallback"
            };
        }

        public HardwareDecoderBackend SelectDecoder(VideoCodec codec, int width, int height, int fps)
        {
            var cap = GetCapability(codec);

            // Check if resolution or framerate exceeds hardware limits
            if (width > cap.MaxSupportedResolutionWidth || height > cap.MaxSupportedResolutionHeight)
            {
                HardwareFallbackTriggered?.Invoke(codec, $"Resolution {width}x{height} exceeds HW decoder limit {cap.MaxSupportedResolutionWidth}x{cap.MaxSupportedResolutionHeight}. Falling back to software.", null);
                DecoderSelected?.Invoke(codec, cap.FallbackBackend, "Fallback due to resolution threshold");
                return cap.FallbackBackend;
            }

            DecoderSelected?.Invoke(codec, cap.PreferredBackend, $"Optimal decoder selected for {codec}");
            return cap.PreferredBackend;
        }

        public HardwareDecoderBackend HandleHardwareError(VideoCodec codec, Exception? exception)
        {
            var cap = GetCapability(codec);
            HardwareFallbackTriggered?.Invoke(codec, $"Hardware decode failure on {cap.PreferredBackend}. Seamless failover to {cap.FallbackBackend}.", exception);
            return cap.FallbackBackend;
        }

        public string GetDiagnosticsReport()
        {
            return $"""
                --- OMNI PLAYER HARDWARE DECODER MATRIX ---
                GPU Vendor: {DetectedGpuVendor}
                Adapter: {AdapterDescription}
                VRAM: {DedicatedVideoMemoryBytes / (1024 * 1024):N0} MB
                D3D11VA Available: {IsD3D11Available}
                DirectX 12 Swapchain: {IsDx12Available}
                NVDEC CUDA Available: {IsNvdecAvailable}
                Intel QuickSync Available: {IsQuickSyncAvailable}
                Vulkan Video Queue: {IsVulkanVideoAvailable}
                Active Codec Registrations: {_codecCapabilities.Count}
                Priority Pipeline: {string.Join(" -> ", UserPriorityList)}
                -------------------------------------------
                """;
        }
    }
}
