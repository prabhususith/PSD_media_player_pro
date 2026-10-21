using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OmniPlayer.Core.Audio;
using OmniPlayer.Core.Engine;
using OmniPlayer.Core.Subtitles;
using OmniPlayer.Core.Video;
using OmniPlayer.UI.Controls;

namespace OmniPlayer.UI.Views
{
    /// <summary>
    /// Elegant floating quick-control dialog for real-time video, audio, subtitle, and playback adjustments.
    /// </summary>
    public partial class ControlPanelDialog : Window
    {
        private readonly VideoFilterPipeline _filterPipeline = VideoFilterPipeline.Instance;
        private readonly EqualizerDsp _dsp = EqualizerDsp.Instance;
        private readonly SubtitleEngine _subEngine = SubtitleEngine.Instance;
        private readonly ToolsMenuController _tools = ToolsMenuController.Instance;
        private bool _isUpdatingUi = false;

        #region Public Events
        // Video Events
        public event EventHandler<VideoFilterSettings>? VideoFiltersChanged;
        public event EventHandler<string>? AspectRatioChanged;
        public event EventHandler<bool>? DeinterlacingChanged;
        public event EventHandler? VideoResetRequested;

        // Audio Events
        public event EventHandler<double>? VolumeChanged;
        public event EventHandler<double>? AudioBalanceChanged;
        public event EventHandler<string>? EqualizerPresetChanged;
        public event EventHandler<float>? PreampChanged;
        public event EventHandler<bool>? NightModeChanged;
        public event EventHandler<bool>? SurroundChanged;
        public event EventHandler<SoundTechnologyMode>? SoundTechnologyChanged;
        public event EventHandler<RoomEchoPreset>? RoomEchoChanged;
        public event EventHandler<float>? ReverbWetChanged;
        public event EventHandler<int>? PitchChanged;
        public event EventHandler<bool>? KaraokeChanged;
        public event EventHandler<bool>? DialogueClarityChanged;
        public event EventHandler? OpenFullEqualizerRequested;

        // Subtitle Events
        public event EventHandler<double>? SubtitleFontSizeChanged;
        public event EventHandler<int>? SubtitleVerticalPosChanged;
        public event EventHandler<int>? SubtitleSyncOffsetAdjusted;
        public event EventHandler? SubtitleSyncReset;
        public event EventHandler<string>? SubtitleColorChanged;
        public event EventHandler<bool>? DualSubtitlesChanged;

        // Playback Events
        public event EventHandler<double>? PlaybackSpeedChanged;
        public event EventHandler? SetMarkerARequested;
        public event EventHandler? SetMarkerBRequested;
        public event EventHandler? ClearAbRepeatRequested;
        public event EventHandler<int>? SkipDurationChanged;
        #endregion

        #region Public Properties
        public string SelectedAspectRatio { get; private set; } = "Auto";
        public bool IsDeinterlacingEnabled { get; private set; } = false;
        public double AudioBalance { get; private set; } = 0.0;
        public int CurrentSubtitleOffsetMs { get; private set; } = 0;
        public int SkipDurationSeconds { get; private set; } = 5;
        public double CurrentSpeed => SliderSpeed != null ? SliderSpeed.Value : 1.0;
        #endregion

        public ControlPanelDialog()
        {
            InitializeComponent();
            Loaded += ControlPanelDialog_Loaded;
            Closed += ControlPanelDialog_Closed;

            // Subscribe to external A-B Repeat events so status is always in sync
            _tools.AbRepeatChanged += OnAbRepeatChanged;
        }

        private void ControlPanelDialog_Loaded(object sender, RoutedEventArgs e)
        {
            PopulateEqualizerPresets();
            PopulateSoundTechnologies();
            PopulateRoomEchoPresets();
            SyncFromCurrentState();
        }

        private void ControlPanelDialog_Closed(object? sender, EventArgs e)
        {
            _tools.AbRepeatChanged -= OnAbRepeatChanged;
        }

        private void PopulateEqualizerPresets()
        {
            if (CmbEqPreset == null) return;
            CmbEqPreset.Items.Clear();

            foreach (var preset in _dsp.FactoryPresets.Values)
            {
                CmbEqPreset.Items.Add(preset.Name);
            }

            if (CmbEqPreset.Items.Contains("Flat"))
            {
                CmbEqPreset.SelectedItem = "Flat";
            }
            else if (CmbEqPreset.Items.Count > 0)
            {
                CmbEqPreset.SelectedIndex = 0;
            }
        }

