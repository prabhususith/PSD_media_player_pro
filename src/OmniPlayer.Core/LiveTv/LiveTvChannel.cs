using System;

namespace OmniPlayer.Core.LiveTv
{
    public class LiveTvChannel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public string Language { get; set; } = "English";
        public string Country { get; set; } = "IN";
        public string Resolution { get; set; } = "1080p";
        public string LogoUrl { get; set; } = string.Empty;
        public string StreamUrl { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool IsFavorite { get; set; } = false;

        public string DisplayBadge => $"{Category} • {Language} • {Resolution}";

        public override string ToString() => $"{Name} ({Category} - {Resolution})";
    }
}
