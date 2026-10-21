using System;
using System.Windows;
using System.Windows.Controls;
using OmniPlayer.Core.Engine;

namespace OmniPlayer.UI.Views
{
    public partial class OpenUrlDialog : Window
    {
        public event Action<StreamResolutionResult>? StreamResolved;

        public OpenUrlDialog()
        {
            InitializeComponent();
        }

        private void BtnPaste_Click(object sender, RoutedEventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                TxtUrl.Text = Clipboard.GetText().Trim();
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            TxtUrl.Clear();
            TxtStatus.Text = "Ready to stream.";
        }

        private void Preset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string url)
            {
                TxtUrl.Text = url;
                TxtStatus.Text = $"Selected preset: {btn.Content}";
            }
        }

        private async void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            string url = TxtUrl.Text.Trim();
            if (string.IsNullOrWhiteSpace(url))
            {
                TxtStatus.Text = "Please enter or paste a valid stream URL.";
                return;
            }

            BtnPlay.IsEnabled = false;
            TxtStatus.Text = "Analyzing stream & resolving formats...";

            try
            {
                var result = await StreamResolver.ResolveAsync(url);
                if (result.IsSuccess)
                {
                    TxtStatus.Text = $"Stream resolved: {result.Title} ({result.Type})";
                    StreamResolved?.Invoke(result);
                    Close();
                }
                else
                {
                    TxtStatus.Text = $"Resolution failed: {result.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"Error: {ex.Message}";
            }
            finally
            {
                BtnPlay.IsEnabled = true;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
