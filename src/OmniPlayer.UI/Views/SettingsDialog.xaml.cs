using System;
using System.Windows;
using OmniPlayer.Core.Audio;
using OmniPlayer.Core.Engine;

namespace OmniPlayer.UI.Views
{
    public class PlayerSettingsModel
    {
        public bool ResumePlayback { get; set; } = true;
        public bool SingleInstance { get; set; } = true;
        public bool PauseOnMinimize { get; set; } = false;
        public bool AutoPlay { get; set; } = true;
        public double AutoHideSeconds { get; set; } = 2.5;
        public double OsdSeconds { get; set; } = 2.5;
        public double DefaultVolume { get; set; } = 0.8;
        public string HwDecoder { get; set; } = "d3d11va";
        public string DefaultAspect { get; set; } = "Auto";
        public string DeinterlaceMethod { get; set; } = "YADIF";
        public int PerformanceTier { get; set; } = 3;
        public bool WasapiExclusive { get; set; } = false;
        public bool DspEnabled { get; set; } = true;
        public bool DrcEnabled { get; set; } = false;
        public bool HaasSurroundEnabled { get; set; } = false;
        public bool NormalizerEnabled { get; set; } = false;
        public string SubFont { get; set; } = "Segoe UI";
        public double SubSize { get; set; } = 28;
        public string SubColor { get; set; } = "#FFFFFF";
        public string NetworkBufferSize { get; set; } = "4 MB";
        public string UserAgent { get; set; } = "OmniPlayer/2.5.0";
    }

    public partial class SettingsDialog : Window
    {
        public static PlayerSettingsModel CurrentSettings { get; } = new();
        public event Action<PlayerSettingsModel>? SettingsApplied;

        public SettingsDialog()
        {
            InitializeComponent();
            LoadCurrentToUi();
        }

        private void LoadCurrentToUi()
        {
            ChkResumePlayback.IsChecked = CurrentSettings.ResumePlayback;
            ChkSingleInstance.IsChecked = CurrentSettings.SingleInstance;
            ChkPauseOnMinimize.IsChecked = CurrentSettings.PauseOnMinimize;
            ChkAutoPlay.IsChecked = CurrentSettings.AutoPlay;
            SliderDefaultVol.Value = CurrentSettings.DefaultVolume * 100.0;

            CmbAudioMode.SelectedIndex = CurrentSettings.WasapiExclusive ? 1 : 0;
            ChkEnableDsp.IsChecked = CurrentSettings.DspEnabled;
            ChkEnableDrc.IsChecked = CurrentSettings.DrcEnabled;
            ChkEnableHaas.IsChecked = CurrentSettings.HaasSurroundEnabled;
            ChkEnableNormalizer.IsChecked = CurrentSettings.NormalizerEnabled;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            CurrentSettings.ResumePlayback = ChkResumePlayback.IsChecked == true;
            CurrentSettings.SingleInstance = ChkSingleInstance.IsChecked == true;
            CurrentSettings.PauseOnMinimize = ChkPauseOnMinimize.IsChecked == true;
            CurrentSettings.AutoPlay = ChkAutoPlay.IsChecked == true;
            CurrentSettings.DefaultVolume = SliderDefaultVol.Value / 100.0;

            CurrentSettings.WasapiExclusive = CmbAudioMode.SelectedIndex == 1;
            CurrentSettings.DspEnabled = ChkEnableDsp.IsChecked == true;
            CurrentSettings.DrcEnabled = ChkEnableDrc.IsChecked == true;
            CurrentSettings.HaasSurroundEnabled = ChkEnableHaas.IsChecked == true;
            CurrentSettings.NormalizerEnabled = ChkEnableNormalizer.IsChecked == true;

            // Apply directly to DSP
            var dsp = EqualizerDsp.Instance;
            dsp.IsEqualizerEnabled = CurrentSettings.DspEnabled;
            dsp.NightModeDrcEnabled = CurrentSettings.DrcEnabled;
            dsp.SpatialSurroundEnabled = CurrentSettings.HaasSurroundEnabled;
            dsp.VoiceNormalizationEnabled = CurrentSettings.NormalizerEnabled;

            // Apply to Audio Engine
            OmniAudioEngine.Instance.WasapiExclusiveMode = CurrentSettings.WasapiExclusive;

            SettingsApplied?.Invoke(CurrentSettings);
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
