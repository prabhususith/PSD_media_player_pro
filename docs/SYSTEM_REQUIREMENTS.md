# PSD Media Player Pro Edition — System Requirements Specification

Document Reference: PSD-ENG-SRS-2026-V25  
Author: Prabhu Kannan (PSD Technologies)  
Support Contact: prabhukannanmail@gmail.com  
Version: 2.5.0 LTS  
Target Operating Systems: Microsoft Windows 10, Windows 11, Windows Server 2022/2025 (x64 and ARM64)  

---

## 1. Scope and Objective

This specification defines the hardware, operating system, runtime dependencies, and display interface requirements for executing PSD Media Player Pro Edition (v2.5.0 LTS). The software is engineered for low-latency media playback utilizing Direct3D 11 Video Acceleration (D3D11VA), hardware-accelerated video decoding, Windows Audio Session API (WASAPI) exclusive mode bitstreaming, and a 31-band parametric digital signal processing (DSP) audio engine.

---

## 2. Hardware Compatibility Matrix

### 2.1 Standard Definition and High Definition Playback (720p / 1080p SDR)

| Component | Minimum Specification | Recommended Specification |
| :--- | :--- | :--- |
| Processor (x64) | Intel Core i3 4th Gen / AMD FX-6300 (Dual-Core, 2.0 GHz) | Intel Core i5 8th Gen / AMD Ryzen 5 2600 (Quad-Core, 3.2 GHz) |
| Processor (ARM64) | Qualcomm Snapdragon 8cx Gen 1 | Qualcomm Snapdragon X Elite / X Plus |
| System Memory | 2 GB Physical RAM | 4 GB Physical RAM (Dual-Channel) |
| Graphics Processing Unit | Intel HD Graphics 4400, NVIDIA GeForce GT 730, AMD Radeon R7 240 | Intel UHD Graphics 630, NVIDIA GeForce GTX 1050, AMD Radeon RX 560 |
| Direct3D Support | Direct3D Feature Level 11_0 | Direct3D Feature Level 11_1 or 12_0 |
| Video Memory (VRAM) | 512 MB Dedicated or Shared | 2 GB Dedicated VRAM |
| Storage | 600 MB free space (Single-file portable or standard installation) | 2 GB free SSD storage (for stream caching and live recording) |
| Audio Output | Standard AC97 / High Definition Audio 2.0 Stereo Interface | 24-bit 96 kHz High Definition Stereo / Headphone Interface |

### 2.2 Ultra High Definition and High Dynamic Range Playback (4K / 8K HDR10 / 120 FPS)

| Component | Minimum Specification for 4K 60fps HDR | High-End Specification for 8K 60fps / 120fps HDR10+ |
| :--- | :--- | :--- |
| Processor (x64) | Intel Core i7 10th Gen / AMD Ryzen 7 3700X (8 Cores) | Intel Core i9 13th/14th Gen / AMD Ryzen 9 7900X (12+ Cores) |
| System Memory | 8 GB DDR4 RAM | 16 GB DDR4/DDR5 RAM |
| Graphics Processing Unit | NVIDIA GeForce GTX 1660 / RTX 2060, AMD Radeon RX 5700, Intel Arc A380 | NVIDIA GeForce RTX 3080/4080/4090, AMD Radeon RX 7800 XT/7900 XTX |
| Hardware Decoders | Hardware HEVC Main10, VP9 Profile 2, AV1 10-bit Decode Engine | Dual Hardware AV1 10-bit/12-bit, HEVC 4:4:4 Decode Blocks |
| Video Memory (VRAM) | 4 GB Dedicated GDDR6 | 8 GB to 24 GB High-Speed GDDR6X |
| Storage Interface | SATA III Solid State Drive (Sequential Read >= 500 MB/s) | PCIe 4.0 NVMe SSD (Sequential Read >= 3,500 MB/s) |
| Display Output | HDMI 2.0b / DisplayPort 1.4 with HDCP 2.2 support | HDMI 2.1a (FRL 48 Gbps) / DisplayPort 2.1 (UHBR 13.5/20) |
| High Dynamic Range | Windows HDR Display Certification (VESA DisplayHDR 400) | VESA DisplayHDR 1000 / OLED with BT.2020 Color Gamut |
| Multi-Channel Audio | 5.1 / 7.1 Surround Sound Card or HDMI AVR Passthrough | Dolby Atmos / DTS:X certified HDMI eARC Audio Video Receiver |

