# Changelog — PSD Media Player Pro Edition

All notable changes to the PSD Media Player Pro Edition project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Author: Prabhu Kannan (PSD Technologies)  
Support Contact: prabhukannanmail@gmail.com  

---

## [2.5.0] - 2026-10-21 (Long-Term Support Release)

### Added
- **Startup Diagnostics and Self-Healing Asset Resolver**: Implemented `InitializeWindowIconSafely()` runtime fallback loader in `MainWindow.xaml.cs` to prevent BAML TypeConverter exceptions when resolving window icons from disk, application root, or assembly pack URIs.
- **PSD Media Player Pro (Android Edition)**: Released native Android edition (`com.psdtechnologies.psdplayer`) supporting Android 6.0 Marshmallow (API 23) through Android 15/16+ (API 35), benchmarked on Qualcomm Snapdragon 625 (Xiaomi Redmi Note 4). Features hardware MediaCodec acceleration, SurfaceView zero-copy rendering, multi-touch gestures (brightness, volume, seek, pinch zoom), 10/31-band Equalizer, background audio playback with foreground notification, dual-mode floating PiP (Android 8.0+ native and Android 6.0/7.0 system overlay), and 110+ ported theme skins.
- **Official Brand Vector and Icon Integration**: Added high-resolution `psd-player-logo.svg`, 256x256 `psd-player-logo.png`, and multi-resolution `psd-player-icon.ico` directly embedded as assembly resources and packaged with portable binaries.
- **Enterprise Silent Installer Configurations**: Enhanced Inno Setup (`omni-player.iss`) and WiX Toolset (`Package.wxs`) with automated Assets directory deployment, application association, and per-machine GPO silent rollout capabilities.
- **Process Lock Safeguard**: Integrated automatic process termination logic into `build-all.ps1` to prevent unauthorized access exceptions during packaging of active executables.

### Changed
- Rebranded official application title, metadata, company, and copyright headers to **PSD Media Player Pro Edition**, authored by **Prabhu Kannan** under **PSD Technologies**.
- Standardized release artifact nomenclature across standalone executables (`PSDPlayer.exe`), setup installers (`PSDPlayer-Pro-Setup-v2.5.0-x64.exe`), and enterprise MSI packages (`PSDPlayer-Enterprise-x64.msi`).

### Fixed
- Fixed `XamlParseException` on application startup caused by static icon attribute resolution prior to assembly resource registration.
- Resolved file lock contention during multi-target packaging in PowerShell archive generation routines.

---

## [2.2.0] - 2026-10-19

### Added
- **Fullscreen 3-Second Controls Auto-Hide**: Implemented high-precision global cursor tracking and 3.0-second inactivity auto-hide timer for transport controls, timeline, and mouse cursor in borderless fullscreen mode.
- **Controls Pin Toggle**: Added dedicated pin toggle buttons (`BtnPinControls` and `BtnModernPinControls`) on the transport bar allowing users to permanently anchor controls during broadcast monitoring.
- **Edge Reveal Interaction**: Cursor movement to the bottom screen boundary (`Y >= ScreenHeight - 110px`) dynamically reveals playback controls; movement to top boundary (`Y <= 45px`) reveals top menu bar.
- **Interactive Help and Documentation Guide**: Added `HelpGuideDialog` with an 8-topic searchable navigation system covering View Modes, IPTV Hub, Audio DSP, YouTube Seeking, Live Recording, Downloader, Shortcuts, and Architecture.
- **Modern Responsive ComboBox Controls**: Designed custom XAML ComboBox template featuring rounded toggle cards, dynamic vector chevron indicators, deep drop-shadow popups, and theme-responsive hover styling.

### Changed
- Updated menu text foreground bindings (`TextElement.Foreground="{DynamicResource OmniForegroundBrush}"`) across top-level menus, submenus, context menus, and keyboard shortcut labels for automatic contrast adjustment across light and dark skins.

### Fixed
- Fixed tab selection bug in `HelpGuideDialog.xaml.cs` where dynamic button tag reassignment caused integer parsing failures on consecutive tab clicks.
- Corrected taskbar clipping behavior in fullscreen mode utilizing Win32 `SetWindowPos` API interop.

---

## [2.1.0] - 2026-10-18

### Added
- **Global IPTV and Live Television Hub**: Added catalog of over 1,000 curated, verified free-to-air channels with dedicated regional categories for Indian National, Tamil, Telugu, Malayalam, Kannada, Bengali, Marathi, Punjabi, Gujarati, World News, Sports, Cinema, and Documentaries.
- **Dynamic M3U / M3U8 Playlist Ingestion**: Added runtime synchronization against upstream IPTV-org GitHub feeds and local `.m3u` file import capabilities.
- **Live Stream Transport Stream Recorder**: Implemented background chunked streaming recorder writing `.ts` containers asynchronously to `%USERPROFILE%\Videos\PSD_Recordings`.
- **Online Media and Video Downloader**: Added multi-format downloader utility supporting YouTube, Twitter/X, Instagram, and direct HTTP/HTTPS media streams.

### Changed
- Configured `VLC_PLUGIN_PATH` environment variable dynamically during engine startup to guarantee discovery of all 433 native LibVLC access and demuxer plugins in single-file portable execution.
- Sanitized LibVLC media initialization options (`:network-caching=3000`, `:live-caching=2000`, `:http-reconnect`) to eliminate option tokenizer buffer overruns.

### Fixed
- Fixed network stream dropouts and black screens on HLS `.m3u8` streams by enforcing modern TLS 1.2 and TLS 1.3 security protocols in `App.xaml.cs`.

---

## [2.0.0] - 2026-10-15

### Added
- **110 Hot-Reloading Theme Packs**: Introduced a comprehensive library of 110 dynamic skins spanning Classic Player Archetypes, Modern Flat & Fluent, Studio Glass & Acrylic, and Gamer High-Contrast designs.
- **Next-Generation Audio DSP Matrix**: Added Dolby Atmos 3D Simulation, DTS:X dialogue spatializer, 5.1 and 7.1 virtual surround upmixing, and Bauer stereophonic-to-binaural HRTF headphone filters.
- **Parametric 31-Band ISO Equalizer**: Added 1/3-octave equalizer spanning 20Hz through 20kHz with 31 genre-tuned factory presets and Night Mode Dynamic Range Compression.
- **5-Stage Video Performance Profile Engine**: Added hardware optimization profiles from Eco / Battery Saver (Tier 1) through Neural FSRCNNX Master (Tier 5).
- **Four Interface View Modes**: Added runtime switching between Basic, Normal, Advanced, and Modern player layouts.

### Changed
- Migrated primary rendering pipeline to LibVLCSharp WPF with direct swapchain binding.

---

## [1.5.0] - 2026-09-20

### Added
- Dual simultaneous subtitle renderer supporting primary bottom dialogue and secondary top translations.
- Frame-by-frame stepping controller (`.` hotkey).
- YouTube-style timeline seeking with keyboard mappings (`J`, `K`, `L`, arrow keys, numeric percentages).
- Variable playback rate stretching from 0.25x to 4.0x with SOLA pitch preservation.

---

## [1.0.0] - 2026-08-01

### Added
- Initial core architecture release with LibVLC multi-format media engine.
- Direct3D 11 Video Acceleration (D3D11VA) hardware decoding pipeline.
- Matroska, MP4, WebM, AVI, and TS container demuxing.
- Basic playback transport bar, audio volume controls, and file association manifests.
