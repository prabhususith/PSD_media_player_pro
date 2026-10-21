using System;
using System.Collections.Generic;

namespace OmniPlayer.Core.Video
{
    public class VideoFilterSettings
    {
        // Real-time picture adjustments (-100 to +100, default 0; Gamma: 0.1 to 3.0, default 1.0)
        public float Brightness { get; set; } = 0.0f;    // -100 to +100
        public float Contrast { get; set; } = 0.0f;      // -100 to +100
        public float Saturation { get; set; } = 0.0f;    // -100 to +100
        public float Hue { get; set; } = 0.0f;           // -180 to +180 deg
        public float Gamma { get; set; } = 1.0f;         // 0.2 to 3.0 (1.0 = linear)
        public float Sharpness { get; set; } = 0.0f;     // 0 to 100

        // Advanced Enhancement Filters
        public bool DenoiseEnabled { get; set; } = false;
        public float DenoiseStrength { get; set; } = 25.0f; // 0 to 100

        public bool DeringEnabled { get; set; } = false;
        public float DeringStrength { get; set; } = 30.0f;

        public bool DeblockEnabled { get; set; } = false;
        public float DeblockStrength { get; set; } = 50.0f;

        public bool MotionInterpolationEnabled { get; set; } = false;
        public int TargetFramerateFps { get; set; } = 60; // 60, 120, 144, 240

        public bool DebandEnabled { get; set; } = false;
        public int DebandIterations { get; set; } = 1;
        public float DebandThreshold { get; set; } = 48.0f;

        // HDR & Tone-mapping
        public bool HdrPassthroughEnabled { get; set; } = false;
        public string TonemapAlgorithm { get; set; } = "BT.2390"; // BT.2390, Hable, Mobius, Reinhard
        public float TargetDisplayPeakNits { get; set; } = 1000.0f;
        public float SourceContentPeakNits { get; set; } = 4000.0f;

        // Active Neural & Custom Shaders
        public bool FsrcnnxActive { get; set; } = false;
        public bool Anime4kActive { get; set; } = false;
    }

    public class VideoFilterPipeline
    {
        private static readonly Lazy<VideoFilterPipeline> _instance = new(() => new VideoFilterPipeline());
        public static VideoFilterPipeline Instance => _instance.Value;

        public VideoFilterSettings Settings { get; } = new();

        public event Action<VideoFilterSettings>? FilterSettingsChanged;

        public void NotifySettingsChanged()
        {
            FilterSettingsChanged?.Invoke(Settings);
        }

        public void ResetToDefaults()
        {
            Settings.Brightness = 0.0f;
            Settings.Contrast = 0.0f;
            Settings.Saturation = 0.0f;
            Settings.Hue = 0.0f;
            Settings.Gamma = 1.0f;
            Settings.Sharpness = 0.0f;
            Settings.DenoiseEnabled = false;
            Settings.DeringEnabled = false;
            Settings.DeblockEnabled = false;
            Settings.MotionInterpolationEnabled = false;
            Settings.DebandEnabled = false;
            Settings.HdrPassthroughEnabled = false;
            Settings.TonemapAlgorithm = "BT.2390";
            Settings.FsrcnnxActive = false;
            Settings.Anime4kActive = false;

            NotifySettingsChanged();
        }