        private void PopulateSoundTechnologies()
        {
            if (CmbSoundTechnology == null) return;
            CmbSoundTechnology.Items.Clear();
            CmbSoundTechnology.Items.Add("Standard Stereo (Transparent)");
            CmbSoundTechnology.Items.Add("Dolby Atmos 3D Simulation");
            CmbSoundTechnology.Items.Add("DTS:X Spatializer");
            CmbSoundTechnology.Items.Add("5.1 Virtual Surround Upmix");
            CmbSoundTechnology.Items.Add("7.1 Virtual Surround Upmix");
            CmbSoundTechnology.Items.Add("3D Headphone / Headset HRTF");
            CmbSoundTechnology.SelectedIndex = (int)_dsp.ActiveSoundTechnology;
        }

        private void PopulateRoomEchoPresets()
        {
            if (CmbRoomEcho == null) return;
            CmbRoomEcho.Items.Clear();
            CmbRoomEcho.Items.Add("None (Dry Studio)");
            CmbRoomEcho.Items.Add("Living Room (0.4s RT60)");
            CmbRoomEcho.Items.Add("Studio Monitor (0.2s RT60)");
            CmbRoomEcho.Items.Add("Concert Hall (1.8s RT60)");
            CmbRoomEcho.Items.Add("Sacred Cathedral (3.5s RT60)");
            CmbRoomEcho.Items.Add("Cavern Echo (4.5s RT60)");
            CmbRoomEcho.SelectedIndex = (int)_dsp.ActiveRoomEcho;
        }

        /// <summary>
        /// Reads current settings from DSP, video pipeline, and subtitle engine to refresh all UI controls.
        /// </summary>
        public void SyncFromCurrentState()
        {
            _isUpdatingUi = true;
            try
            {
                // 1. Sync Video
                var v = _filterPipeline.Settings;
                if (SliderBrightness != null) SliderBrightness.Value = v.Brightness;
                if (TxtBrightnessVal != null) TxtBrightnessVal.Text = $"{v.Brightness:+0;-0;0}";

                if (SliderContrast != null) SliderContrast.Value = v.Contrast;
                if (TxtContrastVal != null) TxtContrastVal.Text = $"{v.Contrast:+0;-0;0}";

                if (SliderSaturation != null) SliderSaturation.Value = v.Saturation;
                if (TxtSaturationVal != null) TxtSaturationVal.Text = $"{v.Saturation:+0;-0;0}";

                if (SliderHue != null) SliderHue.Value = v.Hue;
                if (TxtHueVal != null) TxtHueVal.Text = $"{v.Hue:0}°";

                if (SliderGamma != null) SliderGamma.Value = v.Gamma;
                if (TxtGammaVal != null) TxtGammaVal.Text = $"{v.Gamma:0.00}";

                if (ChkDeinterlacing != null)
                {
                    var deint = PerformanceProfileManager.Instance.CurrentSettings.Deinterlacing;
                    ChkDeinterlacing.IsChecked = deint != DeinterlaceEngine.None;
                }

                // 2. Sync Audio
                if (SliderAudioVolume != null)
                {
                    double vol = OmniAudioEngine.Instance.IsActive
                        ? OmniAudioEngine.Instance.Volume * 100.0
                        : (Owner is MainWindow mw ? mw.SliderVolume.Value * 100.0 : 80.0);
                    SliderAudioVolume.Value = vol;
                    if (TxtVolumeVal != null) TxtVolumeVal.Text = $"{(int)vol}%";
                }

                if (SliderAudioBalance != null && Owner is MainWindow mainWin)
                {
                    SliderAudioBalance.Value = mainWin.PlayerMediaElement.Balance * 100.0;
                    UpdateBalanceText(SliderAudioBalance.Value);
                }

                if (SliderPreamp != null)
                {
                    SliderPreamp.Value = _dsp.PreampDb;
                    if (TxtPreampVal != null) TxtPreampVal.Text = $"{_dsp.PreampDb:+0.0;-0.0;0.0} dB";
                }

                if (CmbSoundTechnology != null)
                {
                    CmbSoundTechnology.SelectedIndex = (int)_dsp.ActiveSoundTechnology;
                }

                if (CmbRoomEcho != null)
                {
                    CmbRoomEcho.SelectedIndex = (int)_dsp.ActiveRoomEcho;
                }

                if (SliderReverbWet != null)
                {
                    SliderReverbWet.Value = _dsp.ReverbWetMix * 100.0f;
                    if (TxtReverbWetVal != null) TxtReverbWetVal.Text = $"{(int)(_dsp.ReverbWetMix * 100.0f)}%";
                }

                if (SliderPitch != null)
                {
                    SliderPitch.Value = _dsp.PitchSemitones;
                    if (TxtPitchVal != null) TxtPitchVal.Text = $"{_dsp.PitchSemitones:+0;-0;0} st";
                }

                if (ChkKaraoke != null) ChkKaraoke.IsChecked = _dsp.KaraokeVocalRemoverEnabled;
                if (ChkDialogueClarity != null) ChkDialogueClarity.IsChecked = _dsp.DialogueClarityEnabled;
                if (ChkVoiceNorm != null) ChkVoiceNorm.IsChecked = _dsp.VoiceNormalizationEnabled;
                if (ChkNightMode != null) ChkNightMode.IsChecked = _dsp.NightModeDrcEnabled;
                if (ChkSurround != null) ChkSurround.IsChecked = _dsp.SpatialSurroundEnabled;

                // 3. Sync Subtitles
                if (SliderSubFontSize != null)
                {
                    SliderSubFontSize.Value = _subEngine.PrimaryStyle.FontSize;
                    if (TxtSubFontSizeVal != null) TxtSubFontSizeVal.Text = $"{(int)_subEngine.PrimaryStyle.FontSize} pt";
                }

                if (SliderSubVerticalPos != null)
                {
                    SliderSubVerticalPos.Value = _subEngine.PrimaryStyle.MarginVertical;
                    if (TxtSubVerticalPosVal != null) TxtSubVerticalPosVal.Text = $"{_subEngine.PrimaryStyle.MarginVertical} px";
                }

                if (ChkDualSubtitles != null)
                {
                    ChkDualSubtitles.IsChecked = _subEngine.DualSubtitlesEnabled;
                }

                UpdateSubtitleSyncOffsetLabel();

                // 4. Sync Playback
                if (SliderSpeed != null && Owner is MainWindow m)
                {
                    SliderSpeed.Value = m.PlayerMediaElement.SpeedRatio;
                    UpdateSpeedText(m.PlayerMediaElement.SpeedRatio);
                }

                UpdateAbStatusUi(_tools.MarkerA, _tools.MarkerB);
            }
            finally
            {
                _isUpdatingUi = false;
            }
        }

