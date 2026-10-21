using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace OmniPlayer.UI.Views
{
    public class KeyboardShortcutItem
    {
        public string Key { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class GuideTopicItem
    {
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int TabIndex { get; set; }
        public string Snippet { get; set; } = string.Empty;
        public string Keywords { get; set; } = string.Empty;
    }

    public partial class HelpGuideDialog : Window
    {
        private readonly List<KeyboardShortcutItem> _allShortcuts = new();
        private ICollectionView? _shortcutsView;

        private readonly List<GuideTopicItem> _guideTopics = new();
        private readonly Button[] _navButtons = new Button[8];
        private string _activeCategoryFilter = "All";

        public HelpGuideDialog() : this(0)
        {
        }

        public HelpGuideDialog(int initialTabIndex = 0)
        {
            InitializeComponent();

            _navButtons[0] = NavTab1;
            _navButtons[1] = NavTab2;
            _navButtons[2] = NavTab3;
            _navButtons[3] = NavTab4;
            _navButtons[4] = NavTab5;
            _navButtons[5] = NavTab6;
            _navButtons[6] = NavTab7;
            _navButtons[7] = NavTab8;

            InitializeShortcuts();
            InitializeGuideTopics();

            SelectTab(Math.Clamp(initialTabIndex, 0, 7));
        }

        public void SelectTab(int index)
        {
            if (index < 0 || index >= _navButtons.Length) return;

            // Clear search overlay if navigating tabs directly
            if (PanelSearchResults != null)
            {
                PanelSearchResults.Visibility = Visibility.Collapsed;
            }

            GuideTabControl.SelectedIndex = index;

            for (int i = 0; i < _navButtons.Length; i++)
            {
                if (_navButtons[i] != null)
                {
                    _navButtons[i].Tag = (i == index) ? "Active" : null;
                }
            }

            UpdateFooterTip(index);
        }

        private void NavTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                int tabIdx = Array.IndexOf(_navButtons, btn);
                if (tabIdx >= 0)
                {
                    SelectTab(tabIdx);
                }
            }
        }

        private void BtnQuickShortcuts_Click(object sender, RoutedEventArgs e)
        {
            SelectTab(6); // Tab 7: Shortcuts
        }

        private void BtnQuickAbout_Click(object sender, RoutedEventArgs e)
        {
            SelectTab(7); // Tab 8: About
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #region Search Functionality
        private void TxtSearchGuide_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = TxtSearchGuide.Text.Trim();
            bool hasQuery = !string.IsNullOrWhiteSpace(query);

            TxtSearchPlaceholder.Visibility = hasQuery ? Visibility.Collapsed : Visibility.Visible;
            BtnClearSearch.Visibility = hasQuery ? Visibility.Visible : Visibility.Collapsed;

            if (!hasQuery)
            {
                PanelSearchResults.Visibility = Visibility.Collapsed;
                return;
            }

            // Also synchronize with shortcuts filter if on shortcuts tab
            if (GuideTabControl.SelectedIndex == 6 && TxtFilterShortcuts != null)
            {
                TxtFilterShortcuts.Text = query;
            }

            // Search across guide topics
            var matches = _guideTopics.Where(t =>
                t.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.Category.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.Snippet.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.Keywords.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

            TxtSearchResultCount.Text = $"Found {matches.Count} matching topic(s) for '{query}':";
            ItemsSearchResults.ItemsSource = matches;
            PanelSearchResults.Visibility = Visibility.Visible;
        }

        private void BtnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            TxtSearchGuide.Text = string.Empty;
            PanelSearchResults.Visibility = Visibility.Collapsed;
        }

        private void SearchResultItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is GuideTopicItem topic)
            {
                TxtSearchGuide.Text = string.Empty;
                PanelSearchResults.Visibility = Visibility.Collapsed;
                SelectTab(topic.TabIndex);
            }
        }
        #endregion

        #region Shortcuts Tab Filtering
        private void TxtFilterShortcuts_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TxtFilterShortcutsPlaceholder != null)
            {
                TxtFilterShortcutsPlaceholder.Visibility = string.IsNullOrWhiteSpace(TxtFilterShortcuts.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            ApplyShortcutsFilter();
        }

        private void CategoryFilter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string cat)
            {
                _activeCategoryFilter = cat;
                ApplyShortcutsFilter();
            }
        }

        private void ApplyShortcutsFilter()
        {
            if (_shortcutsView == null) return;

            string query = TxtFilterShortcuts?.Text.Trim() ?? string.Empty;

            _shortcutsView.Filter = obj =>
            {
                if (obj is not KeyboardShortcutItem item) return false;

                // Category match
                if (!_activeCategoryFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    if (!item.Category.Contains(_activeCategoryFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                // Text query match
                if (!string.IsNullOrEmpty(query))
                {
                    return item.Key.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           item.Action.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           item.Category.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           item.Description.Contains(query, StringComparison.OrdinalIgnoreCase);
                }

                return true;
            };
        }
        #endregion

        private void UpdateFooterTip(int tabIndex)
        {
            if (TxtFooterTip == null) return;

            TxtFooterTip.Text = tabIndex switch
            {
                0 => "Press V or F8 at any time during playback to cycle between Basic, Normal, Advanced, and Modern views.",
                1 => "Press Ctrl+L to open the 1,000+ Station Hub. Click 'Sync Online Feeds' to refresh live IPTV channel streams.",
                2 => "Press F3 or E to open the 31-Band Audio Equalizer. Toggle Night Mode DRC for late-night movie watching.",
                3 => "Use [ and ] to adjust playback speed smoothly in 0.1x steps. Human vocal pitch is automatically preserved.",
                4 => "Press S for 1-click snapshot capture. Press Ctrl+G to open the Burst and Animated GIF studio.",
                5 => "Press Ctrl+U to download videos from YouTube, Twitter/X, and web URLs for offline watching.",
                6 => "Press Space or K for Play/Pause. Use J and L for YouTube 10s backward/forward jumps.",
                7 => "PSD Media Player Pro Edition v2.5.0 LTS is powered by LibVLC 3.0.24, NAudio WASAPI DSP, and Direct3D 11VA.",
                _ => "Press F1 anywhere in the player to summon this interactive guide."
            };
        }

        private void InitializeShortcuts()
        {
            _allShortcuts.AddRange(new[]
            {
                // Playback
                new KeyboardShortcutItem { Key = "Space / K", Action = "Play / Pause", Category = "Playback", Description = "Toggle playback pause and resume with instant audio un-mute." },
                new KeyboardShortcutItem { Key = "Ctrl + S", Action = "Stop Playback", Category = "Playback", Description = "Stop media playback and reset timeline position to beginning." },
                new KeyboardShortcutItem { Key = "[", Action = "Speed -0.1x", Category = "Playback", Description = "Decrease playback speed by 0.1x with real-time OSD speed bubble." },
                new KeyboardShortcutItem { Key = "]", Action = "Speed +0.1x", Category = "Playback", Description = "Increase playback speed by 0.1x with real-time OSD speed bubble." },
                new KeyboardShortcutItem { Key = "Backspace", Action = "Reset Speed 1.0x", Category = "Playback", Description = "Reset playback speed back to 1.0x normal standard rate." },
                new KeyboardShortcutItem { Key = ". (Period)", Action = "Step Frame Forward", Category = "Playback", Description = "Pause and advance video playback forward by exactly 1 frame." },
                new KeyboardShortcutItem { Key = ", (Comma)", Action = "Step Frame Backward", Category = "Playback", Description = "Pause and step backward 1 frame for precision frame analysis." },

                // Seeking
                new KeyboardShortcutItem { Key = "Left Arrow", Action = "Seek -5 Seconds", Category = "Seeking", Description = "Jump backward 5 seconds on the media timeline." },
                new KeyboardShortcutItem { Key = "Right Arrow", Action = "Seek +5 Seconds", Category = "Seeking", Description = "Jump forward 5 seconds on the media timeline." },
                new KeyboardShortcutItem { Key = "Shift + Left", Action = "Fine Seek -1s", Category = "Seeking", Description = "Precision 1-second fine rewind step." },
                new KeyboardShortcutItem { Key = "Shift + Right", Action = "Fine Seek +1s", Category = "Seeking", Description = "Precision 1-second fine forward step." },
                new KeyboardShortcutItem { Key = "Ctrl + Left", Action = "Jump -30 Seconds", Category = "Seeking", Description = "Coarse rewind jump 30 seconds backward." },
                new KeyboardShortcutItem { Key = "Ctrl + Right", Action = "Jump +30 Seconds", Category = "Seeking", Description = "Coarse forward jump 30 seconds forward." },
                new KeyboardShortcutItem { Key = "J", Action = "YouTube Seek -10s", Category = "Seeking", Description = "Standard YouTube-style 10-second rewind with HUD animation." },
                new KeyboardShortcutItem { Key = "L", Action = "YouTube Seek +10s", Category = "Seeking", Description = "Standard YouTube-style 10-second forward jump with HUD animation." },
                new KeyboardShortcutItem { Key = "0", Action = "Jump to Start (0%)", Category = "Seeking", Description = "Jump immediately to beginning of media." },
                new KeyboardShortcutItem { Key = "1 to 9", Action = "Jump 10% - 90%", Category = "Seeking", Description = "Jump to 10%, 20%, 30% ... 90% timeline milestone instantly." },

                // Audio & Volume
                new KeyboardShortcutItem { Key = "Up Arrow", Action = "Volume Up (+2%)", Category = "Audio", Description = "Increment master audio output level with glowing OSD volume bar." },
                new KeyboardShortcutItem { Key = "Down Arrow", Action = "Volume Down (-2%)", Category = "Audio", Description = "Decrement master audio output level with glowing OSD volume bar." },
                new KeyboardShortcutItem { Key = "M", Action = "Mute / Unmute", Category = "Audio", Description = "Toggle audio mute state with on-screen indicator." },
                new KeyboardShortcutItem { Key = "F3 / E", Action = "Audio Equalizer", Category = "Audio", Description = "Open 31-Band Parametric Equalizer and DSP Spatializer dialog." },
                new KeyboardShortcutItem { Key = "Ctrl + D", Action = "Audio Delay +/-", Category = "Audio", Description = "Nudge audio track synchronization offset for lip-sync alignment." },

                // Video & Display
                new KeyboardShortcutItem { Key = "F / F11 / Enter", Action = "Toggle Fullscreen", Category = "Video", Description = "Switch seamlessly between windowed and borderless fullscreen." },
                new KeyboardShortcutItem { Key = "Esc", Action = "Exit Fullscreen", Category = "Video", Description = "Exit fullscreen mode or dismiss on-screen telemetry overlays." },
                new KeyboardShortcutItem { Key = "A", Action = "Cycle Aspect Ratio", Category = "Video", Description = "Cycle Default, 16:9, 4:3, 21:9 Ultrawide, 1.85:1, 2.39:1, and Stretch." },
                new KeyboardShortcutItem { Key = "H / Ctrl + H", Action = "Hide / Show Controls", Category = "Video", Description = "Toggle HUD controls visibility for unobstructed cinema view." },
                new KeyboardShortcutItem { Key = "V / F8", Action = "Cycle View Modes", Category = "Video", Description = "Cycle between Basic, Normal, Advanced Studio HUD, and Modern Glass." },
                new KeyboardShortcutItem { Key = "Ctrl + 1", Action = "Basic View Mode", Category = "Video", Description = "Switch to minimalist borderless cinema viewport." },
                new KeyboardShortcutItem { Key = "Ctrl + 2", Action = "Normal View Mode", Category = "Video", Description = "Switch to standard desktop media player layout." },
                new KeyboardShortcutItem { Key = "Ctrl + 3", Action = "Advanced View Mode", Category = "Video", Description = "Switch to professional broadcast Studio Telemetry HUD." },
                new KeyboardShortcutItem { Key = "Ctrl + 4", Action = "Modern View Mode", Category = "Video", Description = "Switch to translucent acrylic/glass floating controls." },
                new KeyboardShortcutItem { Key = "F7 / Ctrl + P", Action = "Control Panel Hub", Category = "Video", Description = "Open real-time Picture, Color Grading, Audio, and Subtitle panel." },
                new KeyboardShortcutItem { Key = "F4 / P", Action = "Performance HUD", Category = "Video", Description = "Open Direct3D 11 hardware telemetry and GPU render statistics." },
                new KeyboardShortcutItem { Key = "T", Action = "Cycle Themes (110+)", Category = "Video", Description = "Instantly switch to next dynamic skin across 110+ curated themes." },

                // IPTV, Streams & Tools
                new KeyboardShortcutItem { Key = "Ctrl + L", Action = "Global IPTV Hub", Category = "Tools", Description = "Open 1,000+ Live TV Channels Hub with regional Indian & global feeds." },
                new KeyboardShortcutItem { Key = "Ctrl + O", Action = "Open Local File", Category = "Tools", Description = "Summon file picker for video, audio, and disc playback." },
                new KeyboardShortcutItem { Key = "Ctrl + U", Action = "Open Stream / Downloader", Category = "Tools", Description = "Open Network URL dialog and online video downloader." },
                new KeyboardShortcutItem { Key = "S", Action = "Instant Snapshot", Category = "Tools", Description = "1-click high-resolution GPU frame capture saved to Pictures." },
                new KeyboardShortcutItem { Key = "Ctrl + G", Action = "Capture Studio", Category = "Tools", Description = "Open Snapshot Studio for Burst Sequence and Animated GIF export." },
                new KeyboardShortcutItem { Key = "Ctrl + I", Action = "Media Information", Category = "Tools", Description = "Inspect video codec, audio bitrate, stream format, and metadata." },
                new KeyboardShortcutItem { Key = "Ctrl + ,", Action = "Settings & Preferences", Category = "Tools", Description = "Open global player configuration, performance profiles, and hotkeys." },
                new KeyboardShortcutItem { Key = "F1", Action = "Interactive Help Guide", Category = "Tools", Description = "Open this comprehensive master user manual and shortcut guide." }
            });

            _shortcutsView = CollectionViewSource.GetDefaultView(_allShortcuts);
            ItemsShortcutsList.ItemsSource = _shortcutsView;
        }

        private void InitializeGuideTopics()
        {
            _guideTopics.AddRange(new[]
            {
                // Tab 1
                new GuideTopicItem { Title = "Basic View Mode", Category = "View Modes", TabIndex = 0, Snippet = "Ultra-minimalist borderless viewport, hidden title bar and menus, zero chrome.", Keywords = "basic view mode cinema minimal borderless clean canvas" },
                new GuideTopicItem { Title = "Normal View Mode", Category = "View Modes", TabIndex = 0, Snippet = "Standard desktop player interface with top menu bar and bottom transport controls.", Keywords = "normal view mode standard desktop controls menu bar" },
                new GuideTopicItem { Title = "Advanced Studio Telemetry HUD", Category = "View Modes", TabIndex = 0, Snippet = "Real-time Direct3D 11 hardware telemetry, FPS counter, bitrates, audio channel meters.", Keywords = "advanced studio hud telemetry fps dropped frames latency bitrate" },
                new GuideTopicItem { Title = "Modern Glass HUD", Category = "View Modes", TabIndex = 0, Snippet = "Translucent acrylic/mica glass floating on-screen display controls with auto-hide.", Keywords = "modern view mode glass acrylic mica translucent floating controls hud" },
                new GuideTopicItem { Title = "Opening Media & Drag-and-Drop", Category = "Media Loading", TabIndex = 0, Snippet = "Open files, DVD/Blu-ray discs, URLs, and drag subtitle files (.srt/.ass/.vtt).", Keywords = "open file disc dvd blu-ray drag drop subtitles srt ass vtt" },
                new GuideTopicItem { Title = "Timeline Seeking & Number Keys", Category = "Navigation", TabIndex = 0, Snippet = "Use 0 to 9 keys to jump to percentage milestones or click anywhere on seekbar.", Keywords = "seeking timeline number keys percentage 0-9 milestones jump" },

                // Tab 2
                new GuideTopicItem { Title = "Global IPTV 1000+ Channel Hub", Category = "IPTV", TabIndex = 1, Snippet = "Access 1000+ free high-definition live channels with regional search and filters.", Keywords = "iptv live tv channels stream free stations hub search" },
                new GuideTopicItem { Title = "Indian Regional TV Channels", Category = "IPTV", TabIndex = 1, Snippet = "Tamil, Telugu, Malayalam, Kannada, Bengali, Marathi, Punjabi, Gujarati, and Hindi.", Keywords = "indian regional channels tamil telugu malayalam kannada bengali marathi punjabi gujarati hindi sun tv asianet tv9 dd news" },
                new GuideTopicItem { Title = "GitHub Online Feed Sync", Category = "IPTV", TabIndex = 1, Snippet = "1-click sync live playlists directly from authoritative open-source IPTV repos.", Keywords = "sync online feeds github m3u playlist refresh live streams" },
                new GuideTopicItem { Title = "Import Custom M3U / M3U8 Playlists", Category = "IPTV", TabIndex = 1, Snippet = "Load personal IPTV subscriptions, local playlist files, and custom streaming links.", Keywords = "custom m3u m3u8 playlist import local file url epg" },

                // Tab 3
                new GuideTopicItem { Title = "Dolby Atmos 3D Simulation", Category = "Audio DSP", TabIndex = 2, Snippet = "Virtual object-based spatial rendering creating height and depth dimensions.", Keywords = "dolby atmos 3d spatial spatializer height depth surround virtual" },
                new GuideTopicItem { Title = "DTS:X Spatializer & Acoustic Holography", Category = "Audio DSP", TabIndex = 2, Snippet = "Dynamic room reflection modeling for precise directional sound localization.", Keywords = "dts dts:x spatializer acoustic holography room reflections" },
                new GuideTopicItem { Title = "5.1 / 7.1 Surround Sound Upmix", Category = "Audio DSP", TabIndex = 2, Snippet = "Matrix steering converts 2-channel stereo tracks into an immersive surround soundscape.", Keywords = "5.1 7.1 surround upmix matrix stereo multi-channel lfe subwoofer center" },
                new GuideTopicItem { Title = "Headphone 3D HRTF", Category = "Audio DSP", TabIndex = 2, Snippet = "Binaural ear-pinna acoustic impulse filters for fatigue-free out-of-head listening.", Keywords = "hrtf headphone binaural 3d out of head impulse spatial ear pinna" },
                new GuideTopicItem { Title = "Studio 31-Band Parametric Equalizer", Category = "Audio DSP", TabIndex = 2, Snippet = "High-precision Biquad IIR peaking filters from 20 Hz to 20,000 Hz with ±20 dB gain.", Keywords = "equalizer eq 31-band 10-band biquad parametric audio dsp presets" },
                new GuideTopicItem { Title = "Night Mode DRC & Dialogue Boost", Category = "Audio DSP", TabIndex = 2, Snippet = "Dynamic range compression attenuates loud explosions and amplifies faint dialogue.", Keywords = "night mode drc dynamic range compression dialogue voice normalization speech quiet" },
                new GuideTopicItem { Title = "Karaoke Vocal Suppressor & Pitch Tuning", Category = "Audio DSP", TabIndex = 2, Snippet = "Real-time vocal removal for karaoke singing and ±12 semitone musical pitch transposition.", Keywords = "karaoke vocal suppressor vocal remove vocal cut pitch tune transpose semitones key" },
                new GuideTopicItem { Title = "WASAPI Bitstream Passthrough", Category = "Audio DSP", TabIndex = 2, Snippet = "Bit-perfect raw digital passthrough of AC3, E-AC3, TrueHD Atmos, and DTS-HD MA.", Keywords = "wasapi bitstream passthrough hdmi spdif ac3 truehd dts-hd" },

                // Tab 4
                new GuideTopicItem { Title = "Variable Speed 0.1x - 4.0x", Category = "Speed Controls", TabIndex = 3, Snippet = "Smooth slider from 0.1x to 4.0x with automatic vocal pitch preservation.", Keywords = "speed variable playback rate 0.1x 4.0x pitch preservation time stretch wsola bracket hotkeys" },
                new GuideTopicItem { Title = "Frame-by-Frame Stepping", Category = "Video Controls", TabIndex = 3, Snippet = "Advance forward with '.' (Period) or step backward with ',' (Comma).", Keywords = "frame step period comma advance reverse sports inspection motion" },
                new GuideTopicItem { Title = "Aspect Ratio Cycling", Category = "Video Controls", TabIndex = 3, Snippet = "Cycle Default, 16:9, 4:3, 21:9 Ultrawide CinemaScope, and Stretch via 'A'.", Keywords = "aspect ratio 16:9 4:3 21:9 ultrawide cinemascope stretch hotkey a" },
                new GuideTopicItem { Title = "Hardware Acceleration Direct3D 11VA", Category = "Video Engine", TabIndex = 3, Snippet = "Zero-copy GPU decoding pipeline (Intel QuickSync, NVIDIA NVDEC, AMD AMF).", Keywords = "hardware acceleration d3d11va direct3d 11 gpu zero copy nvdec quicksync amf 8k hdr" },

                // Tab 5
                new GuideTopicItem { Title = "1-Click Live Telecast Recording", Category = "Recording", TabIndex = 4, Snippet = "Record live IPTV telecasts to MP4/TS with direct stream copy and zero CPU overhead.", Keywords = "recording record telecast live tv iptv mp4 ts direct stream copy" },
                new GuideTopicItem { Title = "Direct3D 11 Snapshot Frame Grabber", Category = "Capture", TabIndex = 4, Snippet = "Instant 1-click snapshot via 'S' key, lossless PNG, WebP, BMP, and subtitle burn.", Keywords = "snapshot frame grabber screenshot key s png webp bmp subtitle burn" },
                new GuideTopicItem { Title = "Continuous Burst Sequence Capture", Category = "Capture", TabIndex = 4, Snippet = "Capture rapid sequences of 5, 10, 25, or 50 consecutive frames at native FPS.", Keywords = "burst sequence capture consecutive frames action sports rapid" },
                new GuideTopicItem { Title = "Animated GIF Generator", Category = "Capture", TabIndex = 4, Snippet = "Set In and Out markers to generate lightweight looping GIFs from video clips.", Keywords = "gif animated generator loop markers in out clip" },

                // Tab 6
                new GuideTopicItem { Title = "Online Video Downloader", Category = "Downloader", TabIndex = 5, Snippet = "Download videos from YouTube, Twitter/X, Instagram, and web links for offline watching.", Keywords = "online video downloader download offline youtube twitter x instagram reels clips" },
                new GuideTopicItem { Title = "Integrated yt-dlp & Stream Ripper", Category = "Downloader", TabIndex = 5, Snippet = "Native yt-dlp integration, multi-resolution selection (4K, 1080p), and audio extraction.", Keywords = "yt-dlp stream ripper 4k 1080p audio mp3 extractor subtitle" },

                // Tab 7
                new GuideTopicItem { Title = "Master Keyboard Shortcuts Cheatsheet", Category = "Shortcuts", TabIndex = 6, Snippet = "Searchable grid of 40+ keyboard shortcuts for playback, seeking, audio, and display.", Keywords = "shortcuts keyboard hotkeys cheatsheet table grid keys" },

                // Tab 8
                new GuideTopicItem { Title = "About PSD Media Player Pro Edition", Category = "About", TabIndex = 7, Snippet = "Version 2.5.0 LTS, LibVLC 3.0.24, NAudio WASAPI DSP, and system architecture.", Keywords = "about version 2.5.0 lts architecture libvlc naudio direct3d developer credits" }
            });
        }
    }
}