        public (float cR, float cG, float cB) ApplyColorGrading(float r, float g, float b)
        {
            // 1. Contrast & Brightness
            float contrastFactor = (100.0f + Settings.Contrast) / 100.0f;
            contrastFactor *= contrastFactor;
            float brightnessOffset = Settings.Brightness / 100.0f;

            r = (r - 0.5f) * contrastFactor + 0.5f + brightnessOffset;
            g = (g - 0.5f) * contrastFactor + 0.5f + brightnessOffset;
            b = (b - 0.5f) * contrastFactor + 0.5f + brightnessOffset;

            // 2. Gamma
            if (Math.Abs(Settings.Gamma - 1.0f) > 0.01f && Settings.Gamma > 0.05f)
            {
                float invGamma = 1.0f / Settings.Gamma;
                r = (r > 0) ? (float)Math.Pow(r, invGamma) : 0;
                g = (g > 0) ? (float)Math.Pow(g, invGamma) : 0;
                b = (b > 0) ? (float)Math.Pow(b, invGamma) : 0;
            }

            // 3. Saturation
            float lum = 0.2126f * r + 0.7152f * g + 0.0722f * b;
            float satFactor = (100.0f + Settings.Saturation) / 100.0f;
            r = lum + (r - lum) * satFactor;
            g = lum + (g - lum) * satFactor;
            b = lum + (b - lum) * satFactor;

            // 4. Hue rotation
            if (Math.Abs(Settings.Hue) > 0.1f)
            {
                float rad = Settings.Hue * (float)Math.PI / 180.0f;
                float cosA = (float)Math.Cos(rad);
                float sinA = (float)Math.Sin(rad);

                float rotR = (0.299f + 0.701f * cosA + 0.168f * sinA) * r
                           + (0.587f - 0.587f * cosA + 0.330f * sinA) * g
                           + (0.114f - 0.114f * cosA - 0.497f * sinA) * b;

                float rotG = (0.299f - 0.299f * cosA - 0.328f * sinA) * r
                           + (0.587f + 0.413f * cosA + 0.035f * sinA) * g
                           + (0.114f - 0.114f * cosA + 0.288f * sinA) * b;

                float rotB = (0.299f - 0.300f * cosA + 1.250f * sinA) * r
                           + (0.587f - 0.588f * cosA - 1.050f * sinA) * g
                           + (0.114f + 0.886f * cosA - 0.203f * sinA) * b;

                r = rotR; g = rotG; b = rotB;
            }

            return (Math.Clamp(r, 0f, 1f), Math.Clamp(g, 0f, 1f), Math.Clamp(b, 0f, 1f));
        }

        public float TonemapHable(float x)
        {
            // Uncharted 2 filmic curve
            const float A = 0.15f;
            const float B = 0.50f;
            const float C = 0.10f;
            const float D = 0.20f;
            const float E = 0.02f;
            const float F = 0.30f;
            return ((x * (A * x + C * B) + D * E) / (x * (A * x + B) + D * F)) - E / F;
        }

        public string GenerateHlslShaderCode()
        {
            return $$"""
            // Omni Player Real-Time Direct3D 11 / DX12 HLSL Post-Processing Pipeline
            cbuffer FilterBuffer : register(b0)
            {
                float Brightness;
                float Contrast;
                float Saturation;
                float Hue;
                float Gamma;
                float Sharpness;
                float DenoiseStrength;
                float DebandThreshold;
                float TargetPeakNits;
                float SourcePeakNits;
                uint  DenoiseActive;
                uint  DebandActive;
                uint  MotionInterpActive;
                uint  FsrcnnxActive;
            };

            Texture2D SourceTexture : register(t0);
            SamplerState LinearSampler : register(s0);

            float4 PS_OmniMasterFilter(float4 pos : SV_POSITION, float2 uv : TEXCOORD0) : SV_TARGET
            {
                float4 color = SourceTexture.Sample(LinearSampler, uv);

                // Contrast & Brightness
                float cFactor = pow((100.0 + Contrast) / 100.0, 2.0);
                color.rgb = (color.rgb - 0.5) * cFactor + 0.5 + (Brightness / 100.0);

                // Gamma correction
                if (abs(Gamma - 1.0) > 0.01 && Gamma > 0.05)
                {
                    color.rgb = pow(max(color.rgb, 0.0), 1.0 / Gamma);
                }

                // Saturation
                float luma = dot(color.rgb, float3(0.2126, 0.7152, 0.0722));
                float sFactor = (100.0 + Saturation) / 100.0;
                color.rgb = luma + (color.rgb - luma) * sFactor;

                // Sharpness (Unsharp Masking Convolution)
                if (Sharpness > 1.0)
                {
                    float2 texel = 1.0 / float2(3840.0, 2160.0);
                    float4 n = SourceTexture.Sample(LinearSampler, uv + float2(0, -texel.y));
                    float4 s = SourceTexture.Sample(LinearSampler, uv + float2(0, texel.y));
                    float4 w = SourceTexture.Sample(LinearSampler, uv + float2(-texel.x, 0));
                    float4 e = SourceTexture.Sample(LinearSampler, uv + float2(texel.x, 0));
                    float4 blur = (n + s + w + e) * 0.25;
                    color += (color - blur) * (Sharpness * 0.02);
                }

                return saturate(color);
            }
            """;
        }
    }
}
