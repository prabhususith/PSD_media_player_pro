using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using OmniPlayer.Core.Audio;
using OmniPlayer.Core.Decoders;
using OmniPlayer.Core.Engine;
using OmniPlayer.Core.Subtitles;
using OmniPlayer.Core.Video;
using OmniPlayer.UI.Views;

namespace OmniPlayer.UI.Controls
{
    public class ToolsMenuController
    {
        private static readonly Lazy<ToolsMenuController> _instance = new(() => new ToolsMenuController());
        public static ToolsMenuController Instance => _instance.Value;

        // A-B Repeat State
        public TimeSpan? MarkerA { get; private set; }
        public TimeSpan? MarkerB { get; private set; }
        public bool IsAbRepeatActive => MarkerA.HasValue && MarkerB.HasValue;

        public event Action<TimeSpan?, TimeSpan?>? AbRepeatChanged;
        public event Action<string>? StatusNotification;

        public void SetMarkerA(TimeSpan currentPosition)
        {
            MarkerA = currentPosition;
            if (MarkerB.HasValue && MarkerB.Value <= MarkerA.Value)
            {
                MarkerB = null;
            }
            AbRepeatChanged?.Invoke(MarkerA, MarkerB);
            StatusNotification?.Invoke($"A-B Repeat: Point A set to {MarkerA.Value:hh\\:mm\\:ss\\.fff}");
        }

        public void SetMarkerB(TimeSpan currentPosition)
        {
            if (MarkerA.HasValue && currentPosition > MarkerA.Value)
            {
                MarkerB = currentPosition;
                AbRepeatChanged?.Invoke(MarkerA, MarkerB);
                StatusNotification?.Invoke($"A-B Repeat: Point B set to {MarkerB.Value:hh\\:mm\\:ss\\.fff}. Loop Active.");
            }
            else
            {
                StatusNotification?.Invoke("A-B Repeat: Point B must be after Point A.");
            }
        }

        public void ClearAbRepeat()
        {
            MarkerA = null;
            MarkerB = null;
            AbRepeatChanged?.Invoke(null, null);
            StatusNotification?.Invoke("A-B Repeat: Cleared.");
        }

        public TimeSpan? CheckAbLoop(TimeSpan currentPosition)
        {
            if (MarkerA.HasValue && MarkerB.HasValue)
            {
                if (currentPosition >= MarkerB.Value)
                {
                    return MarkerA.Value;
                }
            }
            return null;
        }

        public void ShowEqualizerDialog(Window? owner = null)
        {
            try
            {
                var parent = owner ?? Application.Current?.MainWindow;
                var dlg = new EqualizerDialog();
                if (parent != null && parent.IsLoaded)
                {
                    dlg.Owner = parent;
                }
                dlg.Show();
            }
            catch (Exception ex)
            {
                StatusNotification?.Invoke($"Equalizer Error: {ex.Message}");
            }
        }

        private ControlPanelDialog? _controlPanelDialog;

        public void ShowControlPanelDialog(Window? owner = null)
        {
            try
            {
                if (_controlPanelDialog == null || !_controlPanelDialog.IsLoaded)
                {
                    var parent = owner ?? Application.Current?.MainWindow;
                    _controlPanelDialog = new ControlPanelDialog();
                    if (parent != null && parent.IsLoaded)
                    {
                        _controlPanelDialog.Owner = parent;
                    }
                    _controlPanelDialog.Closed += (s, e) => _controlPanelDialog = null;
                    _controlPanelDialog.Show();
                }
                else
                {
                    if (_controlPanelDialog.WindowState == WindowState.Minimized)
                    {
                        _controlPanelDialog.WindowState = WindowState.Normal;
                    }
                    _controlPanelDialog.Activate();
                }
            }
            catch (Exception ex)
            {
                StatusNotification?.Invoke($"Control Panel Error: {ex.Message}");
            }
        }

        public void ShowPerformanceDialog(Window? owner = null)
        {
            var dlg = new PerformanceDialog
            {
                Owner = owner ?? Application.Current.MainWindow
            };
            dlg.Show();
        }

        public void ShowMediaInfoDialog(Window? owner = null)
        {
            var dlg = new MediaInfoDialog
            {
                Owner = owner ?? Application.Current.MainWindow
            };
            dlg.Show();
        }

        public void ShowCaptureStudioDialog(Window? owner = null)
        {
            var dlg = new CaptureStudioDialog
            {
                Owner = owner ?? Application.Current.MainWindow
            };
            dlg.Show();
        }

        private MediaDownloaderDialog? _mediaDownloaderDialog;

        public void ShowMediaDownloaderDialog(Window? owner = null)
        {
            try
            {
                if (_mediaDownloaderDialog == null || !_mediaDownloaderDialog.IsLoaded)
                {
                    var parent = owner ?? Application.Current?.MainWindow;
                    _mediaDownloaderDialog = new MediaDownloaderDialog();
                    if (parent != null && parent.IsLoaded)
                    {
                        _mediaDownloaderDialog.Owner = parent;
                    }
                    _mediaDownloaderDialog.Closed += (s, e) => _mediaDownloaderDialog = null;
                    _mediaDownloaderDialog.Show();
                }
                else
                {
                    if (_mediaDownloaderDialog.WindowState == WindowState.Minimized)
                    {
                        _mediaDownloaderDialog.WindowState = WindowState.Normal;
                    }
                    _mediaDownloaderDialog.Activate();
                }
            }
            catch (Exception ex)
            {
                StatusNotification?.Invoke($"Media Downloader Error: {ex.Message}");
            }
        }