---

## 3. Supported Operating Systems

| Operating System | Architecture | Servicing Channel | Validation Status |
| :--- | :--- | :--- | :--- |
| Microsoft Windows 11 | x64 | Version 21H2, 22H2, 23H2, 24H2 | Fully Supported (Recommended) |
| Microsoft Windows 11 | ARM64 | Version 22H2, 23H2, 24H2 | Fully Supported (Native ARM64) |
| Microsoft Windows 10 | x64 | Version 1809 (LTSC), 21H2, 22H2 | Fully Supported |
| Microsoft Windows 10 | ARM64 | Version 21H2, 22H2 | Fully Supported |
| Microsoft Windows Server | x64 | Server 2019, Server 2022, Server 2025 | Supported (Desktop Experience required) |

*Note: Windows 7, Windows 8, and Windows 8.1 are unsupported due to the lack of modern Direct3D 11.1 swapchain and TLS 1.3 protocol APIs.*

---

## 4. Software and Runtime Dependencies

The standalone portable distributions and Inno Setup installers include self-contained dependencies. When compiling from source or deploying the bare assembly, the following runtimes must be present:

1. **Microsoft .NET Desktop Runtime**:
   - Version: .NET 8.0.0 or higher (.NET 8 LTS recommended).
   - Component: `Microsoft.WindowsDesktop.App` (WPF and Windows Base runtime).
2. **Microsoft Visual C++ Redistributable**:
   - Visual C++ 2015-2022 Redistributable (x64 or ARM64, version 14.38.33135 or higher).
   - Required for native LibVLC 3.0.24 libraries and Direct3D interop modules.
3. **DirectX Runtime**:
   - Direct3D 11.1 Runtime (shipped natively with Windows 10 and Windows 11).
4. **Networking Protocols for IPTV & Streaming**:
   - Transport Layer Security (TLS) 1.2 and TLS 1.3 enabled.
   - Outbound HTTP/HTTPS access on ports 80 and 443 for streaming playlists, HLS chunks, and subtitles.
   - User Datagram Protocol (UDP) and Real-Time Transport Protocol (RTP) support for RTSP and raw multicast IPTV feeds.

---

## 5. Media Codec and Container Compatibility

### 5.1 Video Codecs
- **AV1 (AOMedia Video 1)**: Hardware decode via D3D11VA/NVDEC up to 8K 120fps; high-performance multi-threaded Dav1d AVX-512/AVX2 software fallback.
- **HEVC / H.265**: Main, Main 10, Main 12, Main 4:2:2 10-bit, Main 4:4:4 16-bit, HDR10, HDR10+, Dolby Vision (Profiles 5, 8.1).
- **AVC / H.264**: Baseline, Main, High, High 10, High 4:2:2, High 4:4:4 Predictive up to 4K 240fps.
- **VP9 & VP8**: Profile 0 (8-bit) and Profile 2 (10-bit HDR) hardware decoding.
- **MPEG-4 Part 2**: Simple, Advanced Simple Profile (DivX, Xvid).
- **Legacy Standards**: MPEG-1, MPEG-2 (DVD, DVB-T/C/S transport streams), VC-1, WMV3, Apple ProRes (422, HQ, 4444 XQ), VVC / H.266 (software), Theora, RealVideo.

### 5.2 Audio Codecs
- **Dolby Formats**: Dolby Digital (AC-3), Dolby Digital Plus (E-AC-3), Dolby TrueHD, Dolby Atmos (bitstream and decoded spatial simulation).
- **DTS Formats**: DTS Digital Surround, DTS-HD Master Audio, DTS-HD High Resolution, DTS:X.
- **Lossless Formats**: FLAC (up to 32-bit 384 kHz), ALAC (Apple Lossless), Monkey's Audio (APE), WavPack, True Audio (TTA), PCM (WAV, AIFF).
- **Lossy Formats**: AAC (LC, HE-AAC v1, HE-AAC v2), MP3, Opus, Ogg Vorbis, Windows Media Audio (WMA 9, WMA Pro, WMA Lossless), ACELP.

### 5.3 Multimedia Container Formats
- Matroska (MKV, MKA), MP4, M4V, MOV, WebM, AVI, MPEG-TS (.ts, .m2ts, .mts), MPEG-PS (.mpg, .mpeg, .vob), Flash Video (.flv), Windows Media (.wmv, .asf), Ogg (.ogg, .ogv), 3GP.
- **Live Stream Manifests**: HLS (.m3u8), Dynamic Adaptive Streaming over HTTP (DASH .mpd), M3U playlists, RTSP, RTMP.

