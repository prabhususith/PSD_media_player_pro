using System;
using System.Windows;
using System.Windows.Controls;
using OmniPlayer.Core.Decoders;
using OmniPlayer.Core.Engine;
using OmniPlayer.Core.Video;

namespace OmniPlayer.UI.Views
{
    public partial class PerformanceDialog : Window
    {
        private readonly PerformanceProfileManager _profileManager = PerformanceProfileManager.Instance;
        private readonly VideoFilterPipeline _filterPipeline = VideoFilterPipeline.Instance;
        private readonly HardwareDecoderManager _decoderManager = HardwareDecoderManager.Instance;
        private bool _isUpdatingUi = false;

        public PerformanceDialog()
        {
            InitializeComponent();
            PopulateDecoderOptions();
            PopulateToneMappingOptions();
            UpdateUiFromManagers();
        }

        private void PopulateDecoderOptions()
        {
            CmbDecoder.Items.Clear();
            foreach (var backend in Enum.GetValues<HardwareDecoderBackend>())
            {
                CmbDecoder.Items.Add(backend.ToString().Replace('_', ' '));
            }
            CmbDecoder.SelectedIndex = 0;
            TxtGpuStatus.Text = $"{_decoderManager.AdapterDescription} ({_decoderManager.DetectedGpuVendor})";
        }

        private void PopulateToneMappingOptions()
        {
            CmbToneMapping.Items.Clear();
            foreach (var curve in Enum.GetValues<HdrToneMappingCurve>())
            {
                CmbToneMapping.Items.Add(curve.ToString().Replace('_', ' '));
            }
            CmbToneMapping.SelectedItem = "BT2390 EETF";
        }

        private void UpdateUiFromManagers()
        {
            _isUpdatingUi = true;
            try
            {
                SliderTier.Value = (int)_profileManager.CurrentTier;
                var settings = _profileManager.CurrentSettings;
                TxtTierTitle.Text = settings.DisplayName;
                TxtTierDesc.Text = $"{settings.Subtitle} | Scaling: {settings.ScalingAlgorithm} | Shaders: {settings.ActiveShaderChainDescription}";

                // Video filter settings
                var v = _filterPipeline.Settings;
                SliderBrightness.Value = v.Brightness;
                TxtBrightnessVal.Text = $"{v.Brightness:+0;-0;0}";

                SliderContrast.Value = v.Contrast;
                TxtContrastVal.Text = $"{v.Contrast:+0;-0;0}";

                SliderSaturation.Value = v.Saturation;
                TxtSaturationVal.Text = $"{v.Saturation:+0;-0;0}";

                SliderHue.Value = v.Hue;
                TxtHueVal.Text = $"{v.Hue:0}°";

                SliderGamma.Value = v.Gamma;
                TxtGammaVal.Text = $"{v.Gamma:0.00}";

                SliderSharpness.Value = v.Sharpness;
                TxtSharpnessVal.Text = $"{v.Sharpness:0}";

                ChkDenoise.IsChecked = v.DenoiseEnabled;
                ChkDering.IsChecked = v.DeringEnabled;
                ChkDeblock.IsChecked = v.DeblockEnabled;
                ChkDeband.IsChecked = v.DebandEnabled;
                ChkSmoothMotion.IsChecked = v.MotionInterpolationEnabled;
                ChkFsrcnnx.IsChecked = v.FsrcnnxActive;
                ChkAnime4K.IsChecked = v.Anime4kActive;
            }
            finally
            {
                _isUpdatingUi = false;
            }
        }

        private void SliderTier_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingUi) return;
            var tier = (PerformanceTier)(int)e.NewValue;
            _profileManager.CurrentTier = tier;
            var settings = _profileManager.CurrentSettings;

            if (TxtTierTitle != null && TxtTierDesc != null)
            {
                TxtTierTitle.Text = settings.DisplayName;
                TxtTierDesc.Text = $"{settings.Subtitle} | Scaling: {settings.ScalingAlgorithm} | RAM Cache: {settings.MemoryCacheSizeMb}MB | Shaders: {settings.ActiveShaderChainDescription}";
            }
        }

        private void PictureSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingUi) return;
            var v = _filterPipeline.Settings;

            if (sender == SliderBrightness) { v.Brightness = (float)e.NewValue; TxtBrightnessVal.Text = $"{v.Brightness:+0;-0;0}"; }
            else if (sender == SliderContrast) { v.Contrast = (float)e.NewValue; TxtContrastVal.Text = $"{v.Contrast:+0;-0;0}"; }
            else if (sender == SliderSaturation) { v.Saturation = (float)e.NewValue; TxtSaturationVal.Text = $"{v.Saturation:+0;-0;0}"; }
            else if (sender == SliderHue) { v.Hue = (float)e.NewValue; TxtHueVal.Text = $"{v.Hue:0}°"; }
            else if (sender == SliderGamma) { v.Gamma = (float)e.NewValue; TxtGammaVal.Text = $"{v.Gamma:0.00}"; }
            else if (sender == SliderSharpness) { v.Sharpness = (float)e.NewValue; TxtSharpnessVal.Text = $"{v.Sharpness:0}"; }

            _filterPipeline.NotifySettingsChanged();
        }

        private void FilterOption_Changed(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingUi) return;
            var v = _filterPipeline.Settings;
            v.DenoiseEnabled = ChkDenoise.IsChecked == true;
            v.DeringEnabled = ChkDering.IsChecked == true;
            v.DeblockEnabled = ChkDeblock.IsChecked == true;
            v.DebandEnabled = ChkDeband.IsChecked == true;
            v.MotionInterpolationEnabled = ChkSmoothMotion.IsChecked == true;
            v.FsrcnnxActive = ChkFsrcnnx.IsChecked == true;
            v.Anime4kActive = ChkAnime4K.IsChecked == true;

            _filterPipeline.NotifySettingsChanged();
        }

        private void CmbToneMapping_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingUi) return;
            if (CmbToneMapping.SelectedItem is string curveStr)
            {
                _filterPipeline.Settings.TonemapAlgorithm = curveStr;
                _filterPipeline.NotifySettingsChanged();
            }
        }

        private void CmbDecoder_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingUi) return;
            // Configures decode backend priorities
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            _filterPipeline.ResetToDefaults();
            UpdateUiFromManagers();
        }
    }
}
