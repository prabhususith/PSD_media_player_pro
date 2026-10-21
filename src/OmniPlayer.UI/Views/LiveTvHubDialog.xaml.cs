using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using OmniPlayer.Core.LiveTv;

namespace OmniPlayer.UI.Views
{
    public partial class LiveTvHubDialog : Window
    {
        private string _selectedCategory = "All (1000+)";
        private string _selectedCountry = "All Countries";
        public event Action<LiveTvChannel>? ChannelSelected;

        public LiveTvHubDialog()
        {
            InitializeComponent();
            BuildCategoryPills();
            PopulateCountries();
            RefreshChannelList();
        }

        private void BuildCategoryPills()
        {
            PanelCategoryPills.Children.Clear();
            var categories = LiveTvManager.Instance.GetCategories();

            foreach (var cat in categories)
            {
                var btn = new Button
                {
                    Content = cat,
                    Tag = cat,
                    Height = 30,
                    Padding = new Thickness(14, 0, 14, 0),
                    Margin = new Thickness(0, 0, 8, 0),
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12,
                    Cursor = Cursors.Hand
                };

                btn.Click += CategoryPill_Click;
                UpdatePillAppearance(btn, cat == _selectedCategory);
                PanelCategoryPills.Children.Add(btn);
            }
        }

        private void PopulateCountries()
        {
            var countries = LiveTvManager.Instance.GetCountries().ToList();
            CmbCountryFilter.ItemsSource = countries;
            if (countries.Count > 0)
            {
                CmbCountryFilter.SelectedIndex = 0;
                _selectedCountry = countries[0];
            }
        }

        private void UpdatePillAppearance(Button btn, bool isSelected)
        {
            if (isSelected)
            {
                btn.SetResourceReference(Button.BackgroundProperty, "OmniAccentBrush");
                btn.Foreground = Brushes.White;
                btn.BorderThickness = new Thickness(0);
            }
            else
            {
                btn.SetResourceReference(Button.BackgroundProperty, "OmniControlBackgroundBrush");
                btn.SetResourceReference(Button.ForegroundProperty, "OmniForegroundBrush");
                btn.SetResourceReference(Button.BorderBrushProperty, "OmniBorderBrush");
                btn.BorderThickness = new Thickness(1);
            }
        }

