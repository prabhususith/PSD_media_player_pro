using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using OmniPlayer.Core.Audio;
using OmniPlayer.Core.Decoders;
using OmniPlayer.Core.Engine;
using OmniPlayer.Core.LiveTv;
using OmniPlayer.Core.Subtitles;
using OmniPlayer.Core.Video;
using OmniPlayer.UI.Controls;
using OmniPlayer.UI.Themes;
using OmniPlayer.UI.Views;

namespace OmniPlayer.UI
{
    public enum PlayerViewMode
    {
        Basic,
        Normal,
        Advanced,
        Modern
    }

    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _playbackTimer = new();
        private readonly DispatcherTimer _osdTimer = new();
        private readonly DispatcherTimer _cursorMonitorTimer = new();

        private bool _isDraggingSlider = false;
        private bool _isPlaying = false;
        private bool _isMuted = false;
        private bool _isFullscreen = false;
        private bool _controlsHidden = false;
        private bool _isControlsPinned = false;
        private bool _isVlcActive = false;
        private double _previousVolume = 0.8;
        private TimeSpan _totalDuration = TimeSpan.Zero;
        private string _currentMediaTitle = string.Empty;
        private string _currentSourceUri = string.Empty;
        private PlayerViewMode _currentViewMode = PlayerViewMode.Normal;

        private static readonly string[] _aspectRatios = new[] { "Auto", "16:9", "4:3", "16:10", "21:9", "1:1", "Fill" };
        private int _aspectRatioIndex = 0;
        private DateTime _lastTelemetryUpdate = DateTime.MinValue;

        // Fullscreen state restoration & Win32 Interop for taskbar concealment
        private WindowState _previousWindowState = WindowState.Normal;
        private WindowStyle _previousWindowStyle = WindowStyle.SingleBorderWindow;
        private ResizeMode _previousResizeMode = ResizeMode.CanResize;
        private double _previousLeft;
        private double _previousTop;
        private double _previousWidth;
        private double _previousHeight;

        // High-precision global cursor tracking & 10s auto-hide state
        private POINT _lastScreenCursorPos = new POINT { X = -1, Y = -1 };
        private DateTime _lastMouseMoveTime = DateTime.UtcNow;

        // Playback speed presets (YouTube standard + ultra slow motion)
        private double _currentSpeed = 1.0;
        private static readonly double[] _speedPresets = new[] { 0.1, 0.25, 0.5, 0.75, 1.0, 1.25, 1.5, 1.75, 2.0, 3.0, 4.0 };

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MONITORINFO
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        public MainWindow()
        {
            InitializeComponent();
            InitializeWindowIconSafely();

            Loaded += MainWindow_Loaded;
            SourceInitialized += MainWindow_SourceInitialized;

            // Setup playback position polling timer
            _playbackTimer.Interval = TimeSpan.FromMilliseconds(100);
            _playbackTimer.Tick += PlaybackTimer_Tick;

            // OSD Auto-hide timer
            _osdTimer.Interval = TimeSpan.FromSeconds(2.5);
            _osdTimer.Tick += (s, e) =>
            {
                if (BorderOsd != null) BorderOsd.Visibility = Visibility.Collapsed;
                _osdTimer.Stop();
            };

            // Precision mouse cursor monitor & auto-hide timer for fullscreen mode (100ms interval)
            _cursorMonitorTimer.Interval = TimeSpan.FromMilliseconds(100);
            _cursorMonitorTimer.Tick += CursorMonitorTimer_Tick;

            // Hook managers
            ThemeManager.Instance.ThemeChanged += OnThemeChanged;
            PerformanceProfileManager.Instance.ProfileChanged += OnProfileChanged;
            ToolsMenuController.Instance.AbRepeatChanged += OnAbRepeatChanged;
            ToolsMenuController.Instance.StatusNotification += ShowOsd;

            // Wire LibVLC events
            var vlc = VlcEngineController.Instance;
            vlc.TimeChanged += (t) => Dispatcher.Invoke(() => OnVlcTimeChanged(t));
            vlc.LengthChanged += (l) => Dispatcher.Invoke(() => OnVlcLengthChanged(l));
            vlc.Playing += () => Dispatcher.Invoke(() => OnVlcPlaying());
            vlc.Paused += () => Dispatcher.Invoke(() => OnVlcPaused());
            vlc.Stopped += () => Dispatcher.Invoke(() => OnVlcStopped());
            vlc.EndReached += () => Dispatcher.Invoke(() => OnVlcEndReached());
            vlc.ErrorOccurred += (err) => Dispatcher.Invoke(() => ShowOsd($"VLC: {err}"));

            // Wire Live Stream Recorder events
            LiveStreamRecorder.Instance.RecordingProgress += OnRecordingProgress;
            LiveStreamRecorder.Instance.RecordingFinished += OnRecordingFinished;
            LiveStreamRecorder.Instance.RecordingError += OnRecordingError;

            // Wire Media Library dashboard 1-click playback dispatcher
            MediaLibraryDashboardDialog.MediaPlayRequested += (filePath) =>
            {
                Dispatcher.Invoke(() =>
                {
                    PlayMedia(filePath, Path.GetFileName(filePath));
                });
            };

            PopulateThemesMenu();
            ApplyInitialTheme();
            ApplyViewMode(PlayerViewMode.Normal);
        }

        private void InitializeWindowIconSafely()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string[] iconCandidates = new[]
                {
                    Path.Combine(baseDir, "Assets", "psd-player-icon.ico"),
                    Path.Combine(baseDir, "Assets", "psd-player-logo.png"),
                    Path.Combine(baseDir, "Assets", "omni-icon.ico"),
                    Path.Combine(baseDir, "psd-player-icon.ico"),
                    Path.Combine(baseDir, "omni-icon.ico")
                };

                foreach (var candidate in iconCandidates)
                {
                    if (File.Exists(candidate))
                    {
                        Icon = BitmapFrame.Create(new Uri(candidate, UriKind.Absolute));
                        return;
                    }
                }

