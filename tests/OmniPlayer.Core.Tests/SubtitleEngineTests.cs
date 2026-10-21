using System;
using OmniPlayer.Core.Subtitles;
using Xunit;

namespace OmniPlayer.Core.Tests
{
    public class SubtitleEngineTests
    {
        [Fact]
        public void ParseSrtOrVtt_ValidSrtContent_ParsesCuesCorrectly()
        {
            string srt = @"1
00:00:01,000 --> 00:00:04,000
Hello, welcome to PSD Media Player Pro.

2
00:00:05,500 --> 00:00:08,200
Enjoy ultra-high-definition audio and video.
";

            var engine = SubtitleEngine.Instance;
            var track = engine.ParseSrtOrVtt(srt, "en", "test.srt");

            Assert.NotNull(track);
            Assert.Equal("en", track.Language);
            Assert.Equal("test.srt", track.Title);
            Assert.Equal(2, track.Cues.Count);

            Assert.Equal(TimeSpan.FromSeconds(1), track.Cues[0].StartTime);
            Assert.Equal(TimeSpan.FromSeconds(4), track.Cues[0].EndTime);
            Assert.Equal("Hello, welcome to PSD Media Player Pro.", track.Cues[0].Text);

            Assert.Equal(TimeSpan.FromMilliseconds(5500), track.Cues[1].StartTime);
            Assert.Equal(TimeSpan.FromMilliseconds(8200), track.Cues[1].EndTime);
        }

        [Fact]
        public void GetCurrentCue_ReturnsMatchingCueAtGivenTime()
        {
            string srt = @"1
00:00:02,000 --> 00:00:05,000
Active subtitle cue.
";

            var engine = SubtitleEngine.Instance;
            var track = engine.ParseSrtOrVtt(srt, "en", "sample.srt");

            var activeCue = track.GetCurrentCue(TimeSpan.FromSeconds(3));
            Assert.NotNull(activeCue);
            Assert.Equal("Active subtitle cue.", activeCue.Text);

            var inactiveCue = track.GetCurrentCue(TimeSpan.FromSeconds(10));
            Assert.Null(inactiveCue);
        }
    }
}