        private void CategoryPill_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string cat)
            {
                _selectedCategory = cat;
                foreach (Button child in PanelCategoryPills.Children)
                {
                    UpdatePillAppearance(child, (string)child.Tag == _selectedCategory);
                }
                RefreshChannelList();
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TxtSearchPlaceholder != null)
            {
                TxtSearchPlaceholder.Visibility = string.IsNullOrEmpty(TxtSearch.Text) ? Visibility.Visible : Visibility.Collapsed;
            }
            RefreshChannelList();
        }

        private void CmbCountryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbCountryFilter.SelectedItem is string country)
            {
                _selectedCountry = country;
                RefreshChannelList();
            }
        }

        private void RefreshChannelList()
        {
            string query = TxtSearch?.Text ?? string.Empty;
            var list = LiveTvManager.Instance.FilterChannels(_selectedCategory, query, _selectedCountry);
            LstChannels.ItemsSource = list;

            int total = LiveTvManager.Instance.AllChannels.Count;
            TxtFooterStatus.Text = $"Showing {list.Count} of {total} Live TV Channels | Category: {_selectedCategory} | Country: {_selectedCountry}";
            TxtLiveCountBadge.Text = $"⚡ {total} Channels Live";
        }

        private void BtnPlayChannel_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is LiveTvChannel channel)
            {
                PlayChannel(channel);
            }
        }

        private void LstChannels_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (LstChannels.SelectedItem is LiveTvChannel channel)
            {
                PlayChannel(channel);
            }
        }

        private void PlayChannel(LiveTvChannel channel)
        {
            ChannelSelected?.Invoke(channel);
            Close();
        }

        private async void BtnRefreshNetwork_Click(object sender, RoutedEventArgs e)
        {
            BtnRefreshNetwork.IsEnabled = false;
            TxtFooterStatus.Text = "Synchronizing live streams from global public IPTV repositories (India, Kids, Movies, News, Sports...)...";

            try
            {
                int count = await LiveTvManager.Instance.RefreshFromNetworkAsync();
                PopulateCountries();
                RefreshChannelList();
                TxtFooterStatus.Text = $"Network sync complete! Total {count} channels now indexed.";
            }
            catch (Exception ex)
            {
                TxtFooterStatus.Text = $"Sync error: {ex.Message}";
            }
            finally
            {
                BtnRefreshNetwork.IsEnabled = true;
            }
        }

        private void BtnImportM3u_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = "Import Custom M3U / M3U8 Playlist",
                Filter = "Playlist Files (*.m3u;*.m3u8)|*.m3u;*.m3u8|All Files (*.*)|*.*"
            };

            if (ofd.ShowDialog() == true)
            {
                try
                {
                    string content = File.ReadAllText(ofd.FileName);
                    int added = LiveTvManager.Instance.ParseAndMergeM3u(content, Path.GetFileNameWithoutExtension(ofd.FileName));
                    PopulateCountries();
                    RefreshChannelList();
                    MessageBox.Show(
                        $"Successfully imported {added} channels from {Path.GetFileName(ofd.FileName)}!",
                        "M3U Import Successful",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to read M3U file: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnAddCustomStream_Click(object sender, RoutedEventArgs e)
        {
            TxtCustomStreamUrl.Text = string.Empty;
            TxtCustomChannelName.Text = string.Empty;

            var cats = LiveTvManager.Instance.GetCategories()
                .Where(c => !c.StartsWith("All", StringComparison.OrdinalIgnoreCase))
                .ToList();
            CmbCustomCategory.ItemsSource = cats;
            CmbCustomCategory.SelectedItem = cats.Contains(_selectedCategory) ? _selectedCategory : cats.FirstOrDefault();

            OverlayAddStream.Visibility = Visibility.Visible;
            TxtCustomStreamUrl.Focus();
        }

        private void BtnCancelAddStream_Click(object sender, RoutedEventArgs e)
        {
            OverlayAddStream.Visibility = Visibility.Collapsed;
        }

        private async void BtnConfirmAddStream_Click(object sender, RoutedEventArgs e)
        {
            string url = TxtCustomStreamUrl.Text.Trim();
            if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Please enter a valid HTTP/HTTPS stream or playlist URL.", "Invalid URL", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string cat = CmbCustomCategory.SelectedItem as string ?? "Indian National";
            string name = TxtCustomChannelName.Text.Trim();

            // Check if it is an M3U playlist feed
            if (url.EndsWith(".m3u", StringComparison.OrdinalIgnoreCase) || url.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase) && url.Contains("playlist"))
            {
                try
                {
                    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                    string content = await client.GetStringAsync(url);
                    if (content.Contains("#EXTINF"))
                    {
                        int added = LiveTvManager.Instance.ParseAndMergeM3u(content, cat);
                        PopulateCountries();
                        RefreshChannelList();
                        OverlayAddStream.Visibility = Visibility.Collapsed;
                        MessageBox.Show($"Added {added} channels from the playlist!", "Playlist Added", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                }
                catch
                {
                    // Fall back to adding as a single stream
                }
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                try
                {
                    var uri = new Uri(url);
                    name = Path.GetFileNameWithoutExtension(uri.LocalPath);
                    if (string.IsNullOrWhiteSpace(name)) name = "Custom Stream";
                }
                catch
                {
                    name = "Custom Stream";
                }
            }

            var channel = new LiveTvChannel
            {
                Name = name,
                StreamUrl = url,
                Category = cat,
                Language = "English",
                Country = "IN",
                Resolution = "1080p",
                IsActive = true
            };

            LiveTvManager.Instance.AddChannel(channel);
            PopulateCountries();
            RefreshChannelList();
            OverlayAddStream.Visibility = Visibility.Collapsed;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
