using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OmniPlayer.Core.Audio;

namespace OmniPlayer.UI.Views
{
    public partial class EqualizerDialog : Window
    {
        private readonly EqualizerDsp _dsp = EqualizerDsp.Instance;
        private readonly List<Slider> _bandSliders = new();
        private readonly List<TextBlock> _bandValueLabels = new();
        private bool _isUpdatingUi = false;

        public EqualizerDialog()
        {
            InitializeComponent();
            PopulatePresets();
            PopulatePassthroughOptions();
            PopulateSoundTechnologies();
            PopulateRoomEchoPresets();
            RebuildBandsUi();
            UpdateUiFromDsp();
        }

        private void PopulatePresets()
        {
            if (CmbPresets == null) return;
            CmbPresets.Items.Clear();
            foreach (var preset in _dsp.FactoryPresets.Values)
            {
                CmbPresets.Items.Add(preset.Name);
            }
            CmbPresets.SelectedItem = "Flat";
        }

        private void PopulatePassthroughOptions()
        {
            if (CmbPassthrough == null) return;
            CmbPassthrough.Items.Clear();
            foreach (var fmt in Enum.GetValues<BitstreamPassthroughFormat>())
            {
                CmbPassthrough.Items.Add(fmt.ToString().Replace('_', ' '));
            }
            CmbPassthrough.SelectedIndex = (int)_dsp.WasapiPassthrough;
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

        private void RebuildBandsUi()
        {
            if (PanelBands == null || Rb31Band == null) return;

            PanelBands.Children.Clear();
            _bandSliders.Clear();
            _bandValueLabels.Clear();

            bool is31 = Rb31Band.IsChecked == true;
            _dsp.Is31BandMode = is31;

            int count = is31 ? 31 : 10;

            for (int i = 0; i < count; i++)
            {
                int bandIndex = i;
                float freq = is31 ? EqualizerDsp.Standard31Frequencies[i] : EqualizerDsp.Standard31Frequencies[EqualizerDsp.TenBandIndices[i]];
                string freqText = freq >= 1000 ? $"{freq / 1000f:0.#}k" : $"{freq:0}";

                var colPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Width = is31 ? 24 : 65,
                    Margin = new Thickness(is31 ? 2 : 6, 4, is31 ? 2 : 6, 4)
                };

                var txtVal = new TextBlock
                {
                    Text = "0",
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 4)
                };
                txtVal.SetResourceReference(TextBlock.ForegroundProperty, "OmniMutedForegroundBrush");
                _bandValueLabels.Add(txtVal);

                var slider = new Slider
                {
                    Orientation = Orientation.Vertical,
                    Height = 160,
                    Minimum = -20,
                    Maximum = 20,
                    Value = is31 ? _dsp.GetBandGain(bandIndex) : _dsp.GetTenBandGain(bandIndex),
                    SmallChange = 0.5,
                    LargeChange = 2,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                slider.ValueChanged += (s, e) =>
                {
                    if (_isUpdatingUi) return;
                    float gain = (float)e.NewValue;
                    txtVal.Text = $"{gain:+0.#;-0.#;0}";
                    if (is31)
                    {
                        _dsp.SetBandGain(bandIndex, gain);
                    }
                    else
                    {
                        _dsp.SetTenBandGain(bandIndex, gain);
                    }
                };

                _bandSliders.Add(slider);

                var txtFreq = new TextBlock
                {
                    Text = freqText,
                    FontSize = is31 ? 9 : 11,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 6, 0, 0)
                };
                txtFreq.SetResourceReference(TextBlock.ForegroundProperty, "OmniForegroundBrush");

                colPanel.Children.Add(txtVal);
                colPanel.Children.Add(slider);
                colPanel.Children.Add(txtFreq);

                PanelBands.Children.Add(colPanel);
            }
        }

