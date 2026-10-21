using System.Collections.Generic;
using OmniPlayer.Core.LiveTv;
using Xunit;

namespace OmniPlayer.Core.Tests
{
    public class LiveTvManagerTests
    {
        [Fact]
        public void InitialCatalog_IsPopulatedWithCuratedChannels()
        {
            var manager = LiveTvManager.Instance;
            Assert.NotNull(manager);
            var channels = manager.AllChannels;
            Assert.NotNull(channels);
            Assert.NotEmpty(channels);
            Assert.True(channels.Count >= 50);
        }

        [Fact]
        public void GetCategories_ReturnsExpectedCategories()
        {
            var manager = LiveTvManager.Instance;
            var categories = manager.GetCategories();
            Assert.NotNull(categories);
            Assert.Contains(categories, c => c.StartsWith("All"));
            Assert.Contains("Indian National", categories);
            Assert.Contains("Tamil Regional", categories);
            Assert.Contains("Telugu Regional", categories);
            Assert.Contains("World News", categories);
            Assert.Contains("Sports & Action", categories);
        }

        [Fact]
        public void FilterChannels_ByCategory_ReturnsFilteredSubset()
        {
            var manager = LiveTvManager.Instance;
            var nationalChannels = manager.FilterChannels("Indian National", null);
            Assert.NotNull(nationalChannels);
            Assert.NotEmpty(nationalChannels);
            foreach (var ch in nationalChannels)
            {
                Assert.Equal("Indian National", ch.Category);
            }
        }

        [Fact]
        public void FilterChannels_BySearchQuery_ReturnsMatchingResults()
        {
            var manager = LiveTvManager.Instance;
            var results = manager.FilterChannels(null, "Aaj Tak");
            Assert.NotNull(results);
            Assert.NotEmpty(results);
            foreach (var ch in results)
            {
                Assert.Contains("Aaj Tak", ch.Name, System.StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
