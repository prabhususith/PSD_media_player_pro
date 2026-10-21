# PSD Media Player Pro Edition — Comprehensive User Guide and Operator Manual

Document Reference: PSD-DOC-UG-2026-V25  
Author: Prabhu Kannan (PSD Technologies)  
Support Contact: prabhukannanmail@gmail.com  
Version: 2.5.0 LTS  
Target Environment: Microsoft Windows 10 and Windows 11 (x64 / ARM64)  

---

## 1. Introduction and Architectural Overview

PSD Media Player Pro Edition is an enterprise-grade multimedia workstation engineered for ultra-high-fidelity video and audio playback. Powered by a dual-engine core combining LibVLC 3.0.24 LTS with Direct3D 11 Video Acceleration (D3D11VA) and NAudio 31-Band WASAPI parametric digital signal processing (DSP), the player delivers pristine color reproduction, studio-grade spatial audio, zero-latency seeking, and native support for modern network streaming protocols.

---

## 2. Interface View Modes

PSD Media Player features four distinct operational view modes tailored for different usage scenarios. View modes can be selected at runtime via **Tools -> View Modes** or using direct keyboard shortcuts:

| View Mode | Interface Structure | Recommended Usage | Shortcut |
| :--- | :--- | :--- | :--- |
| **Basic View** | Ultra-minimalist interface with timeline and essential transport controls only. | Background audio listening, secondary monitor playback, or low-distraction desktop environments. | `Ctrl+1` |
| **Normal View** | Standard balanced interface with full menu bar, scrub bar, transport controls, and status strip. | General everyday multimedia playback and video viewing. | `Ctrl+2` |
| **Advanced View** | Comprehensive monitoring layout exposing audio bitstream indicators, dropped frame statistics, and codec metrics. | Video mastering, quality assurance, diagnostic inspection, and broadcast stream monitoring. | `Ctrl+3` |
| **Modern View** | Floating glass aesthetics with rounded acrylic control cards, dynamic glowing timeline, and responsive iconography. | High-resolution modern 4K displays and touch-enabled convertible workstations. | `Ctrl+4` |

---

## 3. Playback Controls and Seeking Engine

The playback controller features YouTube-style precise seeking and timeline scrubbing:

1. **Precision Timeline Seeking**:
   - Left-click anywhere along the timeline scrub bar for instantaneous seek response.
   - Dragging the scrub bar thumb displays real-time frame previews and updates playback timing without audio stutters.
2. **Keyboard Seeking Controls**:
   - `Left Arrow` / `Right Arrow`: Fast seek backward / forward by 5 seconds.
   - `J` / `L`: YouTube-standard seek backward / forward by 10 seconds.
   - `Ctrl+Left Arrow` / `Ctrl+Right Arrow`: Long-range seek backward / forward by 60 seconds.
   - `0` through `9` Numeric Keys: Jump immediately to 0%, 10%, 20%, ..., 90% of total video duration.
3. **Variable Playback Speed**:
   - `[`: Decrease playback speed by 0.1x increments (down to 0.25x slow motion).
   - `]`: Increase playback speed by 0.1x increments (up to 4.0x high-speed review).
   - `=`: Reset playback speed instantly to 1.0x standard speed.
   - `.` (Period): Advance video frame-by-frame (single frame step).
4. **Volume and Audio Mute**:
   - `Up Arrow` / `Down Arrow`: Increment / decrement audio volume by 5% increments.
   - `Mouse Wheel`: Adjust volume continuously while hovering anywhere over the video surface.
   - `M`: Toggle audio mute state instantly with on-screen display (OSD) feedback.

---

## 4. Master Keyboard Shortcuts and Gesture Reference

The table below provides a complete reference for all keyboard shortcuts and input gestures supported across PSD Media Player:

| Action / Command | Primary Shortcut | Secondary Shortcut | Functional Description |
| :--- | :--- | :--- | :--- |
| **Play / Pause** | `Space` | `K` | Toggles media playback state. |
| **Stop Playback** | `S` | `Ctrl+.` | Halts playback and resets timeline position to 00:00:00. |
| **Seek Backward 5s** | `Left Arrow` | — | Seeks playback backward by 5 seconds. |
| **Seek Forward 5s** | `Right Arrow` | — | Seeks playback forward by 5 seconds. |
| **Seek Backward 10s** | `J` | — | Seeks playback backward by 10 seconds. |
| **Seek Forward 10s** | `L` | — | Seeks playback forward by 10 seconds. |
| **Seek Backward 60s** | `Ctrl+Left Arrow` | — | Jumps playback backward by 1 minute. |
| **Seek Forward 60s** | `Ctrl+Right Arrow` | — | Jumps playback forward by 1 minute. |
| **Frame-by-Frame Step** | `.` (Period) | — | Advances video by one single frame while paused. |
| **Volume Up (+5%)** | `Up Arrow` | `Mouse Wheel Up` | Increases audio volume level. |
| **Volume Down (-5%)** | `Down Arrow` | `Mouse Wheel Down`| Decreases audio volume level. |
| **Toggle Mute** | `M` | — | Mutes or restores audio channel output. |
| **Toggle Fullscreen** | `F` | `F11` / `Double-Click` | Enters or exits hardware-accelerated borderless fullscreen. |
| **Exit Fullscreen** | `Escape` | — | Restores windowed mode from fullscreen presentation. |
| **Toggle Controls Visibility** | `H` | `Ctrl+H` | Manually toggles visibility of player menus and timeline. |
| **Pin / Unpin Controls** | `P` | `Ctrl+P` | Locks fullscreen controls visible permanently or auto-hides. |
| **Cycle Aspect Ratio** | `A` | — | Toggles between Auto, 16:9, 4:3, 16:10, 21:9, 1:1, and Fill. |
| **Audio Enhancement Hub** | `F3` | `Ctrl+E` | Opens 31-band Equalizer and Spatial Audio DSP dialog. |
| **Performance Matrix Hub** | `F4` | `Ctrl+P` | Opens Video Performance, Shaders and Hardware Profile dialog. |
| **Live TV & IPTV Hub** | `Ctrl+L` | `F7` | Launches Global IPTV catalog with 1,000+ live stream channels. |
| **Media Downloader** | `Ctrl+D` | `F8` | Opens Online Media & Video Downloader utility. |
| **Capture Studio** | `Ctrl+G` | `F9` | Opens Screenshot Grabber, Burst Mode & Animated GIF studio. |
| **Take Instant Snapshot** | `S` (with Shift) | `Ctrl+S` | Saves current frame to `%USERPROFILE%\Pictures\PSD_Snapshots`. |
| **Record Live Telecast** | `R` | `Ctrl+R` | Toggles live transport stream (.ts) recording to disk. |
| **Media Information** | `Ctrl+I` | — | Opens stream technical telemetry and codec inspector. |
| **Interactive Help Guide** | `F1` | — | Opens built-in Master User Guide and Documentation. |
| **Open Local File** | `Ctrl+O` | — | Displays Windows native file browser for media selection. |
| **Open Network URL / Stream**| `Ctrl+U` | — | Displays network stream URL dialog (HLS, RTSP, YouTube). |
| **A-B Repeat Loop** | `Ctrl+R` (Submenu) | — | Sets Loop Marker [A], Marker [B], or clears loop range. |
| **Reset Speed to 1.0x** | `=` | `Backspace` | Resets playback speed to standard 1.0x. |
| **Decrease Speed (-0.1x)** | `[` | — | Decreases playback speed. |
| **Increase Speed (+0.1x)** | `]` | — | Increases playback speed. |
| **Subtitle Sync Delay (-50ms)**| `Z` | — | Retards subtitle display timing by 50 milliseconds. |
| **Subtitle Sync Advance (+50ms)**| `X` | — | Advances subtitle display timing by 50 milliseconds. |
| **Cycle Subtitle Track** | `V` | — | Cycles through available embedded and external subtitle streams.|
| **Cycle Audio Track** | `B` | — | Cycles through multi-language audio streams and dubs. |
| **Jump to Percentage** | `0` to `9` | — | Jumps to 0% (start), 10%, 20%, ..., 90% of media file. |

---

## 5. Fullscreen Mode and Auto-Hide Navigation

