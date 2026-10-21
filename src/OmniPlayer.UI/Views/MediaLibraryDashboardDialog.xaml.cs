using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace OmniPlayer.UI.Views
{
    public class MediaLibraryItem
    {
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // Video, Audio, Image
        public string Extension { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string FormattedSize => FormatBytes(SizeBytes);
        public string FilePath { get; set; } = string.Empty;
        public string DirectoryPath { get; set; } = string.Empty;

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1024 * 1024 * 1024)
                return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
            if (bytes >= 1024 * 1024)
                return $"{bytes / (1024.0 * 1024.0):F1} MB";
            if (bytes >= 1024)
                return $"{bytes / 1024.0:F0} KB";
            return $"{bytes} B";
        }
    }

    public partial class MediaLibraryDashboardDialog : Window
    {
        public static event Action<string>? MediaPlayRequested;

        private readonly List<MediaLibraryItem> _masterCatalog = new();
        private readonly ObservableCollection<MediaLibraryItem> _filteredAll = new();
        private readonly ObservableCollection<MediaLibraryItem> _filteredVideos = new();
        private readonly ObservableCollection<MediaLibraryItem> _filteredAudio = new();
        private string _activeCategoryFilter = "All";

        private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".ts", ".m2ts", ".vob", ".3gp", ".m4v"
        };

        private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".flac", ".wav", ".aac", ".m4a", ".ogg", ".wma", ".opus", ".alac", ".aiff"
        };

        private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".bmp", ".webp", ".gif"
        };

        public MediaLibraryDashboardDialog()
        {
            InitializeComponent();

            DataGridAllMedia.ItemsSource = _filteredAll;
            DataGridVideos.ItemsSource = _filteredVideos;
            DataGridAudio.ItemsSource = _filteredAudio;

            Loaded += async (s, e) => await ScanDeviceMediaAsync();
        }

        private async Task ScanDeviceMediaAsync()
        {
            BtnRescan.IsEnabled = false;
            TxtFooterStatus.Text = "Scanning device storage for media files...";

            _masterCatalog.Clear();

            var searchFolders = new List<string>();
            AddSafePath(searchFolders, Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));
            AddSafePath(searchFolders, Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
            AddSafePath(searchFolders, Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
            AddSafePath(searchFolders, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"));
            AddSafePath(searchFolders, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Videos"));

            await Task.Run(() =>
            {
                foreach (var folder in searchFolders)
                {
                    ScanDirectory(folder);
                }
            });

            UpdateMetricsAndFilters();
            TxtFooterStatus.Text = $"Scan complete! Discovered {_masterCatalog.Count} media assets across local storage.";
            BtnRescan.IsEnabled = true;
        }

        private static void AddSafePath(List<string> list, string path)
        {
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path) && !list.Contains(path, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(path);
            }
        }

        private void ScanDirectory(string rootPath)
        {
            try
            {
                var files = Directory.EnumerateFiles(rootPath, "*.*", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    string ext = Path.GetExtension(file);
                    string cat = "";

                    if (VideoExtensions.Contains(ext)) cat = "Video";
                    else if (AudioExtensions.Contains(ext)) cat = "Audio";
                    else if (ImageExtensions.Contains(ext)) cat = "Image";

                    if (!string.IsNullOrEmpty(cat))
                    {
                        try
                        {
                            var fi = new FileInfo(file);
                            _masterCatalog.Add(new MediaLibraryItem
                            {
                                Title = Path.GetFileNameWithoutExtension(file),
                                Category = cat,
                                Extension = ext.ToUpperInvariant(),
                                SizeBytes = fi.Length,
                                FilePath = file,
                                DirectoryPath = fi.DirectoryName ?? rootPath
                            });
                        }
                        catch { }
                    }
                }

                // Scan 1 level deep subdirectories
                var subDirs = Directory.EnumerateDirectories(rootPath, "*", SearchOption.TopDirectoryOnly);
                foreach (var sub in subDirs)
                {
                    try
                    {
                        var subFiles = Directory.EnumerateFiles(sub, "*.*", SearchOption.TopDirectoryOnly);
                        foreach (var file in subFiles)
                        {
                            string ext = Path.GetExtension(file);
                            string cat = "";
                            if (VideoExtensions.Contains(ext)) cat = "Video";
                            else if (AudioExtensions.Contains(ext)) cat = "Audio";
                            else if (ImageExtensions.Contains(ext)) cat = "Image";

                            if (!string.IsNullOrEmpty(cat))
                            {
                                var fi = new FileInfo(file);
                                _masterCatalog.Add(new MediaLibraryItem
                                {
                                    Title = Path.GetFileNameWithoutExtension(file),
                                    Category = cat,
                                    Extension = ext.ToUpperInvariant(),
                                    SizeBytes = fi.Length,
                                    FilePath = file,
                                    DirectoryPath = fi.DirectoryName ?? sub
                                });
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void UpdateMetricsAndFilters()
        {
            int videos = _masterCatalog.Count(x => x.Category == "Video");
            int music = _masterCatalog.Count(x => x.Category == "Audio");
            long totalBytes = _masterCatalog.Sum(x => x.SizeBytes);

            TxtTotalVideos.Text = videos.ToString();
            TxtTotalMusic.Text = music.ToString();
            TxtTotalStorage.Text = totalBytes >= 1024 * 1024 * 1024
                ? $"{totalBytes / (1024.0 * 1024.0 * 1024.0):F2} GB"
                : $"{totalBytes / (1024.0 * 1024.0):F1} MB";

            ApplySearchAndCategoryFilter();
        }

        private void ApplySearchAndCategoryFilter()
        {
            string query = TxtSearchFilter?.Text?.Trim() ?? "";

            var filtered = _masterCatalog.Where(item =>
            {
                bool matchesType = _activeCategoryFilter == "All" || item.Category.Equals(_activeCategoryFilter, StringComparison.OrdinalIgnoreCase);
                bool matchesQuery = string.IsNullOrEmpty(query) ||
                                    item.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                    item.Extension.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                    item.DirectoryPath.Contains(query, StringComparison.OrdinalIgnoreCase);
                return matchesType && matchesQuery;
            }).ToList();

            _filteredAll.Clear();
            foreach (var item in filtered) _filteredAll.Add(item);

            _filteredVideos.Clear();
            foreach (var item in _masterCatalog.Where(x => x.Category == "Video" && (string.IsNullOrEmpty(query) || x.Title.Contains(query, StringComparison.OrdinalIgnoreCase))))
                _filteredVideos.Add(item);

            _filteredAudio.Clear();
            foreach (var item in _masterCatalog.Where(x => x.Category == "Audio" && (string.IsNullOrEmpty(query) || x.Title.Contains(query, StringComparison.OrdinalIgnoreCase))))
                _filteredAudio.Add(item);
        }

        private void TxtSearchFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearchAndCategoryFilter();
        }

        private void BtnFilterType_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                _activeCategoryFilter = tag;
                ApplySearchAndCategoryFilter();
            }
        }

        private async void BtnRescan_Click(object sender, RoutedEventArgs e)
        {
            await ScanDeviceMediaAsync();
        }

        private void BtnAddFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFolderDialog
            {
                Title = "Select Folder to Scan into Media Library"
            };

            if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.FolderName))
            {
                ScanDirectory(dlg.FolderName);
                UpdateMetricsAndFilters();
                TxtFooterStatus.Text = $"Added and indexed: {dlg.FolderName}";
            }
        }

        private void BtnPlayItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string path)
            {
                PlaySelectedMedia(path);
            }
        }

        private void DataGridMedia_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid grid && grid.SelectedItem is MediaLibraryItem item)
            {
                PlaySelectedMedia(item.FilePath);
            }
        }

        private void PlaySelectedMedia(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    MediaPlayRequested?.Invoke(filePath);
                    if (Application.Current.MainWindow is MainWindow mainWin)
                    {
                        mainWin.PlayMedia(filePath);
                        mainWin.Activate();
                        TxtFooterStatus.Text = $"Now Playing: {Path.GetFileName(filePath)}";
                    }
                }
                catch (Exception ex)
                {
                    TxtFooterStatus.Text = $"Playback Error: {ex.Message}";
                }
            }
        }

        private void BtnOpenSelectedFolder_Click(object sender, RoutedEventArgs e)
        {
            var selected = (DataGridAllMedia.SelectedItem as MediaLibraryItem)
                           ?? (DataGridVideos.SelectedItem as MediaLibraryItem)
                           ?? (DataGridAudio.SelectedItem as MediaLibraryItem);

            if (selected != null && Directory.Exists(selected.DirectoryPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = selected.DirectoryPath,
                    UseShellExecute = true
                });
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