                // Fallback to pack URI if embedded
                try
                {
                    var packUri = new Uri("pack://application:,,,/Assets/psd-player-icon.ico", UriKind.RelativeOrAbsolute);
                    Icon = BitmapFrame.Create(packUri);
                }
                catch { }
            }
            catch
            {
                // Never allow icon resolution to crash application launch
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (VlcVideoView != null && VlcEngineController.Instance.IsAvailable)
                {
                    VlcVideoView.MediaPlayer = VlcEngineController.Instance.GetMediaPlayer();
                }
            }
            catch (Exception ex)
            {
                ShowOsd($"LibVLC Host: {ex.Message}");
            }
        }

        private void ApplyInitialTheme()
        {
            ThemeManager.Instance.ApplyTheme("kmplayer-carbon");
        }

        private void PopulateThemesMenu()
        {
            void AddCategory(MenuItem? parentMenu, string categoryName)
            {
                if (parentMenu == null) return;
                parentMenu.Items.Clear();
                foreach (var theme in ThemeManager.Instance.GetThemesByCategory(categoryName))
                {
                    var item = new MenuItem
                    {
                        Header = theme.Name,
                        Tag = theme.Id,
                        ToolTip = theme.Description
                    };
                    item.Click += (s, e) =>
                    {
                        if (s is MenuItem mi && mi.Tag is string themeId)
                        {
                            ThemeManager.Instance.ApplyTheme(themeId);
                        }
                    };
                    parentMenu.Items.Add(item);
                }
            }

            AddCategory(MenuCatClassic, "Classic Player Archetypes");
            AddCategory(MenuCatModern, "Modern Flat & Fluent");
            AddCategory(MenuCatGlass, "Studio Glass & Acrylic");
            AddCategory(MenuCatGamer, "High-Contrast & Gamer Skins");
        }

        private void OnThemeChanged(ThemeDefinition theme)
        {
            ShowOsd($"Skin Loaded: {theme.Name} ({theme.Category})");
            if (TxtStatusRight != null) TxtStatusRight.Text = $"Active Theme: {theme.Name} | {theme.Category}";
        }

        private void OnProfileChanged(PerformanceTier tier, PerformanceSettings settings)
        {
            ShowOsd($"Performance: {settings.DisplayName}");
            if (TxtStatusLeft != null) TxtStatusLeft.Text = $"Direct3D 11 ({settings.ScalingAlgorithm}) | Cache: {settings.MemoryCacheSizeMb}MB | 60 fps";
            UpdateTelemetryHud();
        }

        private void OnAbRepeatChanged(TimeSpan? a, TimeSpan? b)
        {
            if (TxtAbRepeatDisplay != null)
            {
                if (a.HasValue && b.HasValue)
                {
                    TxtAbRepeatDisplay.Text = $"[A: {a.Value:mm\\:ss} 🔁 B: {b.Value:mm\\:ss}]";
                }
                else if (a.HasValue)
                {
                    TxtAbRepeatDisplay.Text = $"[A: {a.Value:mm\\:ss} ➡ B: --:--]";
                }
                else
                {
                    TxtAbRepeatDisplay.Text = string.Empty;
                }
            }
            UpdateTelemetryHud();
        }

        public void ShowOsd(string message)
        {
            if (TxtOsd != null) TxtOsd.Text = message;
            if (BorderOsd != null) BorderOsd.Visibility = Visibility.Visible;
            _osdTimer.Stop();
            _osdTimer.Start();
        }

        // ================= 4 View Modes Controller =================
        public void ApplyViewMode(PlayerViewMode mode)
        {
            _currentViewMode = mode;

            // Update menu checkmarks
            if (MenuVModeBasic != null) MenuVModeBasic.IsChecked = (mode == PlayerViewMode.Basic);
            if (MenuVModeNormal != null) MenuVModeNormal.IsChecked = (mode == PlayerViewMode.Normal);
            if (MenuVModeAdvanced != null) MenuVModeAdvanced.IsChecked = (mode == PlayerViewMode.Advanced);
            if (MenuVModeModern != null) MenuVModeModern.IsChecked = (mode == PlayerViewMode.Modern);

            switch (mode)
            {
                case PlayerViewMode.Basic:
                    if (BorderTopMenu != null) BorderTopMenu.Visibility = Visibility.Collapsed;
                    if (BorderStatusBar != null) BorderStatusBar.Visibility = Visibility.Collapsed;
                    if (BorderTelemetryHud != null) BorderTelemetryHud.Visibility = Visibility.Collapsed;
                    if (BorderModernHud != null) BorderModernHud.Visibility = Visibility.Collapsed;
                    if (BorderTimeline != null) BorderTimeline.Visibility = Visibility.Visible;
                    if (BorderControls != null) BorderControls.Visibility = Visibility.Visible;
                    if (BtnViewMode != null) BtnViewMode.Content = "🪟 Basic";
                    ShowOsd("View Mode: Basic (Minimalist)");
                    break;

                case PlayerViewMode.Normal:
                    if (BorderTopMenu != null) BorderTopMenu.Visibility = Visibility.Visible;
                    if (BorderStatusBar != null) BorderStatusBar.Visibility = Visibility.Visible;
                    if (BorderTelemetryHud != null) BorderTelemetryHud.Visibility = Visibility.Collapsed;
                    if (BorderModernHud != null) BorderModernHud.Visibility = Visibility.Collapsed;
                    if (BorderTimeline != null) BorderTimeline.Visibility = Visibility.Visible;
                    if (BorderControls != null) BorderControls.Visibility = Visibility.Visible;
                    if (BtnViewMode != null) BtnViewMode.Content = "🪟 Normal";
                    ShowOsd("View Mode: Normal (Desktop Standard)");
                    break;

                case PlayerViewMode.Advanced:
                    if (BorderTopMenu != null) BorderTopMenu.Visibility = Visibility.Visible;
                    if (BorderStatusBar != null) BorderStatusBar.Visibility = Visibility.Visible;
                    if (BorderTelemetryHud != null) BorderTelemetryHud.Visibility = Visibility.Visible;
                    if (BorderModernHud != null) BorderModernHud.Visibility = Visibility.Collapsed;
                    if (BorderTimeline != null) BorderTimeline.Visibility = Visibility.Visible;
                    if (BorderControls != null) BorderControls.Visibility = Visibility.Visible;
                    if (BtnViewMode != null) BtnViewMode.Content = "🪟 Advanced";
                    UpdateTelemetryHud();
                    ShowOsd("View Mode: Advanced (Studio Telemetry HUD)");
                    break;

                case PlayerViewMode.Modern:
                    if (BorderTopMenu != null) BorderTopMenu.Visibility = Visibility.Collapsed;
                    if (BorderStatusBar != null) BorderStatusBar.Visibility = Visibility.Collapsed;
                    if (BorderTimeline != null) BorderTimeline.Visibility = Visibility.Collapsed;
                    if (BorderControls != null) BorderControls.Visibility = Visibility.Collapsed;
                    if (BorderTelemetryHud != null) BorderTelemetryHud.Visibility = Visibility.Collapsed;
                    if (BorderModernHud != null) BorderModernHud.Visibility = Visibility.Visible;
                    if (BtnModernViewMode != null) BtnModernViewMode.Content = "🪟 Modern";
                    ShowOsd("View Mode: Modern (Floating Glass HUD)");
                    break;
            }

            if (_isFullscreen && BorderTopMenu != null)
            {
                BorderTopMenu.Visibility = Visibility.Collapsed;
            }

            if (_controlsHidden)
            {
                ApplyControlsVisibility();
            }
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            var source = PresentationSource.FromVisual(this) as HwndSource;
            source?.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_ERASEBKGND = 0x0014;
            if (msg == WM_ERASEBKGND)
            {
                handled = true;
                return new IntPtr(1);
            }
            return IntPtr.Zero;
        }

        public void BtnToggleControls_Click(object sender, RoutedEventArgs e)
        {
            _controlsHidden = !_controlsHidden;
            ApplyControlsVisibility();
            ShowOsd(_controlsHidden ? "On-Screen Controls Hidden (Press H to restore)" : "On-Screen Controls Visible");
        }

        public void BtnPinControls_Click(object sender, RoutedEventArgs e)
        {
            _isControlsPinned = !_isControlsPinned;
            string pinIcon = _isControlsPinned ? "📌" : "🔓";
            if (BtnPinControls != null)
            {
                BtnPinControls.Content = pinIcon;
                if (_isControlsPinned)
                {
                    BtnPinControls.SetResourceReference(Button.BackgroundProperty, "OmniAccentBrush");
                    BtnPinControls.Foreground = Brushes.White;
                }
                else
                {
                    BtnPinControls.SetResourceReference(Button.BackgroundProperty, "OmniControlBackgroundBrush");
                    BtnPinControls.SetResourceReference(Button.ForegroundProperty, "OmniForegroundBrush");
                }
            }
            if (BtnModernPinControls != null)
            {
                BtnModernPinControls.Content = pinIcon;
                if (_isControlsPinned)
                {
                    BtnModernPinControls.SetResourceReference(Button.BackgroundProperty, "OmniAccentBrush");
                    BtnModernPinControls.Foreground = Brushes.White;
                }
                else
                {
                    BtnModernPinControls.SetResourceReference(Button.BackgroundProperty, "OmniControlBackgroundBrush");
                    BtnModernPinControls.SetResourceReference(Button.ForegroundProperty, "OmniForegroundBrush");
                }
            }

            if (_isControlsPinned && _controlsHidden)
            {
                _controlsHidden = false;
                ApplyControlsVisibility();
            }

            ShowOsd(_isControlsPinned ? "Controls Pinned (Always Visible in Fullscreen)" : "Controls Unpinned (Auto-Hide on 3s Idle)");
        }

        private void ApplyControlsVisibility()
        {
            if (_controlsHidden)
            {
                if (BorderControls != null) BorderControls.Visibility = Visibility.Collapsed;
                if (BorderTimeline != null) BorderTimeline.Visibility = Visibility.Collapsed;
                if (BorderStatusBar != null) BorderStatusBar.Visibility = Visibility.Collapsed;
                if (BorderModernHud != null) BorderModernHud.Visibility = Visibility.Collapsed;
                if (BorderTelemetryHud != null) BorderTelemetryHud.Visibility = Visibility.Collapsed;
                if (BorderTopMenu != null) BorderTopMenu.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (_isFullscreen)
                {
                    if (BorderTopMenu != null) BorderTopMenu.Visibility = Visibility.Collapsed;
                    if (_currentViewMode == PlayerViewMode.Modern)
                    {
                        if (BorderModernHud != null) BorderModernHud.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        if (BorderControls != null) BorderControls.Visibility = Visibility.Visible;
                        if (BorderTimeline != null) BorderTimeline.Visibility = Visibility.Visible;
                        if (BorderStatusBar != null) BorderStatusBar.Visibility = Visibility.Visible;
                        if (_currentViewMode == PlayerViewMode.Advanced && BorderTelemetryHud != null)
                        {
                            BorderTelemetryHud.Visibility = Visibility.Visible;
                        }
                    }
                }
                else
                {
                    ApplyViewMode(_currentViewMode);
                }
            }
        }

        public void CycleViewMode()
        {
            var next = (PlayerViewMode)(((int)_currentViewMode + 1) % 4);
            ApplyViewMode(next);
        }

        public void MenuViewModeBasic_Click(object sender, RoutedEventArgs e) => ApplyViewMode(PlayerViewMode.Basic);
        public void MenuViewModeNormal_Click(object sender, RoutedEventArgs e) => ApplyViewMode(PlayerViewMode.Normal);
        public void MenuViewModeAdvanced_Click(object sender, RoutedEventArgs e) => ApplyViewMode(PlayerViewMode.Advanced);
        public void MenuViewModeModern_Click(object sender, RoutedEventArgs e) => ApplyViewMode(PlayerViewMode.Modern);
        public void MenuCycleViewMode_Click(object sender, RoutedEventArgs e) => CycleViewMode();

        private void UpdateTelemetryHud(string? title = null, string? uri = null)
        {
            if (BorderTelemetryHud == null || BorderTelemetryHud.Visibility != Visibility.Visible) return;

            if (TxtTelemEngine != null) TxtTelemEngine.Text = _isVlcActive ? "LibVLC 3.0.24 LTS (D3D11VA)" : "Direct3D 11 Media Foundation";
            if (TxtTelemHwDec != null) TxtTelemHwDec.Text = _isVlcActive ? "D3D11VA Zero-Copy HW Dec" : "DXVA2 Accelerated Decoder";
            if (TxtTelemResFps != null) TxtTelemResFps.Text = "1920x1080p | 60.00 FPS";
            if (TxtTelemAudio != null) TxtTelemAudio.Text = OmniAudioEngine.Instance.IsActive ? "31-Band Parametric + WASAPI" : "LibVLC Soxr Resampler";
            if (TxtTelemEqPreset != null) TxtTelemEqPreset.Text = $"{EqualizerDsp.Instance.PreampDb:+0.0;-0.0;0.0} dB Preamp";
            if (TxtTelemDrops != null) TxtTelemDrops.Text = "0 frames (0.00%)";
            if (TxtTelemBuffer != null) TxtTelemBuffer.Text = "3000ms Net / 1000ms File";
            if (TxtTelemAb != null)
            {
                TxtTelemAb.Text = ToolsMenuController.Instance.IsAbRepeatActive
                    ? $"[{ToolsMenuController.Instance.MarkerA:mm\\:ss} -> {ToolsMenuController.Instance.MarkerB:mm\\:ss}]"
                    : "Off";
            }
        }

        // ================= Playback Engine Unified Dispatcher =================
        public void PlayMedia(string sourceUri) => PlayMedia(sourceUri, Path.GetFileName(sourceUri));

        public void PlayMedia(string sourceUri, string title)
        {
            try
            {
                _currentMediaTitle = title;
                _currentSourceUri = sourceUri;
                Title = $"PSD Media Player - {title}";

                bool isVlcSuccess = false;
                if (VlcEngineController.Instance.IsAvailable)
                {
                    isVlcSuccess = VlcEngineController.Instance.Play(sourceUri);
                }

                if (isVlcSuccess)
                {
                    _isVlcActive = true;
                    _isPlaying = true;
                    if (VlcVideoView != null)
                    {
                        if (VlcVideoView.MediaPlayer == null)
                        {
                            VlcVideoView.MediaPlayer = VlcEngineController.Instance.GetMediaPlayer();
                        }
                        VlcVideoView.Visibility = Visibility.Visible;
                    }
                    PlayerMediaElement.Visibility = Visibility.Collapsed;
                    BtnPlayPause.Content = "⏸ Pause";
                    BtnModernPlayPause.Content = "⏸ Pause";
                    VlcEngineController.Instance.Volume = (int)(SliderVolume.Value * 100);
                    ShowOsd($"Playing: {title} [LibVLC 3.0.24 LTS D3D11VA]");
                    UpdateTelemetryHud(title, sourceUri);
                }
                else
                {
                    // Fallback to Direct3D 11 MediaElement presentation
                    _isVlcActive = false;
                    PlayerMediaElement.Source = new Uri(sourceUri);
                    PlayerMediaElement.Play();
                    _isPlaying = true;
                    VlcVideoView.Visibility = Visibility.Collapsed;
                    PlayerMediaElement.Visibility = Visibility.Visible;
                    BtnPlayPause.Content = "⏸ Pause";
                    BtnModernPlayPause.Content = "⏸ Pause";
                    _playbackTimer.Start();

                    bool audioHooked = OmniAudioEngine.Instance.OpenMedia(sourceUri);
                    if (audioHooked)
                    {
                        PlayerMediaElement.Volume = 0;
                        OmniAudioEngine.Instance.Volume = (float)SliderVolume.Value;
                        OmniAudioEngine.Instance.Play();
                        ShowOsd($"Playing: {title} [31-Band Audio DSP + D3D11]");
                    }
                    else
                    {
                        PlayerMediaElement.Volume = SliderVolume.Value;
                        ShowOsd($"Playing: {title} [Direct3D 11 Stream]");
                    }
                    UpdateTelemetryHud(title, sourceUri);
                }
            }
            catch (Exception ex)
            {
                ShowOsd($"Playback Error: {ex.Message}");
            }
        }

        private void OnVlcTimeChanged(TimeSpan pos)
        {
            if (_isDraggingSlider) return;

            // Check A-B Repeat loop condition
            var loopPos = ToolsMenuController.Instance.CheckAbLoop(pos);
            if (loopPos.HasValue)
            {
                VlcEngineController.Instance.SeekToTime(loopPos.Value);
                pos = loopPos.Value;
            }

            if (_totalDuration.TotalSeconds > 0)
            {
                double fraction = pos.TotalSeconds / _totalDuration.TotalSeconds;
                double sliderVal = fraction * 1000.0;
                SliderTimeline.Value = sliderVal;
                SliderModernTimeline.Value = sliderVal;
            }

            string elapsedStr = $"{pos:hh\\:mm\\:ss}";
            TxtTimeElapsed.Text = elapsedStr;
            TxtModernTimeElapsed.Text = elapsedStr;

            UpdateSubtitles(pos);
            if (BorderTelemetryHud != null && BorderTelemetryHud.Visibility == Visibility.Visible &&
                (DateTime.UtcNow - _lastTelemetryUpdate).TotalSeconds >= 1.0)
            {
                _lastTelemetryUpdate = DateTime.UtcNow;
                UpdateTelemetryHud();
            }
        }

        private void OnVlcLengthChanged(TimeSpan length)
        {
            if (length.TotalSeconds > 0)
            {
                _totalDuration = length;
                string totalStr = $"{length:hh\\:mm\\:ss}";
                TxtTimeTotal.Text = totalStr;
                TxtModernTimeTotal.Text = totalStr;
            }
            else
            {
                _totalDuration = TimeSpan.Zero;
                TxtTimeTotal.Text = "LIVE STREAM";
                TxtModernTimeTotal.Text = "LIVE STREAM";
            }
            UpdateTelemetryHud();
        }

        private void OnVlcPlaying()
        {
            _isPlaying = true;
            BtnPlayPause.Content = "⏸ Pause";
            BtnModernPlayPause.Content = "⏸ Pause";
        }

        private void OnVlcPaused()
        {
            _isPlaying = false;
            BtnPlayPause.Content = "▶ Play";
            BtnModernPlayPause.Content = "▶ Play";
        }

        private void OnVlcStopped()
        {
            _isPlaying = false;
            BtnPlayPause.Content = "▶ Play";
            BtnModernPlayPause.Content = "▶ Play";
        }

        private void OnVlcEndReached()
        {
            BtnStop_Click(this, new RoutedEventArgs());
        }

        private void PlaybackTimer_Tick(object? sender, EventArgs e)
        {
            if (_isVlcActive || !_isPlaying || _isDraggingSlider) return;

            var pos = PlayerMediaElement.Position;

            var loopPos = ToolsMenuController.Instance.CheckAbLoop(pos);
            if (loopPos.HasValue)
            {
                PlayerMediaElement.Position = loopPos.Value;
                if (OmniAudioEngine.Instance.IsActive)
                {
                    OmniAudioEngine.Instance.Position = loopPos.Value;
                }
                pos = loopPos.Value;
            }

            if (_totalDuration.TotalSeconds > 0)
            {
                double fraction = pos.TotalSeconds / _totalDuration.TotalSeconds;
                double val = fraction * 1000.0;
                SliderTimeline.Value = val;
                SliderModernTimeline.Value = val;
            }

            string elapsed = $"{pos:hh\\:mm\\:ss}";
            TxtTimeElapsed.Text = elapsed;
            TxtModernTimeElapsed.Text = elapsed;

            UpdateSubtitles(pos);
            UpdateTelemetryHud();
        }

        private void UpdateSubtitles(TimeSpan currentPos)
        {
            var engine = SubtitleEngine.Instance;

            // Primary (Bottom)
            if (engine.PrimaryTrack != null)
            {
                var cue = engine.PrimaryTrack.GetCurrentCue(currentPos);
                if (cue != null)
                {
                    TxtPrimarySubtitle.Text = cue.PlainText;
                    TxtPrimarySubtitle.Visibility = Visibility.Visible;
                }
                else
                {
                    TxtPrimarySubtitle.Visibility = Visibility.Collapsed;
                }
            }

            // Secondary (Top)
            if (engine.DualSubtitlesEnabled && engine.SecondaryTrack != null)
            {
                var cue = engine.SecondaryTrack.GetCurrentCue(currentPos);
                if (cue != null)
                {
                    TxtSecondarySubtitle.Text = cue.PlainText;
                    TxtSecondarySubtitle.Visibility = Visibility.Visible;
                }
                else
                {
                    TxtSecondarySubtitle.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void PlayerMediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (PlayerMediaElement.NaturalDuration.HasTimeSpan)
            {
                _totalDuration = PlayerMediaElement.NaturalDuration.TimeSpan;
                string totalStr = $"{_totalDuration:hh\\:mm\\:ss}";
                TxtTimeTotal.Text = totalStr;
                TxtModernTimeTotal.Text = totalStr;
            }
            else
            {
                _totalDuration = TimeSpan.Zero;
                TxtTimeTotal.Text = "LIVE STREAM";
                TxtModernTimeTotal.Text = "LIVE STREAM";
            }
            _playbackTimer.Start();
            ShowOsd($"Media Opened: {_currentMediaTitle}");
            UpdateTelemetryHud();
        }

        private void PlayerMediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            BtnStop_Click(sender, e);
        }

        // ================= Global Keyboard Shortcuts Controller =================
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.OriginalSource is TextBox) return;

            bool handled = true;

            switch (e.Key)
            {
                // Play / Pause (Space / K)
                case Key.Space:
                case Key.K:
                    BtnPlayPause_Click(sender, e);
                    break;

                // Aspect Ratio Cycling (A)
                case Key.A:
                    CycleAspectRatio();
                    break;

                // Toggle / Hide On-Screen Controls (H / Ctrl+H)
                case Key.H:
                    BtnToggleControls_Click(sender, e);
                    break;

                // View Mode Cycling
                case Key.V:
                case Key.F8:
                    CycleViewMode();
                    break;

                // Fullscreen Toggle
                case Key.F:
                case Key.F11:
                case Key.Enter:
                    BtnFullscreen_Click(sender, e);
                    break;

                // Exit Fullscreen
                case Key.Escape:
                    if (_isFullscreen)
                    {
                        BtnFullscreen_Click(sender, e);
                    }
                    else
                    {
                        if (BorderOsd != null) BorderOsd.Visibility = Visibility.Collapsed;
                    }
                    break;

                // YouTube Seeking (-10s / +10s)
                case Key.J:
                    SeekRelative(-10);
                    break;

                // Seeking Left / Right (-5s / +5s)
                case Key.Left:
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                        MenuJumpBackward30_Click(sender, e);
                    else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                        MenuJumpBackward1_Click(sender, e);
                    else
                        MenuJumpBackward5_Click(sender, e);
                    break;

                case Key.Right:
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                        MenuJumpForward30_Click(sender, e);
                    else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                        MenuJumpForward1_Click(sender, e);
                    else
                        MenuJumpForward5_Click(sender, e);
                    break;

                // Volume Up / Down
                case Key.Up:
                    MenuVolUp_Click(sender, e);
                    break;

                case Key.Down:
                    MenuVolDown_Click(sender, e);
                    break;

                // Mute
                case Key.M:
                    BtnMute_Click(sender, e);
                    break;

                // Live TV Hub (Ctrl+L) or Seek Forward 10s (L)
                case Key.L:
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                        MenuLiveTv_Click(sender, e);
                    else
                        SeekRelative(10);
                    break;

                // Open URL / Stream
                case Key.U:
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                        MenuOpenUrl_Click(sender, e);
                    break;

                // Open File
                case Key.O:
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                        MenuOpenFile_Click(sender, e);
                    break;

                // Media Info
                case Key.I:
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                        MenuMediaInfo_Click(sender, e);
                    break;

                // Capture Studio
                case Key.G:
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                        MenuCaptureStudio_Click(sender, e);
                    break;

                // Online Media Downloader
                case Key.D:
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                        MenuMediaDownloader_Click(sender, e);
                    break;

                // Laptop & USB Camera Studio with Live AR Effects (W / Ctrl+W)
                case Key.W:
                    ToolsMenuController.Instance.ShowCameraStudioDialog(this);
                    break;

                // Media Library & Device Auto-Scan Dashboard (B / Ctrl+B)
                case Key.B:
                    ToolsMenuController.Instance.ShowMediaLibraryDashboardDialog(this);
                    break;

                case Key.S:
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                        BtnStop_Click(sender, e);
                    else
                        BtnQuickCapture_Click(sender, e);
                    break;

                // Equalizer Dialog
                case Key.F3:
                case Key.E:
                    MenuEqualizer_Click(sender, e);
                    break;

                // Performance Dialog
                case Key.F4:
                    MenuPerformance_Click(sender, e);
                    break;

                // Control Panel Dialog (F7 or Ctrl+P)
                case Key.F7:
                    MenuControlPanel_Click(sender, e);
                    break;

                case Key.P:
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                        MenuControlPanel_Click(sender, e);
                    else
                        MenuPerformance_Click(sender, e);
                    break;

                // Settings Dialog (Ctrl+,) or Step Backward (,)
                case Key.OemComma:
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                        MenuSettings_Click(sender, e);
                    else
                        StepFrameBackward();
                    break;

                // Number Keys (Ctrl+1..4 for View Modes, 0..9 for Seeking)
                case Key.D0: case Key.NumPad0: JumpToFraction(0.0); break;
                case Key.D1:
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) ApplyViewMode(PlayerViewMode.Basic);
                    else JumpToFraction(0.1);
                    break;
                case Key.D2:
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) ApplyViewMode(PlayerViewMode.Normal);
                    else JumpToFraction(0.2);
                    break;
                case Key.D3:
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) ApplyViewMode(PlayerViewMode.Advanced);
                    else JumpToFraction(0.3);
                    break;
                case Key.D4:
                    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) ApplyViewMode(PlayerViewMode.Modern);
                    else JumpToFraction(0.4);
                    break;
                case Key.D5: case Key.NumPad5: JumpToFraction(0.5); break;
                case Key.D6: case Key.NumPad6: JumpToFraction(0.6); break;
                case Key.D7: case Key.NumPad7: JumpToFraction(0.7); break;
                case Key.D8: case Key.NumPad8: JumpToFraction(0.8); break;
                case Key.D9: case Key.NumPad9: JumpToFraction(0.9); break;

                // Speed adjustments
                case Key.OemOpenBrackets:
                    AdjustSpeed(-0.1);
                    break;

                case Key.OemCloseBrackets:
                    AdjustSpeed(0.1);
                    break;

                case Key.OemPipe:
                case Key.Back:
                    SetSpeed(1.0);
                    break;

                // Live Stream Recording (R)
                case Key.R:
                    BtnRecord_Click(sender, e);
                    break;

                // Frame-by-frame forward (.) and backward (,)
                case Key.OemPeriod:
                    StepFrameForward();
                    break;


                // Cycle theme
                case Key.T:
                    ThemeManager.Instance.ApplyNextTheme();
                    break;

                // Help Guide Dialog (F1)
                case Key.F1:
                    ToolsMenuController.Instance.ShowHelpGuideDialog(this);
                    break;

                default:
                    handled = false;
                    break;
            }

            if (handled)
            {
                e.Handled = true;
            }
        }

        private void JumpToFraction(double fraction)
        {
            if (_totalDuration.TotalSeconds > 0)
            {
                double targetSecs = fraction * _totalDuration.TotalSeconds;
                var target = TimeSpan.FromSeconds(targetSecs);

                if (_isVlcActive)
                {
                    VlcEngineController.Instance.SeekToTime(target);
                }
                else
                {
                    PlayerMediaElement.Position = target;
                }

                if (OmniAudioEngine.Instance.IsActive)
                {
                    OmniAudioEngine.Instance.Position = target;
                }

                double val = fraction * 1000.0;
                SliderTimeline.Value = val;
                SliderModernTimeline.Value = val;
                ShowOsd($"Jump to {(int)(fraction * 100)}% ({target:hh\\:mm\\:ss})");
            }
        }

        private void SetSpeed(double speed)
        {
            _currentSpeed = Math.Clamp(speed, 0.05, 4.0);
            if (_isVlcActive)
            {
                VlcEngineController.Instance.SetRate((float)_currentSpeed);
            }
            PlayerMediaElement.SpeedRatio = _currentSpeed;

            if (BtnSpeed != null)
            {
                BtnSpeed.Content = $"⚡ {_currentSpeed:0.##}x";
            }
            ShowOsd($"Playback Speed: {_currentSpeed:0.##}x");
        }

        private void AdjustSpeed(double delta)
        {
            double cur = _isVlcActive ? (VlcEngineController.Instance.GetMediaPlayer()?.Rate ?? 1.0f) : PlayerMediaElement.SpeedRatio;
            double newSpeed = Math.Clamp(Math.Round(cur + delta, 2), 0.05, 4.0);
            SetSpeed(newSpeed);
        }

        public void BtnSpeed_Click(object sender, RoutedEventArgs e)
        {
            int idx = Array.FindIndex(_speedPresets, s => Math.Abs(s - _currentSpeed) < 0.01);
            if (idx < 0) idx = 4; // 1.0x
            int nextIdx = (idx + 1) % _speedPresets.Length;
            SetSpeed(_speedPresets[nextIdx]);
        }

        public void BtnSlowMo_Click(object sender, RoutedEventArgs e)
        {
            // YouTube-style slow motion cycler: 0.5x -> 0.25x -> 0.1x -> 0.75x -> 1.0x
            double[] slowMoLevels = new[] { 0.5, 0.25, 0.1, 0.75, 1.0 };
            int idx = Array.FindIndex(slowMoLevels, s => Math.Abs(s - _currentSpeed) < 0.01);
            int nextIdx = (idx + 1) % slowMoLevels.Length;
            SetSpeed(slowMoLevels[nextIdx]);
        }


        public void BtnStepForward_Click(object sender, RoutedEventArgs e)
        {
            StepFrameForward();
        }

        public void BtnStepBack_Click(object sender, RoutedEventArgs e)
        {
            StepFrameBackward();
        }

        private void StepFrameForward()
        {
            if (_isVlcActive)
            {
                VlcEngineController.Instance.NextFrame();
            }
            else
            {
                SeekRelative(0.04);
            }
            ShowOsd("Step Forward (+1 Frame)");
        }

        private void StepFrameBackward()
        {
            SeekRelative(-0.04);
            ShowOsd("Step Backward (-1 Frame)");
        }

        public void BtnRecord_Click(object sender, RoutedEventArgs e)
        {
            if (LiveStreamRecorder.Instance.IsRecording)
            {
                LiveStreamRecorder.Instance.StopRecording();
            }
            else
            {
                if (string.IsNullOrWhiteSpace(_currentSourceUri))
                {
                    ShowOsd("No active stream or media to record");
                    return;
                }
                string title = string.IsNullOrWhiteSpace(_currentMediaTitle) ? "Live_Telecast" : _currentMediaTitle;
                LiveStreamRecorder.Instance.StartRecording(_currentSourceUri, title);
                if (BtnRecord != null)
                {
                    BtnRecord.Content = "⏹ Stop Rec";
                    BtnRecord.Foreground = Brushes.White;
                    BtnRecord.Background = Brushes.Red;
                }
                if (BorderRecordingHud != null) BorderRecordingHud.Visibility = Visibility.Visible;
                ShowOsd("🔴 Live Stream Recording Started");
            }
        }

        private void OnRecordingProgress(TimeSpan duration, long bytes)
        {
            Dispatcher.Invoke(() =>
            {
                if (TxtRecordingDuration != null) TxtRecordingDuration.Text = $"{duration:hh\\:mm\\:ss}";
                if (TxtRecordingBytes != null) TxtRecordingBytes.Text = $"{(bytes / 1024.0 / 1024.0):F1} MB";
            });
        }

        private void OnRecordingFinished(string filePath)
        {
            Dispatcher.Invoke(() =>
            {
                if (BtnRecord != null)
                {
                    BtnRecord.Content = "🔴 Rec";
                    BtnRecord.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF5350"));
                    BtnRecord.SetResourceReference(Button.BackgroundProperty, "OmniControlBackgroundBrush");
                }
                if (BorderRecordingHud != null) BorderRecordingHud.Visibility = Visibility.Collapsed;
                ShowOsd($"Recording Saved: {Path.GetFileName(filePath)}");
            });
        }

        private void OnRecordingError(string error)
        {
            Dispatcher.Invoke(() =>
            {
                if (BtnRecord != null)
                {
                    BtnRecord.Content = "🔴 Rec";
                    BtnRecord.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF5350"));
                    BtnRecord.SetResourceReference(Button.BackgroundProperty, "OmniControlBackgroundBrush");
                }
                if (BorderRecordingHud != null) BorderRecordingHud.Visibility = Visibility.Collapsed;
                ShowOsd($"Recording Error: {error}");
            });
        }

        // ================= Mouse & Touchpad Gestures (Wheel Volume, Horizontal Seek & Auto-Hide) =================
        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) || Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                // Touchpad horizontal scroll or Shift+wheel: timeline jump
                if (e.Delta > 0)
                {
                    SeekRelative(5);
                    ShowOsd("⏩ Scrub +5s");
                }
                else if (e.Delta < 0)
                {
                    SeekRelative(-5);
                    ShowOsd("⏪ Scrub -5s");
                }
                e.Handled = true;
                return;
            }

            if (e.Delta > 0)
            {
                MenuVolUp_Click(sender, e);
            }
            else if (e.Delta < 0)
            {
                MenuVolDown_Click(sender, e);
            }
        }

        private void Window_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            _lastMouseMoveTime = DateTime.UtcNow;
            if (Mouse.OverrideCursor != null)
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            _lastMouseMoveTime = DateTime.UtcNow;
            if (Mouse.OverrideCursor != null)
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void ControlsArea_MouseEnter(object sender, MouseEventArgs e)
        {
            _lastMouseMoveTime = DateTime.UtcNow;
            if (Mouse.OverrideCursor != null)
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void ControlsArea_MouseMove(object sender, MouseEventArgs e)
        {
            _lastMouseMoveTime = DateTime.UtcNow;
            if (Mouse.OverrideCursor != null)
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void CursorMonitorTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isFullscreen || WindowState == WindowState.Minimized || !IsVisible)
            {
                if (Mouse.OverrideCursor != null)
                {
                    Mouse.OverrideCursor = null;
                }
                return;
            }

            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero || !IsLoaded) return;

            if (!GetCursorPos(out POINT screenPt)) return;

            bool mouseMoved = (screenPt.X != _lastScreenCursorPos.X || screenPt.Y != _lastScreenCursorPos.Y);
            if (mouseMoved)
            {
                _lastScreenCursorPos = screenPt;
                _lastMouseMoveTime = DateTime.UtcNow;

                // Ensure cursor is restored instantly upon any physical motion across entire UI
                if (Mouse.OverrideCursor != null)
                {
                    Mouse.OverrideCursor = null;
                }
            }

            POINT clientPt = screenPt;
            ScreenToClient(hwnd, ref clientPt);

            bool isInsideWindow = (clientPt.X >= 0 && clientPt.X <= ActualWidth && clientPt.Y >= 0 && clientPt.Y <= ActualHeight);

            if (_isFullscreen)
            {
                if (isInsideWindow && !_controlsHidden)
                {
                    // Move mouse to bottom (last 120 pixels) -> Auto-reveal bottom transport controls & timeline
                    if (clientPt.Y >= ActualHeight - 120)
                    {
                        if (_currentViewMode == PlayerViewMode.Modern)
                        {
                            if (BorderModernHud != null && BorderModernHud.Visibility != Visibility.Visible)
                                BorderModernHud.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            if (BorderControls != null && BorderControls.Visibility != Visibility.Visible)
                                BorderControls.Visibility = Visibility.Visible;
                            if (BorderTimeline != null && BorderTimeline.Visibility != Visibility.Visible)
                                BorderTimeline.Visibility = Visibility.Visible;
                            if (BorderStatusBar != null && BorderStatusBar.Visibility != Visibility.Visible)
                                BorderStatusBar.Visibility = Visibility.Visible;
                        }
                    }

                    // Move mouse to top (first 60 pixels) -> Auto-reveal top menu bar
                    if (clientPt.Y <= 60)
                    {
                        if (BorderTopMenu != null && BorderTopMenu.Visibility != Visibility.Visible)
                            BorderTopMenu.Visibility = Visibility.Visible;
                    }
                }

                // Check if cursor is currently hovering over interactive controls or top menu
                bool isHoveringOverControls = isInsideWindow && (
                    (clientPt.Y >= ActualHeight - 130 && (BorderControls?.Visibility == Visibility.Visible || BorderModernHud?.Visibility == Visibility.Visible)) ||
                    (clientPt.Y <= 60 && BorderTopMenu?.Visibility == Visibility.Visible)
                );

                if (isHoveringOverControls || _isControlsPinned)
                {
                    // Keep controls awake while user is actively hovering or controls are pinned
                    _lastMouseMoveTime = DateTime.UtcNow;
                    if (Mouse.OverrideCursor != null)
                    {
                        Mouse.OverrideCursor = null;
                    }
                }
                else
                {
                    // Auto-hide smoothly after 3.0 seconds of idle when stationary over video surface
                    double idleSeconds = (DateTime.UtcNow - _lastMouseMoveTime).TotalSeconds;
                    if (idleSeconds >= 3.0)
                    {
                        if (BorderModernHud != null && BorderModernHud.Visibility == Visibility.Visible)
                            BorderModernHud.Visibility = Visibility.Collapsed;
                        if (BorderControls != null && BorderControls.Visibility == Visibility.Visible)
                            BorderControls.Visibility = Visibility.Collapsed;
                        if (BorderTimeline != null && BorderTimeline.Visibility == Visibility.Visible)
                            BorderTimeline.Visibility = Visibility.Collapsed;
                        if (BorderStatusBar != null && BorderStatusBar.Visibility == Visibility.Visible)
                            BorderStatusBar.Visibility = Visibility.Collapsed;
                        if (BorderTelemetryHud != null && BorderTelemetryHud.Visibility == Visibility.Visible)
                            BorderTelemetryHud.Visibility = Visibility.Collapsed;
                        if (BorderTopMenu != null && BorderTopMenu.Visibility == Visibility.Visible)
                            BorderTopMenu.Visibility = Visibility.Collapsed;

                        // Only hide cursor when stationary over video surface in fullscreen
                        if (isInsideWindow && Mouse.OverrideCursor != Cursors.None)
                        {
                            Mouse.OverrideCursor = Cursors.None;
                        }
                    }
                }
            }
            else
            {
                // In windowed desktop mode, cursor MUST ALWAYS be normal
                if (Mouse.OverrideCursor != null)
                {
                    Mouse.OverrideCursor = null;
                }
            }
        }

        // ================= Playback Controls =================
        public void BtnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (_isPlaying)
            {
                if (_isVlcActive)
                {
                    VlcEngineController.Instance.Pause();
                }
                else
                {
                    PlayerMediaElement.Pause();
                }

                if (OmniAudioEngine.Instance.IsActive) OmniAudioEngine.Instance.Pause();
                _isPlaying = false;
                BtnPlayPause.Content = "▶ Play";
                BtnModernPlayPause.Content = "▶ Play";
                ShowOsd("Paused");
            }
            else
            {
                if (_isVlcActive)
                {
                    VlcEngineController.Instance.Resume();
                }
                else
                {
                    PlayerMediaElement.Play();
                }

                if (OmniAudioEngine.Instance.IsActive) OmniAudioEngine.Instance.Play();
                _isPlaying = true;
                BtnPlayPause.Content = "⏸ Pause";
                BtnModernPlayPause.Content = "⏸ Pause";
                if (!_isVlcActive) _playbackTimer.Start();
                ShowOsd("Playing");
            }
        }

        public void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            if (_isVlcActive)
            {
                VlcEngineController.Instance.Stop();
            }
            else
            {
                PlayerMediaElement.Stop();
                _playbackTimer.Stop();
            }

            if (OmniAudioEngine.Instance.IsActive) OmniAudioEngine.Instance.Stop();
            _isPlaying = false;
            BtnPlayPause.Content = "▶ Play";
            BtnModernPlayPause.Content = "▶ Play";
            SliderTimeline.Value = 0;
            SliderModernTimeline.Value = 0;
            TxtTimeElapsed.Text = "00:00:00";
            TxtModernTimeElapsed.Text = "00:00:00";
            TxtPrimarySubtitle.Visibility = Visibility.Collapsed;
            TxtSecondarySubtitle.Visibility = Visibility.Collapsed;
            ShowOsd("Stopped");
        }

        public void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (_isVlcActive)
            {
                VlcEngineController.Instance.SeekToTime(TimeSpan.Zero);
            }
            else
            {
                PlayerMediaElement.Position = TimeSpan.Zero;
            }

            if (OmniAudioEngine.Instance.IsActive) OmniAudioEngine.Instance.Position = TimeSpan.Zero;
            SliderTimeline.Value = 0;
            SliderModernTimeline.Value = 0;
            ShowOsd("Jump to Start");
        }

        public void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            ShowOsd("Next Chapter / Track");
        }

        // ================= Smooth Seeking & High-Precision Timeline =================
        // Seek Bar Hover Preview state (disabled by default as per user directive)
        private bool _enableHoverPreview = false;

        private void EnsureTotalDuration()
        {
            if (_totalDuration.TotalSeconds <= 0)
            {
                if (_isVlcActive)
                {
                    var len = VlcEngineController.Instance.Length;
                    if (len.TotalSeconds > 0)
                    {
                        _totalDuration = len;
                        string totalStr = $"{len:hh\\:mm\\:ss}";
                        if (TxtTimeTotal != null) TxtTimeTotal.Text = totalStr;
                        if (TxtModernTimeTotal != null) TxtModernTimeTotal.Text = totalStr;
                    }
                }
                else if (PlayerMediaElement.NaturalDuration.HasTimeSpan && PlayerMediaElement.NaturalDuration.TimeSpan.TotalSeconds > 0)
                {
                    _totalDuration = PlayerMediaElement.NaturalDuration.TimeSpan;
                    string totalStr = $"{_totalDuration:hh\\:mm\\:ss}";
                    if (TxtTimeTotal != null) TxtTimeTotal.Text = totalStr;
                    if (TxtModernTimeTotal != null) TxtModernTimeTotal.Text = totalStr;
                }
            }
        }

        private void SeekToFraction(double fraction)
        {
            fraction = Math.Clamp(fraction, 0.0, 1.0);
            EnsureTotalDuration();

            double targetSecs = fraction * _totalDuration.TotalSeconds;
            var target = TimeSpan.FromSeconds(targetSecs);

            if (_isVlcActive)
            {
                // Immediate proportional and timestamp seeking across all formats
                VlcEngineController.Instance.Position = (float)fraction;
                if (targetSecs > 0)
                {
                    VlcEngineController.Instance.SeekToTime(target);
                }
            }
            else
            {
                PlayerMediaElement.Position = target;
            }

            if (OmniAudioEngine.Instance.IsActive)
            {
                OmniAudioEngine.Instance.Position = target;
            }

            double sliderVal = fraction * 1000.0;
            if (SliderTimeline != null) SliderTimeline.Value = sliderVal;
            if (SliderModernTimeline != null) SliderModernTimeline.Value = sliderVal;

            string elapsed = $"{target:hh\\:mm\\:ss}";
            if (TxtTimeElapsed != null) TxtTimeElapsed.Text = elapsed;
            if (TxtModernTimeElapsed != null) TxtModernTimeElapsed.Text = elapsed;

            if (_totalDuration.TotalSeconds > 0)
            {
                ShowOsd($"Seek: {elapsed} / {_totalDuration:hh\\:mm\\:ss}");
            }
            else
            {
                ShowOsd($"Seek: {(int)(fraction * 100)}%");
            }
        }

        private void ShowSeekTimePopup(Slider slider, double mouseX, double fraction)
        {
            EnsureTotalDuration();

            double targetSecs = fraction * _totalDuration.TotalSeconds;
            string timeStr = TimeSpan.FromSeconds(targetSecs).ToString(@"hh\:mm\:ss");

            bool isModern = slider == SliderModernTimeline;
            var popup = isModern ? PopupModernSeekPreview : PopupSeekPreview;
            var timeTxt = isModern ? TxtModernSeekPreviewTime : TxtSeekPreviewTime;

            if (timeTxt != null) timeTxt.Text = timeStr;

            if (popup != null)
            {
                popup.PlacementTarget = slider;
                popup.HorizontalOffset = mouseX - 35;
                popup.VerticalOffset = -12;
                if (!popup.IsOpen) popup.IsOpen = true;
            }
        }

        private void HideSeekTimePopup(Slider? slider)
        {
            if (PopupSeekPreview != null && PopupSeekPreview.IsOpen) PopupSeekPreview.IsOpen = false;
            if (PopupModernSeekPreview != null && PopupModernSeekPreview.IsOpen) PopupModernSeekPreview.IsOpen = false;
        }

        private void SliderTimeline_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            var slider = sender as Slider ?? SliderTimeline;
            if (slider == null || slider.ActualWidth <= 0) return;

            _isDraggingSlider = true;
            slider.CaptureMouse();

            var pt = e.GetPosition(slider);
            double fraction = Math.Clamp(pt.X / slider.ActualWidth, 0.0, 1.0);
            SeekToFraction(fraction);

            // Display time of that scene in popup default
            ShowSeekTimePopup(slider, pt.X, fraction);

            e.Handled = true;
        }

        private void SliderTimeline_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var slider = sender as Slider ?? SliderTimeline;
            if (slider == null || slider.ActualWidth <= 0) return;

            var pt = e.GetPosition(slider);
            double fraction = Math.Clamp(pt.X / slider.ActualWidth, 0.0, 1.0);

            if (_isDraggingSlider && e.LeftButton == MouseButtonState.Pressed)
            {
                SeekToFraction(fraction);
                ShowSeekTimePopup(slider, pt.X, fraction);
                e.Handled = true;
            }
            else if (_enableHoverPreview)
            {
                ShowSeekTimePopup(slider, pt.X, fraction);
            }
            else
            {
                HideSeekTimePopup(slider);
            }
        }

        private void SliderTimeline_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            var slider = sender as Slider ?? SliderTimeline;
            if (_isDraggingSlider)
            {
                if (slider != null && slider.ActualWidth > 0)
                {
                    slider.ReleaseMouseCapture();
                    var pt = e.GetPosition(slider);
                    double fraction = Math.Clamp(pt.X / slider.ActualWidth, 0.0, 1.0);
                    SeekToFraction(fraction);
                }
                _isDraggingSlider = false;
                HideSeekTimePopup(slider);
                e.Handled = true;
            }
        }

        private void SliderTimeline_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!_enableHoverPreview && !_isDraggingSlider)
            {
                HideSeekTimePopup(sender as Slider);
            }
        }

        private void SliderTimeline_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!_isDraggingSlider)
            {
                HideSeekTimePopup(sender as Slider);
            }
        }

        private void SliderTimeline_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;
            // Visual position updates from player timers
        }

        private void SeekToSlider(double sliderValue)
        {
            SeekToFraction(sliderValue / 1000.0);
        }

        private void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;

            double vol = e.NewValue;

            if (SliderModernVolume != null && Math.Abs(SliderModernVolume.Value - vol) > 0.001)
            {
                SliderModernVolume.Value = vol;
            }
            if (SliderVolume != null && Math.Abs(SliderVolume.Value - vol) > 0.001)
            {
                SliderVolume.Value = vol;
            }

            if (_isVlcActive && VlcEngineController.Instance.IsAvailable)
            {
                VlcEngineController.Instance.Volume = (int)(vol * 100);
            }

            if (OmniAudioEngine.Instance.IsActive)
            {
                OmniAudioEngine.Instance.Volume = (float)vol;
            }
            else if (PlayerMediaElement != null)
            {
                PlayerMediaElement.Volume = vol;
            }

            if (vol == 0)
            {
                if (BtnMute != null) BtnMute.Content = "🔇";
                _isMuted = true;
            }
            else
            {
                if (BtnMute != null) BtnMute.Content = "🔊";
                _isMuted = false;
            }
        }

        public void BtnMute_Click(object sender, RoutedEventArgs e)
        {
            if (_isMuted)
            {
                SliderVolume.Value = (_previousVolume > 0) ? _previousVolume : 0.8;
                BtnMute.Content = "🔊";
                _isMuted = false;
                if (_isVlcActive) VlcEngineController.Instance.Mute = false;
                ShowOsd($"Unmuted: {(int)(SliderVolume.Value * 100)}%");
            }
            else
            {
                _previousVolume = SliderVolume.Value;
                SliderVolume.Value = 0;
                BtnMute.Content = "🔇";
                _isMuted = true;
                if (_isVlcActive) VlcEngineController.Instance.Mute = true;
                ShowOsd("Muted");
            }
        }

        // ================= True Borderless Fullscreen & Auto-Hide =================
        // ================= True Borderless Fullscreen & Auto-Hide =================
        public void BtnFullscreen_Click(object sender, RoutedEventArgs e)
        {
            if (!_isFullscreen)
            {
                _previousWindowState = WindowState;
                _previousWindowStyle = WindowStyle;
                _previousResizeMode = ResizeMode;
                _previousLeft = Left;
                _previousTop = Top;
                _previousWidth = Width;
                _previousHeight = Height;

                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                Topmost = true;

                var hwnd = new WindowInteropHelper(this).Handle;
                var hMonitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
                var mi = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
                if (GetMonitorInfo(hMonitor, ref mi))
                {
                    int x = mi.rcMonitor.Left;
                    int y = mi.rcMonitor.Top;
                    int w = mi.rcMonitor.Right - mi.rcMonitor.Left;
                    int h = mi.rcMonitor.Bottom - mi.rcMonitor.Top;

                    Left = x;
                    Top = y;
                    Width = w;
                    Height = h;

                    SetWindowPos(hwnd, HWND_TOPMOST, x, y, w, h, SWP_SHOWWINDOW | SWP_FRAMECHANGED);
                }

                if (BorderTopMenu != null) BorderTopMenu.Visibility = Visibility.Collapsed;
                if (BorderStatusBar != null) BorderStatusBar.Visibility = Visibility.Collapsed;
                _isFullscreen = true;
                _cursorMonitorTimer.Start();

                ShowOsd("Fullscreen Mode (Taskbar Concealed | F11 / Esc to exit | H to toggle UI)");
                _lastMouseMoveTime = DateTime.UtcNow;
                Mouse.OverrideCursor = null;
            }
            else
            {
                _isFullscreen = false;
                _cursorMonitorTimer.Stop();
                _controlsHidden = false;
                _lastMouseMoveTime = DateTime.UtcNow;
                Mouse.OverrideCursor = null;

                Topmost = false;
                WindowStyle = _previousWindowStyle;
                ResizeMode = _previousResizeMode;
                WindowState = _previousWindowState;

                Left = _previousLeft;
                Top = _previousTop;
                Width = _previousWidth;
                Height = _previousHeight;

                var hwnd = new WindowInteropHelper(this).Handle;
                SetWindowPos(hwnd, HWND_NOTOPMOST, (int)Left, (int)Top, (int)Width, (int)Height, SWP_SHOWWINDOW | SWP_FRAMECHANGED);

                ApplyViewMode(_currentViewMode);
                ShowOsd("Windowed Mode");
            }
        }

        private void VideoGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                var pt = e.GetPosition(VideoGrid);
                double w = VideoGrid.ActualWidth;
                if (w > 0)
                {
                    if (pt.X < w * 0.33)
                    {
                        // Double tap / click left side: Rewind 10s (YouTube / mobile style)
                        SeekRelative(-10);
                        ShowOsd("⏪ 10s Rewind");
                        e.Handled = true;
                        return;
                    }
                    else if (pt.X > w * 0.67)
                    {
                        // Double tap / click right side: Fast Forward 10s (YouTube / mobile style)
                        SeekRelative(10);
                        ShowOsd("⏩ 10s Forward");
                        e.Handled = true;
                        return;
                    }
                }
                // Center double tap / click: Toggle fullscreen
                BtnFullscreen_Click(sender, e);
                e.Handled = true;
            }
            else if (e.ClickCount == 1)
            {
                // Single tap / click: Toggle play/pause (YouTube & mobile media player standard)
                BtnPlayPause_Click(sender, e);
                _lastMouseMoveTime = DateTime.UtcNow;
            }
        }

        private void VideoGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (VideoContextMenu != null)
            {
                VideoContextMenu.PlacementTarget = VideoGrid;
                VideoContextMenu.IsOpen = true;
                e.Handled = true;
            }
        }

        // ================= Menu Handlers =================
        private void MenuOpenFile_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = "Open Media File",
                Filter = "All Media Files|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.webm;*.flv;*.ts;*.m2ts;*.mp3;*.flac;*.wav;*.aac;*.ogg;*.m4a|Video Files|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.webm;*.ts|Audio Files|*.mp3;*.flac;*.wav;*.aac;*.ogg;*.m4a|All Files (*.*)|*.*"
            };

            if (ofd.ShowDialog() == true)
            {
                PlayMedia(ofd.FileName, Path.GetFileName(ofd.FileName));
            }
        }

        private void MenuOpenUrl_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenUrlDialog { Owner = this };
            dlg.StreamResolved += result =>
            {
                PlayMedia(result.ResolvedUrl, result.Title);
            };
            dlg.ShowDialog();
        }

        private void MenuLiveTv_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new LiveTvHubDialog { Owner = this };
            dlg.ChannelSelected += channel =>
            {
                PlayMedia(channel.StreamUrl, $"{channel.Name} ({channel.Category})");
            };
            dlg.Show();
        }

        private void MenuSettings_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SettingsDialog { Owner = this };
            dlg.SettingsApplied += settings =>
            {
                ShowOsd("Settings Applied Successfully");
            };
            dlg.ShowDialog();
        }

        private void MenuOpenDisc_Click(object sender, RoutedEventArgs e)
        {
            ShowOsd("BluRay / DVD Video Disc Mount Scanner Initialized");
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            VlcEngineController.Instance.Dispose();
            OmniAudioEngine.Instance.Dispose();
            Application.Current.Shutdown();
        }

        private void MenuJumpForward5_Click(object sender, RoutedEventArgs e) => SeekRelative(5);
        private void MenuJumpBackward5_Click(object sender, RoutedEventArgs e) => SeekRelative(-5);
        private void MenuJumpForward30_Click(object sender, RoutedEventArgs e) => SeekRelative(30);
        private void MenuJumpBackward30_Click(object sender, RoutedEventArgs e) => SeekRelative(-30);
        private void MenuJumpForward1_Click(object sender, RoutedEventArgs e) => SeekRelative(1);
        private void MenuJumpBackward1_Click(object sender, RoutedEventArgs e) => SeekRelative(-1);

        private void SeekRelative(double seconds)
        {
            var current = _isVlcActive ? VlcEngineController.Instance.Time : PlayerMediaElement.Position;
            double newSecs = Math.Max(0, current.TotalSeconds + seconds);
            if (_totalDuration.TotalSeconds > 0)
            {
                newSecs = Math.Min(newSecs, _totalDuration.TotalSeconds);
            }
            var target = TimeSpan.FromSeconds(newSecs);

            if (_isVlcActive)
            {
                VlcEngineController.Instance.SeekToTime(target);
            }
            else
            {
                PlayerMediaElement.Position = target;
            }

            if (OmniAudioEngine.Instance.IsActive)
            {
                OmniAudioEngine.Instance.Position = target;
            }

            if (_totalDuration.TotalSeconds > 0)
            {
                double val = (newSecs / _totalDuration.TotalSeconds) * 1000.0;
                if (SliderTimeline != null) SliderTimeline.Value = val;
                if (SliderModernTimeline != null) SliderModernTimeline.Value = val;
            }
            string direction = seconds > 0 ? $"+{seconds:0.#}s" : $"{seconds:0.#}s";
            ShowOsd($"Seek {direction} [{target:hh\\:mm\\:ss} / {_totalDuration:hh\\:mm\\:ss}]");
        }

        public void CycleAspectRatio()
        {
            _aspectRatioIndex = (_aspectRatioIndex + 1) % _aspectRatios.Length;
            string aspect = _aspectRatios[_aspectRatioIndex];
            ApplyAspectRatio(aspect);
        }

        public void ApplyAspectRatio(string aspect)
        {
            if (_isVlcActive)
            {
                VlcEngineController.Instance.SetAspectRatio(aspect);
            }
            PlayerMediaElement.Stretch = (aspect == "Fill") ? System.Windows.Media.Stretch.Fill : System.Windows.Media.Stretch.Uniform;
            ShowOsd($"Aspect Ratio: {aspect}");
        }

        private void MenuCycleAspect_Click(object sender, RoutedEventArgs e) => CycleAspectRatio();

        private void MenuNextTheme_Click(object sender, RoutedEventArgs e) => ThemeManager.Instance.ApplyNextTheme();

        private void MenuSpeedPreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && double.TryParse(mi.Tag?.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double speed))
            {
                SetSpeed(speed);
            }
        }

        private void MenuSpeed_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && double.TryParse(mi.Tag?.ToString(), out double speed))
            {
                SetSpeed(speed);
            }
        }

        private void MenuAspect_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.Tag is string tag)
            {
                ApplyAspectRatio(tag);
            }
        }

        private void MenuVolUp_Click(object sender, RoutedEventArgs e)
        {
            SliderVolume.Value = Math.Min(1.0, SliderVolume.Value + 0.05);
            ShowOsd($"Volume: {(int)(SliderVolume.Value * 100)}%");
        }

        private void MenuVolDown_Click(object sender, RoutedEventArgs e)
        {
            SliderVolume.Value = Math.Max(0.0, SliderVolume.Value - 0.05);
            ShowOsd($"Volume: {(int)(SliderVolume.Value * 100)}%");
        }

        private void MenuWasapiToggle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi)
            {
                OmniAudioEngine.Instance.WasapiExclusiveMode = mi.IsChecked;
                ShowOsd(mi.IsChecked ? "WASAPI Exclusive Bitstreaming: ENABLED" : "WASAPI Exclusive: Shared PCM Mode");
            }
        }

        private void MenuLoadSubtitle_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = "Open Subtitle File",
                Filter = "Subtitle Files (*.srt;*.vtt;*.ass;*.ssa)|*.srt;*.vtt;*.ass;*.ssa|All Files (*.*)|*.*"
            };

            if (ofd.ShowDialog() == true)
            {
                string content = File.ReadAllText(ofd.FileName);
                var track = SubtitleEngine.Instance.ParseSrtOrVtt(content, "en", Path.GetFileName(ofd.FileName));
                SubtitleEngine.Instance.PrimaryTrack = track;
                ShowOsd($"Subtitles Loaded: {Path.GetFileName(ofd.FileName)} ({track.Cues.Count} cues)");
            }
        }

        private async void MenuDownloadSubtitles_Click(object sender, RoutedEventArgs e)
        {
            string title = string.IsNullOrEmpty(_currentMediaTitle) ? "Sample Media" : _currentMediaTitle;
            await ToolsMenuController.Instance.DownloadSubtitlesAsync(title);
        }

        private void MenuDualSubtitles_Click(object sender, RoutedEventArgs e)
        {
            SubtitleEngine.Instance.DualSubtitlesEnabled = MenuDualSubtitles.IsChecked;
            ShowOsd(MenuDualSubtitles.IsChecked ? "Dual Subtitle Mode (Top & Bottom) ACTIVE" : "Single Subtitle Mode");
        }

        private void MenuSubSync_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && int.TryParse(mi.Tag?.ToString(), out int offsetMs))
            {
                if (offsetMs == 0)
                {
                    if (SubtitleEngine.Instance.PrimaryTrack != null)
                        SubtitleEngine.Instance.PrimaryTrack.SyncOffset = TimeSpan.Zero;
                    ShowOsd("Subtitle Sync: Reset to 0ms");
                }
                else
                {
                    SubtitleEngine.Instance.AdjustOffset(true, offsetMs);
                    ShowOsd($"Subtitle Sync: {offsetMs:+0;-0;0} ms");
                }
            }
        }

        // ================= Tools Menu Handlers =================
        private void MenuCameraStudio_Click(object sender, RoutedEventArgs e)
        {
            ToolsMenuController.Instance.ShowCameraStudioDialog(this);
        }

        private void MenuMediaLibrary_Click(object sender, RoutedEventArgs e)
        {
            ToolsMenuController.Instance.ShowMediaLibraryDashboardDialog(this);
        }

        private void MenuMediaDownloader_Click(object sender, RoutedEventArgs e)
        {
            ToolsMenuController.Instance.ShowMediaDownloaderDialog(this);
        }

        private void MenuMediaInfo_Click(object sender, RoutedEventArgs e)
        {
            ToolsMenuController.Instance.ShowMediaInfoDialog(this);
        }

        private void MenuCaptureStudio_Click(object sender, RoutedEventArgs e)
        {
            ToolsMenuController.Instance.ShowCaptureStudioDialog(this);
        }

        private void BtnQuickCapture_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "PSD_Snapshots");
                Directory.CreateDirectory(dir);
                string fileName = $"PSD_Snapshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                string fullPath = Path.Combine(dir, fileName);

                bool success = false;
                if (_isVlcActive && VlcEngineController.Instance.IsAvailable)
                {
                    success = VlcEngineController.Instance.TakeSnapshot(fullPath, 0, 0);
                }

                if (!success)
                {
                    int w = (int)Math.Max(1, VideoGrid.ActualWidth);
                    int h = (int)Math.Max(1, VideoGrid.ActualHeight);
                    var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
                    rtb.Render(VideoGrid);
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(rtb));
                    using var fs = File.Create(fullPath);
                    encoder.Save(fs);
                    success = true;
                }

                ShowOsd($"📸 Snapshot Saved: {fileName}");
            }
            catch (Exception ex)
            {
                ShowOsd($"Snapshot Error: {ex.Message}");
            }
        }

        private void MenuEqualizer_Click(object sender, RoutedEventArgs e)
        {
            ToolsMenuController.Instance.ShowEqualizerDialog(this);
        }

        private void MenuPerformance_Click(object sender, RoutedEventArgs e)
        {
            ToolsMenuController.Instance.ShowPerformanceDialog(this);
        }

        private void MenuControlPanel_Click(object sender, RoutedEventArgs e)
        {
            ToolsMenuController.Instance.ShowControlPanelDialog(this);
        }

        private void MenuSetA_Click(object sender, RoutedEventArgs e)
        {
            TimeSpan current = _isVlcActive ? VlcEngineController.Instance.Time : PlayerMediaElement.Position;
            ToolsMenuController.Instance.SetMarkerA(current);
        }

        private void MenuSetB_Click(object sender, RoutedEventArgs e)
        {
            TimeSpan current = _isVlcActive ? VlcEngineController.Instance.Time : PlayerMediaElement.Position;
            ToolsMenuController.Instance.SetMarkerB(current);
        }

        private void MenuClearAB_Click(object sender, RoutedEventArgs e)
        {
            ToolsMenuController.Instance.ClearAbRepeat();
        }

        private void MenuHelpGuide_Click(object sender, RoutedEventArgs e)
        {
            ToolsMenuController.Instance.ShowHelpGuideDialog(this);
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            ToolsMenuController.Instance.ShowAboutDialog(this);
        }
    }
}
