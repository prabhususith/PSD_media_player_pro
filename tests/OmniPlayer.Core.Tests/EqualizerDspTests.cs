using System;
using OmniPlayer.Core.Audio;
using Xunit;

namespace OmniPlayer.Core.Tests
{
    public class EqualizerDspTests
    {
        [Fact]
        public void Standard31Frequencies_ContainsCorrectIsoBands()
        {
            var frequencies = EqualizerDsp.Standard31Frequencies;
            Assert.NotNull(frequencies);
            Assert.Equal(31, frequencies.Length);
            Assert.Equal(20f, frequencies[0]);
            Assert.Equal(800f, frequencies[16]);
            Assert.Equal(1000f, frequencies[17]);
            Assert.Equal(20000f, frequencies[30]);
        }

        [Fact]
        public void FactoryPresets_ContainsRequiredAudioProfiles()
        {
            var dsp = EqualizerDsp.Instance;
            Assert.NotNull(dsp);
            var presets = dsp.FactoryPresets;
            Assert.NotNull(presets);
            Assert.True(presets.ContainsKey("Flat"));
            Assert.True(presets.ContainsKey("Rock"));
            Assert.True(presets.ContainsKey("Pop"));
            Assert.True(presets.ContainsKey("Bass Boost"));
            Assert.True(presets.ContainsKey("Vocal"));
            Assert.True(presets.ContainsKey("Voice Clarity"));
            Assert.True(presets.ContainsKey("Night Mode"));
        }

        [Fact]
        public void PreampDb_ClampsWithinSafeBounds()
        {
            var dsp = EqualizerDsp.Instance;
            dsp.PreampDb = 35.0f;
            Assert.Equal(20.0f, dsp.PreampDb);

            dsp.PreampDb = -35.0f;
            Assert.Equal(-20.0f, dsp.PreampDb);

            dsp.PreampDb = 0.0f;
            Assert.Equal(0.0f, dsp.PreampDb);
        }

        [Fact]
        public void SoundTechnologyMode_EnumsAreAvailable()
        {
            var dsp = EqualizerDsp.Instance;
            dsp.ActiveSoundTechnology = SoundTechnologyMode.DolbyAtmos3D;
            Assert.Equal(SoundTechnologyMode.DolbyAtmos3D, dsp.ActiveSoundTechnology);

            dsp.ActiveSoundTechnology = SoundTechnologyMode.DtsX;
            Assert.Equal(SoundTechnologyMode.DtsX, dsp.ActiveSoundTechnology);

            dsp.ActiveSoundTechnology = SoundTechnologyMode.HeadphoneHRTF;
            Assert.Equal(SoundTechnologyMode.HeadphoneHRTF, dsp.ActiveSoundTechnology);

            dsp.ActiveSoundTechnology = SoundTechnologyMode.StandardStereo;
            Assert.Equal(SoundTechnologyMode.StandardStereo, dsp.ActiveSoundTechnology);
        }

        [Fact]
        public void RoomEchoPreset_EnumsAreAvailable()
        {
            var dsp = EqualizerDsp.Instance;
            dsp.ActiveRoomEcho = RoomEchoPreset.LivingRoom;
            Assert.Equal(RoomEchoPreset.LivingRoom, dsp.ActiveRoomEcho);

            dsp.ActiveRoomEcho = RoomEchoPreset.ConcertHall;
            Assert.Equal(RoomEchoPreset.ConcertHall, dsp.ActiveRoomEcho);

            dsp.ActiveRoomEcho = RoomEchoPreset.None;
            Assert.Equal(RoomEchoPreset.None, dsp.ActiveRoomEcho);
        }

        [Fact]
        public void ProcessAudioBlock_StereoProcessingDoesNotCrash()
        {
            var dsp = EqualizerDsp.Instance;
            int samples = 512;
            float[] left = new float[samples];
            float[] right = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                left[i] = (float)Math.Sin(2.0 * Math.PI * 440.0 * i / 48000.0);
                right[i] = (float)Math.Cos(2.0 * Math.PI * 440.0 * i / 48000.0);
            }

            dsp.IsEqualizerEnabled = true;
            dsp.ActiveSoundTechnology = SoundTechnologyMode.DolbyAtmos3D;
            dsp.NightModeDrcEnabled = true;

            dsp.ProcessAudioBlock(left, right, samples);

            for (int i = 0; i < samples; i++)
            {
                Assert.False(float.IsNaN(left[i]));
                Assert.False(float.IsInfinity(left[i]));
                Assert.InRange(left[i], -2.0f, 2.0f);

                Assert.False(float.IsNaN(right[i]));
                Assert.False(float.IsInfinity(right[i]));
                Assert.InRange(right[i], -2.0f, 2.0f);
            }

            dsp.NightModeDrcEnabled = false;
            dsp.ActiveSoundTechnology = SoundTechnologyMode.StandardStereo;
        }
    }
}
