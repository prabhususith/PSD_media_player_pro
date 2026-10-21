# PSD Media Player Pro Edition

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)](LICENSE)
[![Version: v2.5.0 LTS](https://img.shields.io/badge/Release-v2.5.0%20LTS-brightgreen.svg?style=flat-square)](https://github.com/prabhususith/PSD_media_player_pro/releases/tag/v2.5.0)
[![Platform: Windows 10 / 11](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011%20(x64%20%2F%20ARM64)-informational.svg?style=flat-square)](docs/SYSTEM_REQUIREMENTS.md)
[![Android: 6.0 - 15+](https://img.shields.io/badge/Android-API%2023--35%20(Marshmallow%20to%2015)-green.svg?style=flat-square)](docs/SYSTEM_REQUIREMENTS.md)
[![Rendering: Direct3D 11VA](https://img.shields.io/badge/Rendering-Direct3D%2011VA-orange.svg?style=flat-square)](docs/SYSTEM_REQUIREMENTS.md)
[![Audio: NAudio 31--Band WASAPI](https://img.shields.io/badge/Audio-31--Band%20WASAPI%20DSP-purple.svg?style=flat-square)](docs/USER_GUIDE.md)
[![Engine: LibVLC 3.0.24 LTS](https://img.shields.io/badge/Engine-LibVLC%203.0.24%20LTS-success.svg?style=flat-square)](https://www.videolan.org/)
[![Themes: 110 Skins](https://img.shields.io/badge/Themes-110%20Built--in%20Packs-teal.svg?style=flat-square)](docs/USER_GUIDE.md)

PSD Media Player Pro Edition is an enterprise-grade desktop multimedia workstation engineered for ultra-high-fidelity video and audio playback on Microsoft Windows 10 and Windows 11 (x64 and ARM64). Built on a dual-core architecture combining LibVLC 3.0.24 LTS with hardware Direct3D 11 Video Acceleration (D3D11VA) and NAudio 31-Band WASAPI parametric digital signal processing (DSP), the player integrates 1,000+ curated live IPTV broadcast channels, Dolby Atmos 3D spatial simulation, 110 dynamic hot-reloading themes, live telecast recording, and an online video downloader.

---

## Table of Contents

- [Architectural Overview and Capabilities](#architectural-overview-and-capabilities)
- [Official Release Deliverables and Download Links](#official-release-deliverables-and-download-links)
- [Cryptographic Hash Verification](#cryptographic-hash-verification)
- [System Requirements Specification](#system-requirements-specification)
- [Installation and Deployment Guide](#installation-and-deployment-guide)
- [Enterprise Active Directory and GPO Silent Rollout](#enterprise-active-directory-and-gpo-silent-rollout)
- [User Manual and Feature Tour](#user-manual-and-feature-tour)
- [Master Keyboard Shortcuts Reference](#master-keyboard-shortcuts-reference)
- [Building from Source](#building-from-source)
- [Terms of Use, Privacy Policy, and User Consent](#terms-of-use-privacy-policy-and-user-consent)
- [Third-Party Software and Open Source Disclosures](#third-party-software-and-open-source-disclosures)
- [Author, Maintainer, and Support Contact](#author-maintainer-and-support-contact)
- [License](#license)

---

## Architectural Overview and Capabilities

```
PSD Media Player Architecture
+-----------------------------------------------------------------------------------+
|                                 Presentation Layer                                 |
|   MainWindow (WPF MVVM)  |  Custom Control Cards  |  110+ Theme ResourceDictionaries  |
|   Basic Mode             |  Normal Mode           |  Advanced Mode  |  Modern Mode|
+-----------------------------------------+-----------------------------------------+
                                          |
+-----------------------------------------v-----------------------------------------+
|                                Core Engine Layer                                  |
|   +------------------------------------+   +-----------------------------------+  |
|   |       Video Decoding Pipeline      |   |       Audio DSP Matrix Pipeline   |  |
|   |  - Direct3D 11VA Swapchain Binding |   |  - 31-Band ISO 1/3-Octave EQ      |  |
|   |  - Multi-Threaded Dav1d AV1 Decoder|   |  - Dolby Atmos 3D Elevation Cross |  |
|   |  - 5-Stage Performance Profiles    |   |  - DTS:X Speech Intelligibility   |  |
|   |  - BT.2390 EETF HDR10 Tonemapping  |   |  - 5.1 / 7.1 Virtual Surround     |  |
|   |  - YADIF Adaptive Deinterlacing    |   |  - Bauer Binaural HRTF Crossfeed  |  |
|   +------------------------------------+   +-----------------------------------+  |
+-----------------------------------------+-----------------------------------------+
                                          |
+-----------------------------------------v-----------------------------------------+
|                            Network and Subsystem Layer                            |
|   Global IPTV Hub (1000+ Channels)  |  Live TS Recorder  |  Media Downloader     |
|   LibVLC 3.0.24 C++ Core Interop    |  WASAPI Exclusive  |  Direct3D 11 Runtime   |
+-----------------------------------------------------------------------------------+
```

### 1. Direct3D 11 Zero-Copy Video Pipeline
- Hardware-accelerated decoding through Direct3D 11 Video Acceleration (D3D11VA), NVDEC, Intel QuickSync, and DXVA2.
- Full 8K 120fps and 4K 240fps throughput with multi-threaded Dav1d AVX-512/AVX2 software fallback for high-bit-depth master streams.
- 5-Stage Performance Matrix (Eco, Standard PC, Balanced, High-Performance, Neural FSRCNNX Master).
- HDR10, HDR10+, and Dolby Vision playback with BT.2390 EETF tone-mapping and 16-bit scRGB float swapchains.

### 2. Next-Generation Audio DSP Matrix
- 31-band ISO standard 1/3-octave equalizer (20 Hz to 20 kHz) with 31 genre-engineered presets.
- Dolby Atmos 3D Simulation: Haas delay interaural crossfeed (18ms) and high-shelf elevation filtering (+3dB above 7 kHz).
- DTS:X Spatializer: Speech clarity band enhancement (+4dB between 1.5 kHz and 3.5 kHz) and wide stereo decorrelation.
- 5.1 and 7.1 Virtual Surround: Multi-channel phase synthesis with surround delay (25ms) and center vocal extraction.
- Headphone HRTF: Bauer stereophonic-to-binaural crossfeed modeling interaural level and phase timing.
- Schroeder reverberation acoustic chambers (Living Room, Studio Monitor, Concert Hall, Sacred Cathedral, Cavern Echo).
- SOLA time-stretching pitch preserver (-12 to +12 semitones) and center-channel Karaoke vocal suppressor.

### 3. Global IPTV and Broadcast Engine
- Curated directory of 1,000+ verified free-to-air broadcast channels.
- Comprehensive Indian regional broadcast coverage: Hindi National, Tamil, Telugu, Malayalam, Kannada, Bengali, Marathi, Punjabi, and Gujarati.
- Worldwide News, Sports, Cinema, Family/Kids, Science & Technology, and Documentary feeds.
- Dynamic M3U playlist synchronization against upstream GitHub repositories and local `.m3u`/`.m3u8` playlist import.
- Integrated HTTP reconnect and 3,000ms network buffer caching to eliminate packet-drop stutters.

### 4. Media Tooling Subsystems
- Live Telecast Recorder: Background stream capture saving `.ts` containers to `%USERPROFILE%\Videos\PSD_Recordings`.
- Online Media Downloader: Multi-format stream downloading supporting YouTube, Twitter/X, Instagram, TikTok, and direct HLS.
- Capture Studio: Single-frame lossless PNG/BMP screenshot grabber, continuous burst mode, and animated GIF generator.
- 110 Hot-Reloading Skins: Classic Player Archetypes (25), Modern Flat (35), Studio Glass (25), and Gamer High-Contrast (25).

---

## Official Release Deliverables and Download Links

All official release binaries are compiled with Release optimizations, code-signed, cryptographically hashed, and distributed through the GitHub Release portal:

| Distribution Package | Architecture | Format | File Size | Description and Recommended Use | Direct Download Link |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Windows Setup Installer** | x64 | `.exe` | 414.28 MB | Standard desktop setup with wizard, file associations, desktop shortcuts, and uninstaller. | [Download PSDPlayer-Pro-Setup-v2.5.0-x64.exe](https://github.com/prabhususith/PSD_media_player_pro/releases/download/v2.5.0/PSDPlayer-Pro-Setup-v2.5.0-x64.exe) |
| **Enterprise MSI Installer** | x64 | `.msi` | 198.86 MB | WiX Toolset v4 enterprise package for Active Directory GPO and Microsoft Intune silent rollout. | [Download PSDPlayer-Enterprise-x64.msi](https://github.com/prabhususith/PSD_media_player_pro/releases/download/v2.5.0/PSDPlayer-Enterprise-x64.msi) |
| **Standalone Executable** | x64 | `.exe` | 205.43 MB | Direct single-file executable. Runs without installation or background service prerequisites. | [Download PSDPlayer.exe](https://github.com/prabhususith/PSD_media_player_pro/releases/download/v2.5.0/PSDPlayer.exe) |
| **Portable Archive (x64)** | x64 | `.zip` | 447.55 MB | Complete self-contained portable distribution. Extract and run from local drives or USB media. | [Download PSDPlayer-Pro-v2.5.0-Portable-win-x64.zip](https://github.com/prabhususith/PSD_media_player_pro/releases/download/v2.5.0/PSDPlayer-Pro-v2.5.0-Portable-win-x64.zip) |
| **Portable Archive (ARM64)**| ARM64 | `.zip` | 435.06 MB | Native ARM64 portable package compiled for Snapdragon X Elite, X Plus, and Surface Pro ARM64 devices. | [Download PSDPlayer-Pro-v2.5.0-Portable-win-arm64.zip](https://github.com/prabhususith/PSD_media_player_pro/releases/download/v2.5.0/PSDPlayer-Pro-v2.5.0-Portable-win-arm64.zip) |
| **Android Universal Package** | Universal (ARM64 / ARMv7) | `.apk` | 8.21 MB | Native Android edition for Android 5.0+ (API 21-35), signed with dual Scheme v1 + v2, raster PNG icons, Java 8 desugaring, fully validated on Xiaomi Redmi Note 4 and modern Android devices. | [Download PSDPlayer-Pro-v2.5.0-Release-universal.apk](https://github.com/prabhususith/PSD_media_player_pro/releases/download/v2.5.0/PSDPlayer-Pro-v2.5.0-Release-universal.apk) |
| **Android Redmi Note 4 Edition** | Universal (ARM64 / ARMv7) | `.apk` | 8.21 MB | Dedicated package verified with Scheme v1 JAR signing, legacy MultiDex AOT compilation, raster PNG icons, and API 21+ base for Xiaomi Redmi Note 4 (MIUI 8/9/10). | [Download PSDPlayer-Pro-v2.5.0-RedmiNote4-Ready.apk](https://github.com/prabhususith/PSD_media_player_pro/releases/download/v2.5.0/PSDPlayer-Pro-v2.5.0-RedmiNote4-Ready.apk) |
| **Android Redmi Note 4 (Debug Mirror)** | Universal (ARM64 / ARMv7) | `.apk` | 10.89 MB | Direct debug variant signed with standard Android debug key, identical to reference GoogleNotes build profile for maximum compatibility on MIUI. | [Download PSDPlayer-Pro-v2.5.0-RedmiNote4-Debug.apk](https://github.com/prabhususith/PSD_media_player_pro/releases/download/v2.5.0/PSDPlayer-Pro-v2.5.0-RedmiNote4-Debug.apk) |

*Browse all release notes, release tags, and historical artifacts on the [Official GitHub Releases Portal](https://github.com/prabhususith/PSD_media_player_pro/releases/tag/v2.5.0).*

---

## Cryptographic Hash Verification

To verify file integrity and confirm your downloaded deliverable has not been modified or corrupted, calculate the SHA-256 hash using Windows PowerShell or the Windows Command Prompt:

### PowerShell Verification Command:
```powershell
Get-FileHash -Path ".\PSDPlayer-Pro-Setup-v2.5.0-x64.exe" -Algorithm SHA256 | Format-List
```

### Command Prompt Verification Command:
```cmd
certutil -hashfile PSDPlayer-Pro-Setup-v2.5.0-x64.exe SHA256
```

### Official SHA-256 Checksum Matrix:

| File Name | Exact Size (Bytes) | Cryptographic SHA-256 Hash |
| :--- | :--- | :--- |
| `PSDPlayer-Pro-Setup-v2.5.0-x64.exe` | 434,406,766 | `E44DEF4635BC14C6E5C73748014E17B1171B4783E6DB2DECE292D23CC4B8D78C` |
| `PSDPlayer-Enterprise-x64.msi` | 208,515,072 | `BEA5B598502AB1CACA81727A500E370634B98A918E15C17FC775C6560E1C8591` |
| `PSDPlayer.exe` | 215,412,010 | `801EACA7A8F500F5A36A3DA1D10D0354E22465C12403F17B1433BD199A5D5A5B` |
| `PSDPlayer-Pro-v2.5.0-Portable-win-x64.zip` | 469,287,881 | `E55BC744737FDDBBAED205AF522C941FB217B5B63AB9A255D979C82D00B87BC1` |
| `PSDPlayer-Pro-v2.5.0-Portable-win-arm64.zip` | 456,191,583 | `77CC0749E0E48D7A142406AC9E2ACBCB8EB408C77DA338A54583ED601F0CEF95` |
| `PSDPlayer-Pro-v2.5.0-Release-universal.apk` | 8,607,905 | `701C85018AE4EB7C82109189D5F52BBBB5FCEE3733FCC2D0685AF047EF97A44A` |
| `PSDPlayer-Pro-v2.5.0-RedmiNote4-Ready.apk` | 8,607,905 | `701C85018AE4EB7C82109189D5F52BBBB5FCEE3733FCC2D0685AF047EF97A44A` |
| `PSDPlayer-Pro-v2.5.0-RedmiNote4-Debug.apk` | 11,420,215 | `8CDB211C47EE991BAABA2676271768A9EF5D46074A94C3283285B5B5426781CB` |

---

## System Requirements Specification

Detailed requirements and codec specifications are maintained in [docs/SYSTEM_REQUIREMENTS.md](docs/SYSTEM_REQUIREMENTS.md).

| Specification Level | Processor (CPU) | Memory (RAM) | Graphics (GPU) | Operating System |
| :--- | :--- | :--- | :--- | :--- |
| **Minimum** (720p / 1080p SDR) | Intel Core i3 4th Gen / AMD FX-6300 (Dual-Core 2.0 GHz) | 2 GB Physical RAM | Intel HD 4400 / NVIDIA GT 730 / AMD R7 240 (Direct3D 11) | Windows 10 x64 / ARM64 (Build 1809+) |
| **Recommended** (1080p / 4K 60fps HDR) | Intel Core i5 8th Gen / AMD Ryzen 5 2600 (Quad-Core 3.2 GHz) | 4 GB to 8 GB RAM | Intel UHD 630 / NVIDIA GTX 1050 / AMD RX 560 (2 GB VRAM) | Windows 11 x64 / ARM64 (22H2+) |
| **Ultra / Master** (8K 60fps / 120fps HDR10+) | Intel Core i9 12th+ Gen / AMD Ryzen 9 5900X+ (8+ Cores) | 16 GB DDR4/DDR5 | NVIDIA RTX 3070+ / AMD RX 6800+ / Intel Arc A770 (8+ GB VRAM)| Windows 11 x64 (23H2 / 24H2) |

---

## Installation and Deployment Guide

### 1. Standard Windows Installer (`PSDPlayer-Pro-Setup-v2.5.0-x64.exe`)
1. Download `PSDPlayer-Pro-Setup-v2.5.0-x64.exe` from the official download table.
2. Launch the installer executable. If prompted by Windows User Account Control (UAC), click **Yes**.
3. Select installation directory (default: `C:\Program Files\PSD Media Player\`).
4. Select optional file associations for video (MKV, MP4, AVI, WebM, TS) and audio (FLAC, MP3, WAV, AAC, OGG).
5. Click **Install**. The setup wizard deploys the executable, theme libraries, and LibVLC native plugins.

### 2. Zero-Install Portable Edition (`PSDPlayer-Pro-v2.5.0-Portable-win-x64.zip`)
1. Download `PSDPlayer-Pro-v2.5.0-Portable-win-x64.zip`.
2. Extract the archive to any storage directory (for example, `D:\Tools\PSDPlayer` or an external USB drive).
3. Execute `PSDPlayer.exe`.
4. The portable edition operates self-contained without writing registry entries or requiring administrative credentials.

### 3. Android Mobile Installation (`PSDPlayer-Pro-v2.5.0-Release-universal.apk` / `PSDPlayer-Pro-v2.5.0-RedmiNote4-Ready.apk`)
1. Download `PSDPlayer-Pro-v2.5.0-Release-universal.apk` (or `PSDPlayer-Pro-v2.5.0-RedmiNote4-Ready.apk`) directly to your Android device (Android 6.0 Marshmallow through Android 15/16+).
2. Tap the APK file in your device Downloads or File Manager.
3. When prompted, permit installation from unknown sources or your browser/file manager.
4. Tap **Install**. Both packages are verified with dual APK Signature Scheme v1 (JAR signing) and Scheme v2, guaranteeing zero parse errors on legacy package managers (Xiaomi MIUI 8/9/10, Snapdragon 625, and older Android 6.0 runtimes) as well as modern Android 14/15 devices.
5. Launch **PSD Media Player Pro** from your application launcher.
6. For automated deployment and debugging on a connected device via ADB:
   ```powershell
   powershell -ExecutionPolicy Bypass -File android\scripts\install-to-device.ps1
   ```

---

## Enterprise Active Directory and GPO Silent Rollout

For system administrators deploying across enterprise workstation fleets, the MSI package supports silent execution:

### Silent Installation via Command Line:
```cmd
msiexec.exe /i PSDPlayer-Enterprise-x64.msi /qn /norestart ALLUSERS=1
```

### Silent Installation with Detailed Logging:
```cmd
msiexec.exe /i PSDPlayer-Enterprise-x64.msi /qn /l*v "C:\Logs\PSDPlayer-Install.log" ALLUSERS=1
```

### Silent Uninstallation:
```cmd
msiexec.exe /x PSDPlayer-Enterprise-x64.msi /qn /norestart
```

---

## User Manual and Feature Tour

Comprehensive user instructions and workflow guides are maintained in [docs/USER_GUIDE.md](docs/USER_GUIDE.md).

1. **View Modes**: Toggle between Basic (`Ctrl+1`), Normal (`Ctrl+2`), Advanced (`Ctrl+3`), and Modern (`Ctrl+4`) interfaces.
2. **Global IPTV Hub (`Ctrl+L` / `F7`)**: Filter channels by category pills or search bar. Click **Play** on any channel card for instantaneous playback. Use **Refresh from Network** to pull latest playlists.
3. **Sound Technologies (`F3` / `Ctrl+E`)**: Select spatial presets (Dolby Atmos 3D, DTS:X, 5.1/7.1 Surround, Headphone HRTF), choose Schroeder reverberation echo presets, or adjust the 31-band ISO equalizer.
4. **Live Stream Recording (`R` / `Ctrl+R`)**: Start instant recording of live telecasts to `%USERPROFILE%\Videos\PSD_Recordings`.
5. **Video Downloader (`Ctrl+D` / `F8`)**: Paste URLs from YouTube, Twitter/X, Instagram, or direct video streams for local offline saving.
6. **Capture Studio (`Ctrl+G` / `F9`)**: Capture lossless snapshots, burst sequences, and animated GIFs.

---

## Master Keyboard Shortcuts Reference

| Command / Operation | Key Binding | Alternate Binding | Description |
| :--- | :--- | :--- | :--- |
| **Play / Pause** | `Space` | `K` | Toggles media playback state. |
| **Stop** | `S` | `Ctrl+.` | Halts playback and resets timeline position. |
| **Seek Backward 5s** | `Left Arrow` | — | Jumps backward by 5 seconds. |
| **Seek Forward 5s** | `Right Arrow` | — | Jumps forward by 5 seconds. |
| **Seek Backward 10s** | `J` | — | YouTube-standard seek backward by 10 seconds. |
| **Seek Forward 10s** | `L` | — | YouTube-standard seek forward by 10 seconds. |
| **Seek Backward 60s** | `Ctrl+Left Arrow` | — | Jumps backward by 1 minute. |
| **Seek Forward 60s** | `Ctrl+Right Arrow` | — | Jumps forward by 1 minute. |
| **Frame Advance** | `.` (Period) | — | Advances video by one single frame while paused. |
| **Volume Up (+5%)** | `Up Arrow` | `Mouse Wheel Up` | Increments audio volume level. |
| **Volume Down (-5%)** | `Down Arrow` | `Mouse Wheel Down`| Decrements audio volume level. |
| **Mute / Unmute** | `M` | — | Mutes or restores audio channel output. |
| **Toggle Fullscreen** | `F` | `F11` / `Double Click`| Enters or exits borderless fullscreen. |
| **Exit Fullscreen** | `Escape` | — | Restores normal windowed mode. |
| **Toggle Controls OSD**| `H` | `Ctrl+H` | Toggles visibility of transport controls and menus. |
| **Pin / Unpin Controls**| `P` | `Ctrl+P` | Locks fullscreen controls visible permanently. |
| **Cycle Aspect Ratio** | `A` | — | Cycles Auto, 16:9, 4:3, 16:10, 21:9, 1:1, Fill. |
| **Audio Enhancement** | `F3` | `Ctrl+E` | Opens 31-Band Equalizer & Spatial Audio dialog. |
| **Performance Matrix** | `F4` | `Ctrl+P` | Opens Hardware Decoding and Shaders dialog. |
| **Live TV & IPTV Hub** | `Ctrl+L` | `F7` | Launches Global IPTV live broadcast catalog. |
| **Media Downloader** | `Ctrl+D` | `F8` | Opens Online Media & Video Downloader. |
| **Capture Studio** | `Ctrl+G` | `F9` | Opens Screenshot Grabber and GIF Studio. |
| **Instant Snapshot** | `Shift+S` | `Ctrl+S` | Lossless frame grab to `%USERPROFILE%\Pictures\PSD_Snapshots`. |
| **Record Live Stream** | `R` | `Ctrl+R` | Toggles live transport stream recording to disk. |
| **Media Information** | `Ctrl+I` | — | Displays technical codec metrics and dropped frames. |
| **Interactive Help** | `F1` | — | Opens interactive Help and User Guide. |
| **Open File** | `Ctrl+O` | — | Displays native Windows file browser. |
| **Open Stream URL** | `Ctrl+U` | — | Opens network stream URL dialogue. |
| **A-B Repeat Loop** | `Ctrl+R` (Menu) | — | Sets Loop Marker [A], Marker [B], or clears loop. |
| **Reset Speed (1.0x)** | `=` | `Backspace` | Resets playback speed to 1.0x. |
| **Decrease Speed** | `[` | — | Decreases playback speed by 0.1x increments. |
| **Increase Speed** | `]` | — | Increases playback speed by 0.1x increments. |
| **Subtitle Sync Delay**| `Z` | — | Retards subtitle display timing by 50ms. |
| **Subtitle Sync Lead** | `X` | — | Advances subtitle display timing by 50ms. |
| **Cycle Subtitle Track**| `V` | — | Cycles available embedded and external subtitle tracks. |
| **Cycle Audio Track** | `B` | — | Cycles multi-language audio dubs and streams. |

---

## Building from Source

### Prerequisites
- Microsoft Windows 10 (Build 19041+) or Windows 11.
- Microsoft .NET 8.0 SDK (version 8.0.100 or higher).
- Inno Setup 6 (optional, required for `.exe` installer compilation).
- WiX Toolset v4 (optional, required for `.msi` enterprise compilation).

### Compilation Steps:
```powershell
# 1. Clone the repository
git clone https://github.com/prabhususith/PSD_media_player_pro.git
cd PSD_media_player_pro

# 2. Compile all 110 theme pack manifests
powershell -ExecutionPolicy Bypass -File scripts\export-themes.ps1

# 3. Build the solution in Release configuration
dotnet build OmniPlayer.sln -c Release

# 4. Execute the automated multi-target packaging pipeline
powershell -ExecutionPolicy Bypass -File scripts\build-all.ps1 -Configuration Release
```

All generated deliverables will be populated in the `dist/` directory.

---

## Terms of Use, Privacy Policy, and User Consent

Complete terms, legal disclosures, and privacy agreements are maintained in [docs/TERMS_AND_PRIVACY.md](docs/TERMS_AND_PRIVACY.md).

- **Data Sovereignty and Local Storage**: Zero analytics, zero telemetric identifiers, and zero user-tracking services are contained in this software. Playback history and configuration profiles remain exclusively within `%LOCALAPPDATA%\OmniPlayer`.
- **Network Transparency**: Streaming requests and online media downloads connect directly from the user's client machine to the host content delivery network. No intermediary proxies or telemetry hubs are utilized.
- **Fair Use and Acceptable Use Policy**: Users are responsible for ensuring that all streamed, recorded, and downloaded media content complies with regional intellectual property laws and provider agreements.

---

## Third-Party Software and Open Source Disclosures

PSD Media Player Pro Edition acknowledges and utilizes the following open-source projects:

- **VideoLAN LibVLC (v3.0.24)**: GNU Lesser General Public License v2.1. Dynamic linking to unmodified binary distribution.
- **LibVLCSharp**: LGPL v2.1 / MS-PL .NET binding for LibVLC.
- **NAudio (v2.2.1)**: Microsoft Public License (Ms-PL). Core WASAPI interop and digital audio buffering.
- **WiX Toolset (v4.0.5)**: .NET Foundation Open Source License.
- **Inno Setup (v6.3.3)**: Inno Setup License (Jordan Russell).

---

## Author, Maintainer, and Support Contact

- **Software Architect & Author**: Prabhu Kannan
- **Organization**: PSD Technologies
- **Official Technical Support Email**: prabhukannanmail@gmail.com
- **Official Source Repository**: https://github.com/prabhususith/PSD_media_player_pro
- **Issue Tracking & Feature Requests**: https://github.com/prabhususith/PSD_media_player_pro/issues

---

## License

This project is licensed under the terms of the **MIT License**.  
See the complete license text in the [LICENSE](LICENSE) file.

Copyright (c) 2026 Prabhu Kannan (PSD Technologies). All Rights Reserved.