In fullscreen presentation mode:
1. **True Borderless Presentation**: Windows taskbar and operating system frame decorations are suppressed using native Win32 `SetWindowPos` interop.
2. **Auto-Hide Idle Behavior**: When the user ceases mouse movement for 3.0 seconds, both the playback controls and the mouse cursor are hidden automatically to ensure zero visual obstruction.
3. **Cursor Edge Reveal**: Moving the mouse cursor to the bottom boundary of the screen (`Y >= ScreenHeight - 110px`) immediately reveals playback transport controls. Moving cursor to the top boundary (`Y <= 45px`) reveals the application menu bar.
4. **Pin Controls Button**: A pin toggle button (`BtnPinControls` / `BtnModernPinControls`) on the controls bar locks the interface visible permanently for monitoring operations.
5. **Native Context Menu**: Right-clicking anywhere on the media surface displays the full native Windows context menu containing all playback commands, tools, themes, and audio track selections.

---

## 6. Global IPTV and Live Television Hub

The IPTV Hub (`Ctrl+L` or **Tools -> Live TV & IPTV Hub**) provides direct access to over 1,000 curated, legally verified free-to-air broadcast television feeds:

1. **Regional Categorization**:
   - **Indian National**: Aaj Tak, India TV, NDTV 24x7, ABP News, Republic World, WION, DD News, Sansad TV, CNBC Awaaz, Good News Today, 9X Jalwa, 9XM.
   - **Regional Indian Feeds**: Dedicated categories for Tamil (Puthiya Thalaimurai, Polimer News, Thanthi TV), Telugu (TV9 Telugu, NTV, Sakshi TV), Malayalam (Asianet News, Manorama News, 24 News), Kannada (Public TV, TV9 Kannada, Suvarna News), Bengali (ABP Ananda, 24 Ghanta), Marathi (ABP Majha, Zee 24 Taas), Punjabi and Gujarati.
   - **International Feeds**: Bloomberg Global, Al Jazeera English, DW English, France 24 English, Sky News, Reuters, ABC News Live, CBS News, Euronews, CNA, NHK World.
   - **Thematic Categories**: Sports & Action, Movies & Cinema, Kids & Family, Late Night & Entertainment, Music, Science & Tech, and Documentaries.
2. **Dynamic Playlist Ingestion**:
   - Select **Refresh from Network** to dynamically synchronize the latest IPTV-org GitHub playlist feeds.
   - Select **Import M3U File** to load local `.m3u` or `.m3u8` playlist files from storage.
   - Select **Add Stream / M3U URL** to add custom live streaming manifests.
3. **Network Caching and Buffer Protection**:
   - Streams are processed with 3,000 millisecond network caching and automatic HTTP reconnect logic to eliminate buffering stutters on fluctuating network connections.

---

## 7. Audio DSP Engine and Sound Technologies

Accessible via **Tools -> Audio Enhancement Hub (F3)**:

1. **Next-Generation Sound Technology Modes**:
   - **Standard Stereo**: Clean, uncolored bit-perfect stereo reproduction.
   - **Dolby Atmos 3D Simulation**: Haas-effect interaural time delay (18ms crossfeed) and high-shelf elevation filtering (+3dB above 7 kHz) simulating upper vertical soundstage dimensions.
   - **DTS:X Spatializer**: Speech clarity extraction with dialogue band enhancement (+4dB between 1.5 kHz and 3.5 kHz) and wide stereo decorrelation.
   - **5.1 Virtual Surround Upmix**: Phase-matrix multi-channel synthesis with rear surround delay (25ms) and center extraction.
   - **7.1 Virtual Surround Upmix**: Expansive 8-channel virtual acoustic soundfield.
   - **Headphone 3D HRTF**: Bauer stereophonic-to-binaural crossfeed modeling interaural level and phase differences, moving the perceived acoustic image outside of the listener's head.
2. **Room Echo and Acoustic Reverberation Chambers**:
   - Five precision Schroeder reverberator acoustic presets: Living Room (0.4s), Studio Monitor (0.8s), Concert Hall (1.8s), Sacred Cathedral (3.5s), and Cavern Echo (4.5s).
   - Adjustable Reverb Wet/Dry Mix slider (0% to 100%).
3. **Parametric 31-Band ISO Equalizer**:
   - Standard 1/3-octave ISO frequencies: 20Hz through 20kHz with +/-15.0 dB gain sliders.
   - 31 factory-tuned genre presets including Rock, Jazz, Classical, Bass Boost, Voice Clarity, and Night Mode Dynamic Range Compression (DRC).
4. **Pitch Tuning and Vocal Suppression**:
   - SOLA pitch shifter adjustable across -12 to +12 semitones without altering playback speed.
   - Karaoke Vocal Remover isolating and attenuating center-panned vocals while preserving low-frequency bass lines.