        #region Always on Top
        private void ChkAlwaysOnTop_Changed(object sender, RoutedEventArgs e)
        {
            Topmost = ChkAlwaysOnTop?.IsChecked == true;
        }
        #endregion

        #region Video Tab Handlers
        private void PictureSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingUi || _filterPipeline == null) return;

            var v = _filterPipeline.Settings;
            if (sender == SliderBrightness && TxtBrightnessVal != null)
            {
                v.Brightness = (float)e.NewValue;
                TxtBrightnessVal.Text = $"{v.Brightness:+0;-0;0}";
            }
            else if (sender == SliderContrast && TxtContrastVal != null)
            {
                v.Contrast = (float)e.NewValue;
                TxtContrastVal.Text = $"{v.Contrast:+0;-0;0}";
            }
            else if (sender == SliderSaturation && TxtSaturationVal != null)
            {
                v.Saturation = (float)e.NewValue;
                TxtSaturationVal.Text = $"{v.Saturation:+0;-0;0}";
            }
            else if (sender == SliderHue && TxtHueVal != null)
            {
                v.Hue = (float)e.NewValue;
                TxtHueVal.Text = $"{v.Hue:0}°";
            }
            else if (sender == SliderGamma && TxtGammaVal != null)
            {
                v.Gamma = (float)e.NewValue;
                TxtGammaVal.Text = $"{v.Gamma:0.00}";
            }

