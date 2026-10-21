using System.Windows;
using OmniPlayer.Core.Decoders;

namespace OmniPlayer.UI.Views
{
    public partial class MediaInfoDialog : Window
    {
        public MediaInfoDialog()
        {
            InitializeComponent();
            TxtDecoderBackend.Text = $"{HardwareDecoderManager.Instance.AdapterDescription} ({HardwareDecoderManager.Instance.UserPriorityList[0]})";
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            string report = HardwareDecoderManager.Instance.GetDiagnosticsReport();
            Clipboard.SetText(report);
            MessageBox.Show("Diagnostics telemetry copied to clipboard!", "Omni Player Diagnostics", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