---

## 8. Video Controller and Performance Profiles

Accessible via **Tools -> Performance & Shader Hub (F4)**:

1. **Hardware Performance Profiles (Tiers 1 to 5)**:
   - **Tier 1 (Eco / Battery Saver)**: Optimized for mobile laptops on battery; suppresses shaders, limits frame copies, caps background rendering.
   - **Tier 2 (Standard PC)**: Standard Direct3D 11 hardware decoding with Spline36 texture scaling and Reinhard tone-mapping.
   - **Tier 3 (Balanced - Default)**: Lanczos3 scaling, YADIF motion-adaptive deinterlacing, and Mobius HDR curve.
   - **Tier 4 (High-Performance)**: EWA-LanczosSharp / Jinc scaling, active debanding, and BT.2390 EETF HDR10 curve.
   - **Tier 5 (Master / Extreme GPU)**: Neural FSRCNNX 8-pass texture reconstruction, extreme deband, and 16-bit float scRGB swapchain presentation.
2. **Aspect Ratio and Frame Geometry**:
   - Toggle instantly between Auto, 16:9, 4:3, 16:10, 21:9, 1:1, and Fill modes via the `A` hotkey.

---

## 9. Live Stream Recorder and Snapshot Studio

1. **Live Telecast Stream Recorder**:
   - Press `R` or select **Tools -> Record Live Stream** while viewing any IPTV channel or media stream.
   - Chunks are written asynchronously to `%USERPROFILE%\Videos\PSD_Recordings\PSD_Rec_{Title}_{Timestamp}.ts` without dropping display frames.
   - Press `R` again to gracefully finalize and flush the stream container to disk.
2. **Snapshot Studio (`Ctrl+G`)**:
   - Single Frame Capture: Saves full-resolution lossless PNG/BMP frame with or without rendered subtitles.
   - Rapid Burst Capture: Captures 5, 10, 25, or 50 sequential frames at custom millisecond intervals.
   - Animated GIF Generator: Exports cropped loop sequences with customizable frame rate (15-60 fps).

---

## 10. Online Video and Media Downloader

Accessible via **Tools -> Online Media Downloader (Ctrl+D)**:

1. Paste any media stream or online video URL (supports YouTube, Twitter/X, Instagram, TikTok, direct HLS `.m3u8`, direct `.mp4`/`.mkv`).
2. Select target format: Best Quality (Video + Audio), 1080p Full HD, 720p HD, or Audio Only (MP3 320 kbps).
3. Choose destination folder (defaults to `%USERPROFILE%\Downloads`).
4. Click **Start Download**. The native chunked streaming engine displays real-time transfer speed, percentage progress, transferred bytes, and estimated time of arrival (ETA).
5. Completed downloads can be launched directly in PSD Player via the **Play in PSD Player** button.

---

## 11. Theme Selection and Customization

PSD Media Player includes 110 built-in skins organized into four distinct design archetypes:
1. **Classic Player Archetypes (25 skins)**: Authentic aesthetics inspired by KMPlayer Carbon, PotPlayer Black, VLC Modern Orange, GOM Minimal, MPC Retro Silver, Winamp Classic Gold, JetAudio Titanium, QuickTime Dark Onyx, BSPlayer, RealPlayer Neo Cyan, CyberLink PowerDVD, and Foobar.
2. **Modern Flat and Fluent (35 skins)**: Obsidian Dark, Cyber Teal, Pure White, Slate Graphite, Tokyo Night, Catppuccin, Nord Arctic, Dracula, Gruvbox, One Dark Pro, Monokai Pro, GitHub Dark, Material Deep Ocean, and Palenight.
3. **Studio Glass and Acrylic (25 skins)**: Windows 11 Mica Glass, Deep Frost Acrylic, Frosted Sapphire, Smoked Quartz, Emerald Tint, Amber Glow, Violet Mist, Obsidian Frost, and Rose Quartz Glass.
4. **High-Contrast and Gamer Themes (25 skins)**: Neon Cyberpunk, Synthwave 84, Matrix Emerald, Blood Dragon, Hyper Neon Pink, Toxic Lime, Acid Yellow, OLED Pure Black, Viper Crimson, and WCAG AAA Monochrome.

*Themes can be switched dynamically at runtime via the **Themes** menu without pausing or interrupting active video playback.*
