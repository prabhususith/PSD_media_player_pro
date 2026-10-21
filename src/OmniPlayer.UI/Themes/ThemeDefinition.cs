using System;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace OmniPlayer.UI.Themes
{
    public class ThemeDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public string Description { get; set; } = string.Empty;

        // Core Palette
        public string BackgroundColorHex { get; set; } = "#121214";
        public string SurfaceColorHex { get; set; } = "#1A1A1E";
        public string HeaderColorHex { get; set; } = "#161619";
        public string BorderColorHex { get; set; } = "#2C2C34";
        public string AccentColorHex { get; set; } = "#0078D4";
        public string AccentHoverColorHex { get; set; } = "#1E90FF";
        public string ForegroundColorHex { get; set; } = "#FFFFFF";
        public string MutedForegroundColorHex { get; set; } = "#8A8A96";
        public string ControlBackgroundColorHex { get; set; } = "#222228";
        public string ControlHoverColorHex { get; set; } = "#2E2E36";
        public string SliderTrackColorHex { get; set; } = "#3E3E48";
        public string SliderThumbColorHex { get; set; } = "#0078D4";
        public string DialogBackgroundColorHex { get; set; } = "#1E1E24";

        // Styling parameters
        public double CornerRadius { get; set; } = 6.0;
        public double GlassBlurOpacity { get; set; } = 0.95;
        public bool GlowEffect { get; set; } = false;
        public string GlowColorHex { get; set; } = "#0078D4";
        public bool IsDark { get; set; } = true;
        public string FontPrimary { get; set; } = "Segoe UI Variable Text, Segoe UI, sans-serif";

        public ResourceDictionary ToResourceDictionary()
        {
            var dict = new ResourceDictionary();

            void AddBrush(string key, string hex)
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                dict[key] = brush;
            }

            AddBrush("OmniBackgroundBrush", BackgroundColorHex);
            AddBrush("OmniSurfaceBrush", SurfaceColorHex);
            AddBrush("OmniHeaderBrush", HeaderColorHex);
            AddBrush("OmniBorderBrush", BorderColorHex);
            AddBrush("OmniAccentBrush", AccentColorHex);
            AddBrush("OmniAccentHoverBrush", AccentHoverColorHex);
            AddBrush("OmniForegroundBrush", ForegroundColorHex);
            AddBrush("OmniMutedForegroundBrush", MutedForegroundColorHex);
            AddBrush("OmniControlBackgroundBrush", ControlBackgroundColorHex);
            AddBrush("OmniControlHoverBrush", ControlHoverColorHex);
            AddBrush("OmniSliderTrackBrush", SliderTrackColorHex);
            AddBrush("OmniSliderThumbBrush", SliderThumbColorHex);
            AddBrush("OmniDialogBackgroundBrush", DialogBackgroundColorHex);

            dict["OmniCornerRadius"] = new CornerRadius(CornerRadius);
            dict["OmniGlowColor"] = (Color)ColorConverter.ConvertFromString(GlowColorHex);
            dict["OmniGlassBlurOpacity"] = GlassBlurOpacity;

            return dict;
        }

        public string ToXamlManifest()
        {
            return $"""
            <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                                xmlns:sys="clr-namespace:System;assembly=mscorlib">
                <!-- Omni Player Theme Manifest: {Name} ({Category}) -->
                <sys:String x:Key="ThemeId">{Id}</sys:String>
                <sys:String x:Key="ThemeName">{Name}</sys:String>
                <sys:String x:Key="ThemeCategory">{Category}</sys:String>
                <sys:Double x:Key="GlassBlurOpacity">{GlassBlurOpacity}</sys:Double>
                <CornerRadius x:Key="OmniCornerRadius">{CornerRadius}</CornerRadius>

                <SolidColorBrush x:Key="OmniBackgroundBrush" Color="{BackgroundColorHex}" />
                <SolidColorBrush x:Key="OmniSurfaceBrush" Color="{SurfaceColorHex}" />
                <SolidColorBrush x:Key="OmniHeaderBrush" Color="{HeaderColorHex}" />
                <SolidColorBrush x:Key="OmniBorderBrush" Color="{BorderColorHex}" />
                <SolidColorBrush x:Key="OmniAccentBrush" Color="{AccentColorHex}" />
                <SolidColorBrush x:Key="OmniAccentHoverBrush" Color="{AccentHoverColorHex}" />
                <SolidColorBrush x:Key="OmniForegroundBrush" Color="{ForegroundColorHex}" />
                <SolidColorBrush x:Key="OmniMutedForegroundBrush" Color="{MutedForegroundColorHex}" />
                <SolidColorBrush x:Key="OmniControlBackgroundBrush" Color="{ControlBackgroundColorHex}" />
                <SolidColorBrush x:Key="OmniControlHoverBrush" Color="{ControlHoverColorHex}" />
                <SolidColorBrush x:Key="OmniSliderTrackBrush" Color="{SliderTrackColorHex}" />
                <SolidColorBrush x:Key="OmniSliderThumbBrush" Color="{SliderThumbColorHex}" />
                <SolidColorBrush x:Key="OmniDialogBackgroundBrush" Color="{DialogBackgroundColorHex}" />
            </ResourceDictionary>
            """;
        }

        public string ToCssVariables()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"/* Omni Player Skin CSS: {Name} */");
            sb.AppendLine(":root {");
            sb.AppendLine($"  --omni-bg: {BackgroundColorHex};");
            sb.AppendLine($"  --omni-surface: {SurfaceColorHex};");
            sb.AppendLine($"  --omni-header: {HeaderColorHex};");
            sb.AppendLine($"  --omni-border: {BorderColorHex};");
            sb.AppendLine($"  --omni-accent: {AccentColorHex};");
            sb.AppendLine($"  --omni-accent-hover: {AccentHoverColorHex};");
            sb.AppendLine($"  --omni-fg: {ForegroundColorHex};");
            sb.AppendLine($"  --omni-muted-fg: {MutedForegroundColorHex};");
            sb.AppendLine($"  --omni-ctrl-bg: {ControlBackgroundColorHex};");
            sb.AppendLine($"  --omni-ctrl-hover: {ControlHoverColorHex};");
            sb.AppendLine($"  --omni-slider-track: {SliderTrackColorHex};");
            sb.AppendLine($"  --omni-slider-thumb: {SliderThumbColorHex};");
            sb.AppendLine($"  --omni-dialog-bg: {DialogBackgroundColorHex};");
            sb.AppendLine($"  --omni-radius: {CornerRadius}px;");
            sb.AppendLine($"  --omni-glass-opacity: {GlassBlurOpacity};");
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