        private void CmbPresets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingUi) return;
            if (CmbPresets.SelectedItem is string presetName)
            {
                _dsp.ApplyPreset(presetName);
                UpdateUiFromDsp();
            }
        }

        private void UpdateUiFromDsp()
        {
            if (SliderPreamp == null || TxtPreampVal == null || PanelBands == null || Rb31Band == null) return;
            _isUpdatingUi = true;
            try
            {
                SliderPreamp.Value = _dsp.PreampDb;
                TxtPreampVal.Text = $"{_dsp.PreampDb:+0.0;-0.0;0.0} dB";

                bool is31 = Rb31Band.IsChecked == true;
                for (int i = 0; i < _bandSliders.Count; i++)
                {
                    float gain = is31 ? _dsp.GetBandGain(i) : _dsp.GetTenBandGain(i);
                    _bandSliders[i].Value = gain;
                    if (i < _bandValueLabels.Count)
                    {
                        _bandValueLabels[i].Text = $"{gain:+0.#;-0.#;0}";
                    }
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
                }
                if (TxtReverbWetVal != null)
                {
                    TxtReverbWetVal.Text = $"{(int)(_dsp.ReverbWetMix * 100.0f)}%";
                }

                if (SliderPitch != null)
                {
                    SliderPitch.Value = _dsp.PitchSemitones;
                }
                if (TxtPitchVal != null)
                {
                    TxtPitchVal.Text = $"{_dsp.PitchSemitones:+0;-0;0} st";
                }

                if (ChkKaraoke != null) ChkKaraoke.IsChecked = _dsp.KaraokeVocalRemoverEnabled;
                if (ChkDialogueClarity != null) ChkDialogueClarity.IsChecked = _dsp.DialogueClarityEnabled;
                if (ChkVoiceNorm != null) ChkVoiceNorm.IsChecked = _dsp.VoiceNormalizationEnabled;
                if (ChkNightMode != null) ChkNightMode.IsChecked = _dsp.NightModeDrcEnabled;
                if (ChkSurround != null) ChkSurround.IsChecked = _dsp.SpatialSurroundEnabled;
                if (ChkPitchPreserve != null) ChkPitchPreserve.IsChecked = _dsp.PitchPreserverEnabled;

                if (CmbPassthrough != null)
                {
                    CmbPassthrough.SelectedIndex = (int)_dsp.WasapiPassthrough;
                }
            }
            finally
            {
                _isUpdatingUi = false;
            }
        }

        private void SliderPreamp_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingUi || _dsp == null) return;
            _dsp.PreampDb = (float)e.NewValue;
            if (TxtPreampVal != null)
            {
                TxtPreampVal.Text = $"{_dsp.PreampDb:+0.0;-0.0;0.0} dB";
            }
        }

        private void BandMode_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded || PanelBands == null) return;
            RebuildBandsUi();
            UpdateUiFromDsp();
        }

        private void ChkEqEnable_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded || PanelBands == null || _dsp == null) return;
            _dsp.IsEqualizerEnabled = ChkEqEnable.IsChecked == true;
            PanelBands.IsEnabled = _dsp.IsEqualizerEnabled;
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            if (CmbPresets != null) CmbPresets.SelectedItem = "Flat";
            _dsp.ApplyPreset("Flat");
            UpdateUiFromDsp();
        }

        private void CmbSoundTechnology_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingUi || CmbSoundTechnology == null || _dsp == null) return;
            if (CmbSoundTechnology.SelectedIndex >= 0)
            {
                _dsp.ActiveSoundTechnology = (SoundTechnologyMode)CmbSoundTechnology.SelectedIndex;
            }
        }

        private void CmbRoomEcho_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingUi || CmbRoomEcho == null || _dsp == null) return;
            if (CmbRoomEcho.SelectedIndex >= 0)
            {
                _dsp.ActiveRoomEcho = (RoomEchoPreset)CmbRoomEcho.SelectedIndex;
            }
        }

        private void SliderReverbWet_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingUi || _dsp == null) return;
            float wet = (float)(e.NewValue / 100.0);
            _dsp.ReverbWetMix = wet;
            if (TxtReverbWetVal != null)
            {
                TxtReverbWetVal.Text = $"{(int)e.NewValue}%";
            }
        }

        private void SliderPitch_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingUi || _dsp == null) return;
            int semitones = (int)Math.Round(e.NewValue);
            _dsp.PitchSemitones = semitones;
            if (TxtPitchVal != null)
            {
                TxtPitchVal.Text = $"{semitones:+0;-0;0} st";
            }
        }

        private void BtnResetPitch_Click(object sender, RoutedEventArgs e)
        {
            if (_dsp == null) return;
            _dsp.PitchSemitones = 0;
            _dsp.PitchCents = 0.0f;
            if (SliderPitch != null) SliderPitch.Value = 0;
            if (TxtPitchVal != null) TxtPitchVal.Text = "0 st";
        }

        private void DspOption_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded || _dsp == null) return;
            _dsp.VoiceNormalizationEnabled = ChkVoiceNorm?.IsChecked == true;
            _dsp.NightModeDrcEnabled = ChkNightMode?.IsChecked == true;
            _dsp.SpatialSurroundEnabled = ChkSurround?.IsChecked == true;
            _dsp.PitchPreserverEnabled = ChkPitchPreserve?.IsChecked == true;
            _dsp.KaraokeVocalRemoverEnabled = ChkKaraoke?.IsChecked == true;
            _dsp.DialogueClarityEnabled = ChkDialogueClarity?.IsChecked == true;
        }

        private void CmbPassthrough_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingUi || CmbPassthrough == null || _dsp == null) return;
            if (CmbPassthrough.SelectedIndex >= 0)
            {
                _dsp.WasapiPassthrough = (BitstreamPassthroughFormat)CmbPassthrough.SelectedIndex;
            }
        }
    }
}