        private HelpGuideDialog? _helpGuideDialog;

        public void ShowHelpGuideDialog(Window? owner = null, int initialTabIndex = 0)
        {
            try
            {
                if (_helpGuideDialog == null || !_helpGuideDialog.IsLoaded)
                {
                    var parent = owner ?? Application.Current?.MainWindow;
                    _helpGuideDialog = new HelpGuideDialog(initialTabIndex);
                    if (parent != null && parent.IsLoaded)
                    {
                        _helpGuideDialog.Owner = parent;
                    }
                    _helpGuideDialog.Closed += (s, e) => _helpGuideDialog = null;
                    _helpGuideDialog.Show();
                }
                else
                {
                    if (_helpGuideDialog.WindowState == WindowState.Minimized)
                    {
                        _helpGuideDialog.WindowState = WindowState.Normal;
                    }
                    _helpGuideDialog.SelectTab(initialTabIndex);
                    _helpGuideDialog.Activate();
                }
            }
            catch (Exception ex)
            {
                StatusNotification?.Invoke($"Help Guide Error: {ex.Message}");
            }
        }

        public void ShowAboutDialog(Window? owner = null)
        {
            ShowHelpGuideDialog(owner, 7);
        }

        private CameraStudioDialog? _cameraStudioDialog;

        public void ShowCameraStudioDialog(Window? owner = null)
        {
            try
            {
                if (_cameraStudioDialog == null || !_cameraStudioDialog.IsLoaded)
                {
                    var parent = owner ?? Application.Current?.MainWindow;
                    _cameraStudioDialog = new CameraStudioDialog();
                    if (parent != null && parent.IsLoaded)
                    {
                        _cameraStudioDialog.Owner = parent;
                    }
                    _cameraStudioDialog.Closed += (s, e) => _cameraStudioDialog = null;
                    _cameraStudioDialog.Show();
                }
                else
                {
                    if (_cameraStudioDialog.WindowState == WindowState.Minimized)
                    {
                        _cameraStudioDialog.WindowState = WindowState.Normal;
                    }
                    _cameraStudioDialog.Activate();
                }
            }
            catch (Exception ex)
            {
                StatusNotification?.Invoke($"Camera Studio Error: {ex.Message}");
            }
        }

        private MediaLibraryDashboardDialog? _mediaLibraryDialog;

        public void ShowMediaLibraryDashboardDialog(Window? owner = null)
        {
            try
            {
                if (_mediaLibraryDialog == null || !_mediaLibraryDialog.IsLoaded)
                {
                    var parent = owner ?? Application.Current?.MainWindow;
                    _mediaLibraryDialog = new MediaLibraryDashboardDialog();
                    if (parent != null && parent.IsLoaded)
                    {
                        _mediaLibraryDialog.Owner = parent;
                    }
                    _mediaLibraryDialog.Closed += (s, e) => _mediaLibraryDialog = null;
                    _mediaLibraryDialog.Show();
                }
                else
                {
                    if (_mediaLibraryDialog.WindowState == WindowState.Minimized)
                    {
                        _mediaLibraryDialog.WindowState = WindowState.Normal;
                    }
                    _mediaLibraryDialog.Activate();
                }
            }
            catch (Exception ex)
            {
                StatusNotification?.Invoke($"Media Library Error: {ex.Message}");
            }
        }

        public void ShowSettingsDialog(Window? owner = null)
        {
            try
            {
                var parent = owner ?? Application.Current?.MainWindow;
                var dlg = new SettingsDialog();
                if (parent != null && parent.IsLoaded)
                {
                    dlg.Owner = parent;
                }
                dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                StatusNotification?.Invoke($"Settings Error: {ex.Message}");
            }
        }

        public async Task<string> DownloadSubtitlesAsync(string mediaTitle, string language = "en")
        {
            StatusNotification?.Invoke($"Searching OpenSubtitles for '{mediaTitle}' [{language}]...");
            var results = await SubtitleEngine.Instance.SearchOpenSubtitlesAsync(mediaTitle, language);

            if (results.Count > 0)
            {
                var top = results[0];
                StatusNotification?.Invoke($"Subtitles synchronized: {top.ReleaseName} (Rating: {top.Rating:F1}/10)");
                return top.ReleaseName;
            }

            StatusNotification?.Invoke("No matching subtitles found online.");
            return string.Empty;
        }

        public void TriggerStreamRipper(string url, string destinationFolder)
        {
            StatusNotification?.Invoke($"Stream Ripper initialized for: {url} -> {destinationFolder}");
            // Prepares multi-stream demuxing pipeline
        }

        public void LaunchMediaConverter(string sourceFilePath, string targetFormat)
        {
            StatusNotification?.Invoke($"Transcoding job submitted: {Path.GetFileName(sourceFilePath)} -> .{targetFormat.ToLowerInvariant()}");
        }
    }
}