            _filterPipeline.NotifySettingsChanged();
            VideoFiltersChanged?.Invoke(this, v);
        }

        private void BtnResetVideo_Click(object sender, RoutedEventArgs e)
        {
            if (_filterPipeline == null) return;
            _filterPipeline.ResetToDefaults();

            _isUpdatingUi = true;
            try
            {
                var v = _filterPipeline.Settings;
                if (SliderBrightness != null) { SliderBrightness.Value = v.Brightness; TxtBrightnessVal.Text = "0"; }
                if (SliderContrast != null) { SliderContrast.Value = v.Contrast; TxtContrastVal.Text = "0"; }
                if (SliderSaturation != null) { SliderSaturation.Value = v.Saturation; TxtSaturationVal.Text = "0"; }
                if (SliderHue != null) { SliderHue.Value = v.Hue; TxtHueVal.Text = "0°"; }
                if (SliderGamma != null) { SliderGamma.Value = v.Gamma; TxtGammaVal.Text = "1.00"; }
            }
            finally
            {
                _isUpdatingUi = false;
            }

            VideoResetRequested?.Invoke(this, EventArgs.Empty);
            if (Owner is MainWindow mw)
            {
                mw.ShowOsd("Video Adjustments: Reset to Defaults");
            }
        }

        private void CmbAspectRatio_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingUi || CmbAspectRatio == null) return;
            if (CmbAspectRatio.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                SelectedAspectRatio = tag;
                AspectRatioChanged?.Invoke(this, tag);

                if (Owner is MainWindow mw)
                {
                    mw.PlayerMediaElement.Stretch = (tag == "Fill")
                        ? System.Windows.Media.Stretch.Fill
                        : System.Windows.Media.Stretch.Uniform;
                    mw.ShowOsd($"Aspect Ratio: {tag}");
                }
            }
        }

        private void ChkDeinterlacing_Changed(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingUi || ChkDeinterlacing == null) return;
            bool enabled = ChkDeinterlacing.IsChecked == true;
            IsDeinterlacingEnabled = enabled;

            PerformanceProfileManager.Instance.CurrentSettings.Deinterlacing = enabled
                ? DeinterlaceEngine.YADIF_Auto
                : DeinterlaceEngine.None;

            DeinterlacingChanged?.Invoke(this, enabled);
            if (Owner is MainWindow mw)
            {
                mw.ShowOsd(enabled ? "Deinterlacing: Active (YADIF)" : "Deinterlacing: Disabled");
            }
        }
        #endregion

        #region Audio Tab Handlers
        private void SliderAudioVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingUi) return;
            double volPercent = e.NewValue;
            if (TxtVolumeVal != null) TxtVolumeVal.Text = $"{(int)volPercent}%";

            float linearVol = (float)(volPercent / 100.0);
            if (OmniAudioEngine.Instance.IsActive)
            {
                OmniAudioEngine.Instance.Volume = linearVol;
            }

            if (Owner is MainWindow mw)
            {
                mw.SliderVolume.Value = linearVol;
            }

            VolumeChanged?.Invoke(this, linearVol);
        }

        private void SliderAudioBalance_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingUi) return;
            double bal = e.NewValue;
            UpdateBalanceText(bal);

            double normalizedBal = bal / 100.0;
            AudioBalance = normalizedBal;

            if (Owner is MainWindow mw)
            {
                mw.PlayerMediaElement.Balance = normalizedBal;
                mw.ShowOsd(TxtBalanceVal?.Text != null ? $"Audio Balance: {TxtBalanceVal.Text}" : "Audio Balance");
            }

            AudioBalanceChanged?.Invoke(this, normalizedBal);
        }

        private void UpdateBalanceText(double val)
        {
            if (TxtBalanceVal == null) return;
            int rounded = (int)Math.Round(val);
            if (rounded == 0)
            {
                TxtBalanceVal.Text = "Center";
            }
            else if (rounded < 0)
            {
                TxtBalanceVal.Text = $"L {Math.Abs(rounded)}%";
            }
            else
            {
                TxtBalanceVal.Text = $"R {rounded}%";
            }
        }

        private void CmbEqPreset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingUi || CmbEqPreset == null || _dsp == null) return;
            if (CmbEqPreset.SelectedItem is string presetName)
            {
                _dsp.ApplyPreset(presetName);
                if (SliderPreamp != null) SliderPreamp.Value = _dsp.PreampDb;
                if (TxtPreampVal != null) TxtPreampVal.Text = $"{_dsp.PreampDb:+0.0;-0.0;0.0} dB";

                EqualizerPresetChanged?.Invoke(this, presetName);
                if (Owner is MainWindow mw)
                {
                    mw.BtnQuickEq.Content = $"🎚 EQ: {presetName}";
                    mw.ShowOsd($"EQ Preset: {presetName}");
                }
            }
        }

        private void SliderPreamp_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingUi || _dsp == null) return;
            float preamp = (float)e.NewValue;
            _dsp.PreampDb = preamp;
            if (TxtPreampVal != null)
            {
                TxtPreampVal.Text = $"{preamp:+0.0;-0.0;0.0} dB";
            }

            PreampChanged?.Invoke(this, preamp);
        }

        private void CmbSoundTechnology_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingUi || CmbSoundTechnology == null || _dsp == null) return;
            if (CmbSoundTechnology.SelectedIndex >= 0)
            {
                var mode = (SoundTechnologyMode)CmbSoundTechnology.SelectedIndex;
                _dsp.ActiveSoundTechnology = mode;
                SoundTechnologyChanged?.Invoke(this, mode);
                if (Owner is MainWindow mw)
                {
                    mw.ShowOsd($"Sound Technology: {mode}");
                }
            }
        }

        private void CmbRoomEcho_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingUi || CmbRoomEcho == null || _dsp == null) return;
            if (CmbRoomEcho.SelectedIndex >= 0)
            {
                var preset = (RoomEchoPreset)CmbRoomEcho.SelectedIndex;
                _dsp.ActiveRoomEcho = preset;
                RoomEchoChanged?.Invoke(this, preset);
                if (Owner is MainWindow mw)
                {
                    mw.ShowOsd($"Room Echo Preset: {preset}");
                }
            }
        }

        private void SliderReverbWet_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingUi || _dsp == null) return;
            float wet = (float)(e.NewValue / 100.0);
            _dsp.ReverbWetMix = wet;
            if (TxtReverbWetVal != null) TxtReverbWetVal.Text = $"{(int)e.NewValue}%";
            ReverbWetChanged?.Invoke(this, wet);
        }

        private void SliderPitch_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingUi || _dsp == null) return;
            int semitones = (int)Math.Round(e.NewValue);
            _dsp.PitchSemitones = semitones;
            if (TxtPitchVal != null) TxtPitchVal.Text = $"{semitones:+0;-0;0} st";
            PitchChanged?.Invoke(this, semitones);
            if (Owner is MainWindow mw)
            {
                mw.ShowOsd($"Pitch: {semitones:+0;-0;0} semitones");
            }
        }

        private void BtnResetPitch_Click(object sender, RoutedEventArgs e)
        {
            if (_dsp == null) return;
            _dsp.PitchSemitones = 0;
            _dsp.PitchCents = 0.0f;
            if (SliderPitch != null) SliderPitch.Value = 0;
            if (TxtPitchVal != null) TxtPitchVal.Text = "0 st";
            PitchChanged?.Invoke(this, 0);
            if (Owner is MainWindow mw)
            {
                mw.ShowOsd("Pitch: 0 semitones (Normal)");
            }
        }

        private void DspOption_Changed(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingUi || _dsp == null) return;

            if (sender == ChkNightMode)
            {
                bool night = ChkNightMode?.IsChecked == true;
                _dsp.NightModeDrcEnabled = night;
                NightModeChanged?.Invoke(this, night);
                if (Owner is MainWindow mw)
                {
                    mw.ShowOsd(night ? "Audio DSP: Night Mode (DRC) ENABLED" : "Audio DSP: Night Mode DISABLED");
                }
            }
            else if (sender == ChkVoiceNorm)
            {
                bool voiceNorm = ChkVoiceNorm?.IsChecked == true;
                _dsp.VoiceNormalizationEnabled = voiceNorm;
                if (Owner is MainWindow mw)
                {
                    mw.ShowOsd(voiceNorm ? "Audio DSP: Voice Normalization ENABLED" : "Audio DSP: Voice Normalization DISABLED");
                }
            }
            else if (sender == ChkSurround)
            {
                bool surround = ChkSurround?.IsChecked == true;
                _dsp.SpatialSurroundEnabled = surround;
                SurroundChanged?.Invoke(this, surround);
                if (Owner is MainWindow mw)
                {
                    mw.ShowOsd(surround ? "Audio DSP: 3D Spatial Surround ACTIVE" : "Audio DSP: 3D Surround DISABLED");
                }
            }
            else if (sender == ChkKaraoke)
            {
                bool karaoke = ChkKaraoke?.IsChecked == true;
                _dsp.KaraokeVocalRemoverEnabled = karaoke;
                KaraokeChanged?.Invoke(this, karaoke);
                if (Owner is MainWindow mw)
                {
                    mw.ShowOsd(karaoke ? "Audio DSP: Karaoke Vocal Remover ACTIVE" : "Audio DSP: Karaoke Vocal Remover OFF");
                }
            }
            else if (sender == ChkDialogueClarity)
            {
                bool clarity = ChkDialogueClarity?.IsChecked == true;
                _dsp.DialogueClarityEnabled = clarity;
                DialogueClarityChanged?.Invoke(this, clarity);
                if (Owner is MainWindow mw)
                {
                    mw.ShowOsd(clarity ? "Audio DSP: Smart Dialogue Enhancer ACTIVE" : "Audio DSP: Dialogue Enhancer OFF");
                }
            }
        }

        private void BtnOpenFullEq_Click(object sender, RoutedEventArgs e)
        {
            OpenFullEqualizerRequested?.Invoke(this, EventArgs.Empty);
            _tools.ShowEqualizerDialog(this.Owner ?? this);
        }
        #endregion

        #region Subtitles Tab Handlers
        private void SliderSubFontSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingUi || _subEngine == null) return;
            double sz = Math.Round(e.NewValue);
            if (TxtSubFontSizeVal != null) TxtSubFontSizeVal.Text = $"{sz:0} pt";

            _subEngine.PrimaryStyle.FontSize = sz;
            if (Owner is MainWindow mw && mw.TxtPrimarySubtitle != null)
            {
                mw.TxtPrimarySubtitle.FontSize = sz;
            }

            SubtitleFontSizeChanged?.Invoke(this, sz);
        }

        private void SliderSubVerticalPos_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingUi || _subEngine == null) return;
            int margin = (int)Math.Round(e.NewValue);
            if (TxtSubVerticalPosVal != null) TxtSubVerticalPosVal.Text = $"{margin} px";

            _subEngine.PrimaryStyle.MarginVertical = margin;
            if (Owner is MainWindow mw && mw.TxtPrimarySubtitle?.Parent is Border border)
            {
                border.Margin = new Thickness(0, 0, 0, margin);
            }

            SubtitleVerticalPosChanged?.Invoke(this, margin);
        }

        private void CmbSubColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingUi || CmbSubColor == null || _subEngine == null) return;
            if (CmbSubColor.SelectedItem is ComboBoxItem item && item.Tag is string hex)
            {
                _subEngine.PrimaryStyle.PrimaryColorHex = hex;
                if (Owner is MainWindow mw && mw.TxtPrimarySubtitle != null)
                {
                    try
                    {
                        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
                        mw.TxtPrimarySubtitle.Foreground = brush;
                    }
                    catch { }
                }

                SubtitleColorChanged?.Invoke(this, hex);
            }
        }

        private void BtnSyncMinus500_Click(object sender, RoutedEventArgs e) => AdjustSubtitleSync(-500);
        private void BtnSyncMinus50_Click(object sender, RoutedEventArgs e) => AdjustSubtitleSync(-50);
        private void BtnSyncPlus50_Click(object sender, RoutedEventArgs e) => AdjustSubtitleSync(50);
        private void BtnSyncPlus500_Click(object sender, RoutedEventArgs e) => AdjustSubtitleSync(500);

        private void BtnSyncReset_Click(object sender, RoutedEventArgs e)
        {
            if (_subEngine?.PrimaryTrack != null)
            {
                _subEngine.PrimaryTrack.SyncOffset = TimeSpan.Zero;
            }
            CurrentSubtitleOffsetMs = 0;
            UpdateSubtitleSyncOffsetLabel();

            SubtitleSyncReset?.Invoke(this, EventArgs.Empty);
            if (Owner is MainWindow mw)
            {
                mw.ShowOsd("Subtitle Sync: Reset to 0 ms");
            }
        }

        private void AdjustSubtitleSync(int deltaMs)
        {
            if (_subEngine == null) return;
            _subEngine.AdjustOffset(true, deltaMs);

            if (_subEngine.PrimaryTrack != null)
            {
                CurrentSubtitleOffsetMs = (int)_subEngine.PrimaryTrack.SyncOffset.TotalMilliseconds;
            }
            else
            {
                CurrentSubtitleOffsetMs += deltaMs;
            }

            UpdateSubtitleSyncOffsetLabel();
            SubtitleSyncOffsetAdjusted?.Invoke(this, deltaMs);

            if (Owner is MainWindow mw)
            {
                mw.ShowOsd($"Subtitle Sync: {CurrentSubtitleOffsetMs:+0;-0;0} ms");
            }
        }

        private void UpdateSubtitleSyncOffsetLabel()
        {
            if (TxtSyncOffsetVal == null) return;
            int offset = _subEngine?.PrimaryTrack != null
                ? (int)_subEngine.PrimaryTrack.SyncOffset.TotalMilliseconds
                : CurrentSubtitleOffsetMs;
            TxtSyncOffsetVal.Text = $"Sync Offset: {offset:+0;-0;0} ms";
        }

        private void ChkDualSubtitles_Changed(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingUi || ChkDualSubtitles == null || _subEngine == null) return;
            bool enabled = ChkDualSubtitles.IsChecked == true;
            _subEngine.DualSubtitlesEnabled = enabled;

            if (Owner is MainWindow mw)
            {
                mw.MenuDualSubtitles.IsChecked = enabled;
                mw.ShowOsd(enabled ? "Dual Subtitle Mode ACTIVE" : "Dual Subtitle Mode Disabled");
            }

            DualSubtitlesChanged?.Invoke(this, enabled);
        }
        #endregion

        #region Playback Tab Handlers
        private void SliderSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingUi) return;
            double speed = Math.Round(e.NewValue, 2);
            UpdateSpeedText(speed);

            if (Owner is MainWindow mw)
            {
                mw.PlayerMediaElement.SpeedRatio = speed;
                mw.ShowOsd($"Playback Speed: {speed:0.0#}x");
            }

            PlaybackSpeedChanged?.Invoke(this, speed);
        }

        private void UpdateSpeedText(double speed)
        {
            if (TxtSpeedVal == null) return;
            string suffix = Math.Abs(speed - 1.0) < 0.01 ? " (Normal)" : "";
            TxtSpeedVal.Text = $"{speed:0.0#}x{suffix}";
        }

        private void BtnSpeedPreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && double.TryParse(btn.Tag?.ToString(), out double speed))
            {
                if (SliderSpeed != null) SliderSpeed.Value = speed;
            }
        }

        private void BtnSetA_Click(object sender, RoutedEventArgs e)
        {
            if (Owner is MainWindow mw)
            {
                _tools.SetMarkerA(mw.PlayerMediaElement.Position);
            }
            SetMarkerARequested?.Invoke(this, EventArgs.Empty);
        }

        private void BtnSetB_Click(object sender, RoutedEventArgs e)
        {
            if (Owner is MainWindow mw)
            {
                _tools.SetMarkerB(mw.PlayerMediaElement.Position);
            }
            SetMarkerBRequested?.Invoke(this, EventArgs.Empty);
        }

        private void BtnClearAB_Click(object sender, RoutedEventArgs e)
        {
            _tools.ClearAbRepeat();
            ClearAbRepeatRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnAbRepeatChanged(TimeSpan? a, TimeSpan? b)
        {
            Dispatcher.BeginInvoke(new Action(() => UpdateAbStatusUi(a, b)));
        }

        private void UpdateAbStatusUi(TimeSpan? a, TimeSpan? b)
        {
            if (TxtAbStatus == null) return;

            if (a.HasValue && b.HasValue)
            {
                TxtAbStatus.Text = $"🔁 Loop: [A: {a.Value:mm\\:ss\\.f} ➡ B: {b.Value:mm\\:ss\\.f}] ACTIVE";
            }
            else if (a.HasValue)
            {
                TxtAbStatus.Text = $"⏳ Loop: [A: {a.Value:mm\\:ss\\.f} ➡ B: --:--] Set point B";
            }
            else
            {
                TxtAbStatus.Text = "Loop Status: Inactive (Set point A to begin loop)";
            }
        }

        private void CmbSkipDuration_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingUi || CmbSkipDuration == null) return;
            if (CmbSkipDuration.SelectedItem is ComboBoxItem item && int.TryParse(item.Tag?.ToString(), out int sec))
            {
                SkipDurationSeconds = sec;
                SkipDurationChanged?.Invoke(this, sec);
                if (Owner is MainWindow mw)
                {
                    mw.ShowOsd($"Skip / Jump Duration: {sec}s");
                }
            }
        }
        #endregion

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