---

## 6. Audio Interface and Digital Signal Processing (DSP) Requirements

- **Sampling Rates Supported**: 44.1 kHz, 48 kHz, 88.2 kHz, 96 kHz, 176.4 kHz, 192 kHz, 352.8 kHz, 384 kHz.
- **Bit Depths Supported**: 16-bit integer, 24-bit integer, 32-bit IEEE float.
- **Output Protocols**:
  - DirectSound / WASAPI Shared Mode: Audio mixing through Windows Audio Endpoint.
  - WASAPI Exclusive Mode: Direct bit-perfect rendering to external DAC or AV Receiver bypassing Windows mixer.
- **Spatial Audio DSP**:
  - Virtual 5.1 / 7.1 Matrix Upmixer.
  - Bauer Stereophonic-to-Binaural HRTF crossfeed filter.
  - Haas-effect 3D interaural delay spatializer.
  - Schroeder 4-comb 2-allpass reverberation chambers.

---

## 7. Android Mobile Platform Compatibility (Xiaomi Redmi Note 4 Reference Profile)

### 7.1 System Specifications
- **Operating System Range**: Android 5.0 Lollipop (API Level 21) through Android 15 / 16+ (API Level 35).
- **Reference Benchmark Device**: Xiaomi Redmi Note 4.
  - System-on-Chip (SoC): Qualcomm MSM8953 Snapdragon 625 (Octa-Core 2.0 GHz Cortex-A53).
  - Graphics Processing Unit (GPU): Qualcomm Adreno 506.
  - System Memory: 3 GB / 4 GB LPDDR3 RAM.
  - ROM Environment: MIUI 8 / 9 / 10 Global / China Stable (Android 5.0 / 6.0 / 7.0 base).
- **Target Memory Footprint**: Under 85 MB RAM during 1080p 60fps hardware-accelerated playback.

### 7.2 APK Packaging and Signature Standards
- **Baseline SDK**: `minSdkVersion = 21` (API 21: Android 5.0 Lollipop) matching the working reference `GoogleNotesPsd` application profile.
- **Installation Location**: `android:installLocation="auto"` declared in manifest allowing MIUI package installer to allocate storage without constraint errors.
- **Cryptographic Signature**: Dual APK Signature Scheme v1 (JAR signing) and Scheme v2 (APK Signature Scheme v2) signed via standard Android debug key matching reference installation trust.
- **MultiDex Engine**: Integrated `androidx.multidex:multidex:2.0.1` via `PSDPlayerApplication` with `MultiDex.install(this)` to prevent package installer hanging during ahead-of-time (AOT) DEX extraction on MIUI 8/9/10.
- **Hardware Acceleration**: Direct zero-copy `SurfaceView` video rendering backed by Android `MediaCodec` with graceful multi-threaded CPU decoder fallback (Dav1d, FFmpeg).
- **Application Icon Format**: Strict raster PNG bitmap assets deployed across all density buckets (`mipmap-mdpi`, `mipmap-hdpi`, `mipmap-xhdpi`, `mipmap-xxhdpi`, `mipmap-xxxhdpi`) preventing Android `PackageParser` crashes caused by XML vector or adaptive icons.
- **Toolchain & Bytecode Target**: Standardized on Java 17 toolchain (`compileOptions` & `kotlinOptions`) with AndroidX modern libraries and clean deprecation handling.
- **Universal Release Deliverable**: `PSDPlayer-Pro-v2.5.0-Release-universal.apk` (Size: 8,393,430 bytes, SHA-256: `1BC1D2943E22DC29AB71B2DA5443ED6095194D641A2EBC787EBFA775F03F215D`).
- **Dedicated Redmi Note 4 Deliverable**: `PSDPlayer-Pro-v2.5.0-RedmiNote4-Ready.apk` (Size: 8,393,430 bytes, SHA-256: `1BC1D2943E22DC29AB71B2DA5443ED6095194D641A2EBC787EBFA775F03F215D`).
- **Redmi Note 4 Debug Mirror Deliverable**: `PSDPlayer-Pro-v2.5.0-RedmiNote4-Debug.apk` (Size: 10,351,376 bytes, SHA-256: `3D1D906E466636F1A210B4EB5F26C8C9D57A69936A2C6C7550E4835FC267246D`).
