using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace OmniPlayer.UI.Themes
{
    public class ThemeManager
    {
        private static readonly Lazy<ThemeManager> _instance = new(() => new ThemeManager());
        public static ThemeManager Instance => _instance.Value;

        private readonly Dictionary<string, ThemeDefinition> _themes = new(StringComparer.OrdinalIgnoreCase);
        private ThemeDefinition _currentTheme = null!;

        public event Action<ThemeDefinition>? ThemeChanged;

        public ThemeDefinition CurrentTheme => _currentTheme;
        public IReadOnlyList<ThemeDefinition> AllThemes => _themes.Values.ToList();

        public ThemeManager()
        {
            RegisterAllThemes();
            _currentTheme = _themes["kmplayer-carbon"];
        }

        public IEnumerable<ThemeDefinition> GetThemesByCategory(string category)
        {
            return _themes.Values.Where(t => string.Equals(t.Category, category, StringComparison.OrdinalIgnoreCase));
        }

        public bool ApplyTheme(string themeId)
        {
            if (!_themes.TryGetValue(themeId, out var theme))
                return false;

            _currentTheme = theme;

            try
            {
                var dict = theme.ToResourceDictionary();
                if (Application.Current != null)
                {
                    // Merge or replace existing dynamic theme dictionary
                    var merged = Application.Current.Resources.MergedDictionaries;
                    var existing = merged.FirstOrDefault(d => d.Contains("OmniBackgroundBrush"));
                    if (existing != null)
                    {
                        merged.Remove(existing);
                    }
                    merged.Add(dict);
                }
            }
            catch
            {
                // Headless/Unit test fallback
            }

            ThemeChanged?.Invoke(_currentTheme);
            return true;
        }

        public void ApplyNextTheme()
        {
            var list = _themes.Keys.ToList();
            if (list.Count == 0) return;
            int idx = list.IndexOf(_currentTheme.Id);
            int nextIdx = (idx + 1) % list.Count;
            ApplyTheme(list[nextIdx]);
        }

        public void ExportAllPacksToDisk(string directoryPath)
        {
            Directory.CreateDirectory(directoryPath);
            foreach (var theme in _themes.Values)
            {
                string filePath = Path.Combine(directoryPath, $"{theme.Id}.xaml");
                File.WriteAllText(filePath, theme.ToXamlManifest());
            }
        }

        private void Register(ThemeDefinition t)
        {
            _themes[t.Id] = t;
        }

        private void RegisterAllThemes()
        {
            // =========================================================================
            // CATEGORY 1: CLASSIC PLAYER ARCHETYPES (25 Themes)
            // =========================================================================
            Register(new ThemeDefinition
            {
                Id = "kmplayer-carbon",
                Name = "KMPlayer Carbon",
                Category = "Classic Player Archetypes",
                Description = "Sleek carbon-fiber brushed dark aesthetic inspired by classic KMPlayer",
                BackgroundColorHex = "#141416",
                SurfaceColorHex = "#1D1E22",
                HeaderColorHex = "#18191D",
                BorderColorHex = "#2E3038",
                AccentColorHex = "#805AD5",
                AccentHoverColorHex = "#9F7AEA",
                ForegroundColorHex = "#E2E8F0",
                MutedForegroundColorHex = "#A0AEC0",
                ControlBackgroundColorHex = "#25272E",
                ControlHoverColorHex = "#32353E",
                SliderTrackColorHex = "#3A3D47",
                SliderThumbColorHex = "#805AD5",
                DialogBackgroundColorHex = "#1A1B20"
            });

            Register(new ThemeDefinition
            {
                Id = "potplayer-black",
                Name = "PotPlayer Black",
                Category = "Classic Player Archetypes",
                Description = "Minimalist matte black styling inspired by PotPlayer default pro skin",
                BackgroundColorHex = "#0C0C0D",
                SurfaceColorHex = "#161618",
                HeaderColorHex = "#111113",
                BorderColorHex = "#252528",
                AccentColorHex = "#E53E3E",
                AccentHoverColorHex = "#FC8181",
                ForegroundColorHex = "#F7FAFC",
                MutedForegroundColorHex = "#718096",
                ControlBackgroundColorHex = "#1E1E22",
                ControlHoverColorHex = "#2B2B30",
                SliderTrackColorHex = "#36363C",
                SliderThumbColorHex = "#E53E3E",
                DialogBackgroundColorHex = "#151517"
            });

            Register(new ThemeDefinition
            {
                Id = "vlc-modern-orange",
                Name = "VLC Modern Orange",
                Category = "Classic Player Archetypes",
                Description = "Iconic traffic-cone orange and dark graphite blend",
                BackgroundColorHex = "#18181B",
                SurfaceColorHex = "#27272A",
                HeaderColorHex = "#202024",
                BorderColorHex = "#3F3F46",
                AccentColorHex = "#F97316",
                AccentHoverColorHex = "#FB923C",
                ForegroundColorHex = "#FAFAFA",
                MutedForegroundColorHex = "#A1A1AA",
                ControlBackgroundColorHex = "#2F2F35",
                ControlHoverColorHex = "#3E3E46",
                SliderTrackColorHex = "#52525B",
                SliderThumbColorHex = "#F97316",
                DialogBackgroundColorHex = "#242428"
            });

            Register(new ThemeDefinition
            {
                Id = "gom-minimal-orange",
                Name = "GOM Minimal",
                Category = "Classic Player Archetypes",
                Description = "Clean studio dark with warm amber glow from GOM classic",
                BackgroundColorHex = "#17181A",
                SurfaceColorHex = "#222428",
                HeaderColorHex = "#1C1E22",
                BorderColorHex = "#353840",
                AccentColorHex = "#FF8C00",
                AccentHoverColorHex = "#FFA500",
                ForegroundColorHex = "#ECEFF4",
                MutedForegroundColorHex = "#8892B0",
                ControlBackgroundColorHex = "#2B2E34",
                ControlHoverColorHex = "#3A3E46",
                SliderTrackColorHex = "#494E58",
                SliderThumbColorHex = "#FF8C00",
                DialogBackgroundColorHex = "#1F2125"
            });

            Register(new ThemeDefinition
            {
                Id = "mpc-retro-silver",
                Name = "MPC Retro Silver",
                Category = "Classic Player Archetypes",
                Description = "Media Player Classic industrial silver-slate brushed look",
                BackgroundColorHex = "#2B2D30",
                SurfaceColorHex = "#383B40",
                HeaderColorHex = "#32353A",
                BorderColorHex = "#4E525A",
                AccentColorHex = "#4A90E2",
                AccentHoverColorHex = "#6BA4EC",
                ForegroundColorHex = "#F0F2F5",
                MutedForegroundColorHex = "#A0A6B2",
                ControlBackgroundColorHex = "#42464D",
                ControlHoverColorHex = "#50545E",
                SliderTrackColorHex = "#5C616C",
                SliderThumbColorHex = "#4A90E2",
                DialogBackgroundColorHex = "#33363B"
            });

            Register(new ThemeDefinition
            {
                Id = "winamp-classic-gold",
                Name = "Winamp Classic Gold",
                Category = "Classic Player Archetypes",
                Description = "1998 nostalgic Winamp green-gold LED and dark graphite",
                BackgroundColorHex = "#0F1210",
                SurfaceColorHex = "#1A1F1B",
                HeaderColorHex = "#141815",
                BorderColorHex = "#2E3B30",
                AccentColorHex = "#10B981",
                AccentHoverColorHex = "#34D399",
                ForegroundColorHex = "#E6FFFA",
                MutedForegroundColorHex = "#6EE7B7",
                ControlBackgroundColorHex = "#232B25",
                ControlHoverColorHex = "#2E3830",
                SliderTrackColorHex = "#3B473D",
                SliderThumbColorHex = "#F59E0B",
                DialogBackgroundColorHex = "#181D19"
            });

            Register(new ThemeDefinition
            {
                Id = "jetaudio-titanium",
                Name = "JetAudio Titanium",
                Category = "Classic Player Archetypes",
                Description = "Metallic titanium casing with deep cyan LED indicators",
                BackgroundColorHex = "#1B1D20",
                SurfaceColorHex = "#24272C",
                HeaderColorHex = "#1F2226",
                BorderColorHex = "#383D45",
                AccentColorHex = "#06B6D4",
                AccentHoverColorHex = "#22D3EE",
                ForegroundColorHex = "#F1F5F9",
                MutedForegroundColorHex = "#94A3B8",
                ControlBackgroundColorHex = "#2E3238",
                ControlHoverColorHex = "#3A3F47",
                SliderTrackColorHex = "#474D57",
                SliderThumbColorHex = "#06B6D4",
                DialogBackgroundColorHex = "#22252A"
            });

            Register(new ThemeDefinition
            {
                Id = "quicktime-dark-onyx",
                Name = "QuickTime Dark Onyx",
                Category = "Classic Player Archetypes",
                Description = "Polished obsidian glass with silver highlights inspired by QuickTime 7",
                BackgroundColorHex = "#101012",
                SurfaceColorHex = "#1B1B1E",
                HeaderColorHex = "#151517",
                BorderColorHex = "#2C2C32",
                AccentColorHex = "#3B82F6",
                AccentHoverColorHex = "#60A5FA",
                ForegroundColorHex = "#F8FAFC",
                MutedForegroundColorHex = "#64748B",
                ControlBackgroundColorHex = "#24242A",
                ControlHoverColorHex = "#303038",
                SliderTrackColorHex = "#3D3D48",
                SliderThumbColorHex = "#3B82F6",
                DialogBackgroundColorHex = "#18181B"
            });

            Register(new ThemeDefinition
            {
                Id = "bsplayer-slate",
                Name = "BSPlayer Slate",
                Category = "Classic Player Archetypes",
                Description = "Deep slate blue with vibrant green status elements",
                BackgroundColorHex = "#151B24",
                SurfaceColorHex = "#1E2733",
                HeaderColorHex = "#19212C",
                BorderColorHex = "#2F3D4F",
                AccentColorHex = "#22C55E",
                AccentHoverColorHex = "#4ADE80",
                ForegroundColorHex = "#F0FDF4",
                MutedForegroundColorHex = "#86EFAC",
                ControlBackgroundColorHex = "#273343",
                ControlHoverColorHex = "#324154",
                SliderTrackColorHex = "#3D4F66",
                SliderThumbColorHex = "#22C55E",
                DialogBackgroundColorHex = "#1C2430"
            });

            Register(new ThemeDefinition
            {
                Id = "realplayer-neo-cyan",
                Name = "RealPlayer Neo Cyan",
                Category = "Classic Player Archetypes",
                Description = "Retro-modern teal and midnight blue multimedia vibe",
                BackgroundColorHex = "#0E171E",
                SurfaceColorHex = "#16232D",
                HeaderColorHex = "#111C25",
                BorderColorHex = "#243949",
                AccentColorHex = "#14B8A6",
                AccentHoverColorHex = "#2DD4BF",
                ForegroundColorHex = "#F0FDFA",
                MutedForegroundColorHex = "#5EEAD4",
                ControlBackgroundColorHex = "#1E2F3D",
                ControlHoverColorHex = "#273C4D",
                SliderTrackColorHex = "#324A5E",
                SliderThumbColorHex = "#14B8A6",
                DialogBackgroundColorHex = "#131E27"
            });

            Register(new ThemeDefinition
            {
                Id = "powerdvd-cinematic",
                Name = "CyberLink PowerDVD Dark",
                Category = "Classic Player Archetypes",
                Description = "Cinematic deep navy and neon sapphire tone for home theaters",
                BackgroundColorHex = "#0A0E17",
                SurfaceColorHex = "#121A29",
                HeaderColorHex = "#0E1420",
                BorderColorHex = "#1E2C44",
                AccentColorHex = "#2563EB",
                AccentHoverColorHex = "#3B82F6",
                ForegroundColorHex = "#EFF6FF",
                MutedForegroundColorHex = "#93C5FD",
                ControlBackgroundColorHex = "#192438",
                ControlHoverColorHex = "#22314C",
                SliderTrackColorHex = "#2C3E60",
                SliderThumbColorHex = "#2563EB",
                DialogBackgroundColorHex = "#101724"
            });

            Register(new ThemeDefinition
            {
                Id = "foobar-vintage-black",
                Name = "Foobar Vintage",
                Category = "Classic Player Archetypes",
                Description = "Industrial technical audio aesthetic with clean white text",
                BackgroundColorHex = "#121212",
                SurfaceColorHex = "#1C1C1C",
                HeaderColorHex = "#161616",
                BorderColorHex = "#303030",
                AccentColorHex = "#94A3B8",
                AccentHoverColorHex = "#CBD5E1",
                ForegroundColorHex = "#FFFFFF",
                MutedForegroundColorHex = "#71717A",
                ControlBackgroundColorHex = "#262626",
                ControlHoverColorHex = "#333333",
                SliderTrackColorHex = "#404040",
                SliderThumbColorHex = "#94A3B8",
                DialogBackgroundColorHex = "#181818"
            });

            Register(new ThemeDefinition
            {
                Id = "aimp-obsidian-amber",
                Name = "AIMP Obsidian",
                Category = "Classic Player Archetypes",
                Description = "Dark charcoal with glowing amber meters from AIMP",
                BackgroundColorHex = "#131416",
                SurfaceColorHex = "#1C1E22",
                HeaderColorHex = "#17191C",
                BorderColorHex = "#2F323A",
                AccentColorHex = "#D97706",
                AccentHoverColorHex = "#F59E0B",
                ForegroundColorHex = "#FFFBEB",
                MutedForegroundColorHex = "#FCD34D",
                ControlBackgroundColorHex = "#26282E",
                ControlHoverColorHex = "#32353D",
                SliderTrackColorHex = "#3F434D",
                SliderThumbColorHex = "#D97706",
                DialogBackgroundColorHex = "#1A1C20"
            });

            Register(new ThemeDefinition
            {
                Id = "mediamonkey-gold",
                Name = "MediaMonkey Gold",
                Category = "Classic Player Archetypes",
                Description = "Luxurious gold trim on dark espresso background",
                BackgroundColorHex = "#141210",
                SurfaceColorHex = "#1F1C18",
                HeaderColorHex = "#191613",
                BorderColorHex = "#332E27",
                AccentColorHex = "#EAB308",
                AccentHoverColorHex = "#FACC15",
                ForegroundColorHex = "#FEF9C3",
                MutedForegroundColorHex = "#CA8A04",
                ControlBackgroundColorHex = "#2B2620",
                ControlHoverColorHex = "#38322A",
                SliderTrackColorHex = "#473F35",
                SliderThumbColorHex = "#EAB308",
                DialogBackgroundColorHex = "#1B1814"
            });

            Register(new ThemeDefinition
            {
                Id = "smplayer-fusion",
                Name = "SMPlayer Fusion",
                Category = "Classic Player Archetypes",
                Description = "Cross-platform Qt Fusion dark theme with blue accents",
                BackgroundColorHex = "#1F232A",
                SurfaceColorHex = "#2A303A",
                HeaderColorHex = "#242932",
                BorderColorHex = "#3E4654",
                AccentColorHex = "#38BDF8",
                AccentHoverColorHex = "#7DD3FC",
                ForegroundColorHex = "#F0F9FF",
                MutedForegroundColorHex = "#BAE6FD",
                ControlBackgroundColorHex = "#353D4A",
                ControlHoverColorHex = "#424C5C",
                SliderTrackColorHex = "#515D70",
                SliderThumbColorHex = "#38BDF8",
                DialogBackgroundColorHex = "#262B34"
            });

            Register(new ThemeDefinition
            {
                Id = "zoomplayer-graphite",
                Name = "ZoomPlayer Graphite",
                Category = "Classic Player Archetypes",
                Description = "Professional graphite cockpit layout with red-orange markers",
                BackgroundColorHex = "#181A1D",
                SurfaceColorHex = "#22252A",
                HeaderColorHex = "#1C1F23",
                BorderColorHex = "#343942",
                AccentColorHex = "#EA580C",
                AccentHoverColorHex = "#F97316",
                ForegroundColorHex = "#FFF7ED",
                MutedForegroundColorHex = "#FDBA74",
                ControlBackgroundColorHex = "#2B2F36",
                ControlHoverColorHex = "#373C45",
                SliderTrackColorHex = "#464C57",
                SliderThumbColorHex = "#EA580C",
                DialogBackgroundColorHex = "#1F2227"
            });

            Register(new ThemeDefinition
            {
                Id = "divx-player-pro",
                Name = "DivX Player Pro",
                Category = "Classic Player Archetypes",
                Description = "High-definition video pioneer dark blue and silver theme",
                BackgroundColorHex = "#0C131F",
                SurfaceColorHex = "#142033",
                HeaderColorHex = "#101929",
                BorderColorHex = "#203352",
                AccentColorHex = "#0284C7",
                AccentHoverColorHex = "#0EA5E9",
                ForegroundColorHex = "#F0F9FF",
                MutedForegroundColorHex = "#7DD3FC",
                ControlBackgroundColorHex = "#1B2C47",
                ControlHoverColorHex = "#233A5C",
                SliderTrackColorHex = "#2C4974",
                SliderThumbColorHex = "#0284C7",
                DialogBackgroundColorHex = "#121D2E"
            });

            Register(new ThemeDefinition
            {
                Id = "wmp11-onyx",
                Name = "Windows Media Player 11 Onyx",
                Category = "Classic Player Archetypes",
                Description = "Windows Vista WMP 11 glossy dark onyx with electric cyan glow",
                BackgroundColorHex = "#0F1115",
                SurfaceColorHex = "#181C22",
                HeaderColorHex = "#13161B",
                BorderColorHex = "#272D37",
                AccentColorHex = "#00D2FF",
                AccentHoverColorHex = "#54E3FF",
                ForegroundColorHex = "#FFFFFF",
                MutedForegroundColorHex = "#8EA3BE",
                ControlBackgroundColorHex = "#212730",
                ControlHoverColorHex = "#2D3542",
                SliderTrackColorHex = "#3B4556",
                SliderThumbColorHex = "#00D2FF",
                DialogBackgroundColorHex = "#161920"
            });

            Register(new ThemeDefinition
            {
                Id = "wmp12-aero",
                Name = "WMP 12 Aero Glass",
                Category = "Classic Player Archetypes",
                Description = "Windows 7 Aero Glass translucent frosted slate and cobalt",
                BackgroundColorHex = "#1A222C",
                SurfaceColorHex = "#24303E",
                HeaderColorHex = "#1E2834",
                BorderColorHex = "#384A60",
                AccentColorHex = "#3B82F6",
                AccentHoverColorHex = "#60A5FA",
                ForegroundColorHex = "#F8FAFC",
                MutedForegroundColorHex = "#93C5FD",
                ControlBackgroundColorHex = "#2F3E50",
                ControlHoverColorHex = "#3C4E64",
                SliderTrackColorHex = "#4A607B",
                SliderThumbColorHex = "#3B82F6",
                DialogBackgroundColorHex = "#212B37"
            });

            Register(new ThemeDefinition
            {
                Id = "corel-windvd",
                Name = "Corel WinDVD Cinema",
                Category = "Classic Player Archetypes",
                Description = "Rich theater burgundy and warm crimson glow",
                BackgroundColorHex = "#180F12",
                SurfaceColorHex = "#26171C",
                HeaderColorHex = "#1E1216",
                BorderColorHex = "#40252E",
                AccentColorHex = "#E11D48",
                AccentHoverColorHex = "#F43F5E",
                ForegroundColorHex = "#FFF1F2",
                MutedForegroundColorHex = "#FDA4AF",
                ControlBackgroundColorHex = "#331F26",
                ControlHoverColorHex = "#422832",
                SliderTrackColorHex = "#54333F",
                SliderThumbColorHex = "#E11D48",
                DialogBackgroundColorHex = "#211418"
            });

            Register(new ThemeDefinition
            {
                Id = "itunes-retro-dark",
                Name = "iTunes Retro Dark",
                Category = "Classic Player Archetypes",
                Description = "Brushed aluminum dark mode inspired by classic iTunes",
                BackgroundColorHex = "#1A1A1A",
                SurfaceColorHex = "#262626",
                HeaderColorHex = "#202020",
                BorderColorHex = "#3D3D3D",
                AccentColorHex = "#EC4899",
                AccentHoverColorHex = "#F472B6",
                ForegroundColorHex = "#FDF2F8",
                MutedForegroundColorHex = "#F9A8D4",
                ControlBackgroundColorHex = "#333333",
                ControlHoverColorHex = "#424242",
                SliderTrackColorHex = "#545454",
                SliderThumbColorHex = "#EC4899",
                DialogBackgroundColorHex = "#222222"
            });

            Register(new ThemeDefinition
            {
                Id = "songbird-dark",
                Name = "Songbird Dusk",
                Category = "Classic Player Archetypes",
                Description = "Deep plum and purple highlights from open-source Songbird",
                BackgroundColorHex = "#16121C",
                SurfaceColorHex = "#231C2D",
                HeaderColorHex = "#1B1623",
                BorderColorHex = "#392D48",
                AccentColorHex = "#A855F7",
                AccentHoverColorHex = "#C084FC",
                ForegroundColorHex = "#FAF5FF",
                MutedForegroundColorHex = "#D8B4FE",
                ControlBackgroundColorHex = "#2F253C",
                ControlHoverColorHex = "#3D304E",
                SliderTrackColorHex = "#4E3E64",
                SliderThumbColorHex = "#A855F7",
                DialogBackgroundColorHex = "#1F1827"
            });

            Register(new ThemeDefinition
            {
                Id = "clementine-dusk",
                Name = "Clementine Dusk",
                Category = "Classic Player Archetypes",
                Description = "Tangerine orange trim on soft dark blue dusk background",
                BackgroundColorHex = "#121820",
                SurfaceColorHex = "#1B2430",
                HeaderColorHex = "#161D27",
                BorderColorHex = "#2A384A",
                AccentColorHex = "#F97316",
                AccentHoverColorHex = "#FB923C",
                ForegroundColorHex = "#FFF7ED",
                MutedForegroundColorHex = "#FDBA74",
                ControlBackgroundColorHex = "#232F3F",
                ControlHoverColorHex = "#2E3D52",
                SliderTrackColorHex = "#3B4E68",
                SliderThumbColorHex = "#F97316",
                DialogBackgroundColorHex = "#18202A"
            });

            Register(new ThemeDefinition
            {
                Id = "audacious-classic",
                Name = "Audacious Classic",
                Category = "Classic Player Archetypes",
                Description = "Lightweight GTK dark neutral grey theme",
                BackgroundColorHex = "#1E1E1E",
                SurfaceColorHex = "#282828",
                HeaderColorHex = "#232323",
                BorderColorHex = "#404040",
                AccentColorHex = "#60A5FA",
                AccentHoverColorHex = "#93C5FD",
                ForegroundColorHex = "#FFFFFF",
                MutedForegroundColorHex = "#9CA3AF",
                ControlBackgroundColorHex = "#333333",
                ControlHoverColorHex = "#424242",
                SliderTrackColorHex = "#545454",
                SliderThumbColorHex = "#60A5FA",
                DialogBackgroundColorHex = "#252525"
            });

            Register(new ThemeDefinition
            {
                Id = "xmplay-matrix",
                Name = "XMPlay Matrix",
                Category = "Classic Player Archetypes",
                Description = "Retro chiptune player phosphor green terminal look",
                BackgroundColorHex = "#08100A",
                SurfaceColorHex = "#0F1E13",
                HeaderColorHex = "#0B160E",
                BorderColorHex = "#1A3622",
                AccentColorHex = "#22C55E",
                AccentHoverColorHex = "#4ADE80",
                ForegroundColorHex = "#DCFCE7",
                MutedForegroundColorHex = "#86EFAC",
                ControlBackgroundColorHex = "#152A1B",
                ControlHoverColorHex = "#1C3723",
                SliderTrackColorHex = "#264B30",
                SliderThumbColorHex = "#22C55E",
                DialogBackgroundColorHex = "#0D1A10"
            });

            // =========================================================================
            // CATEGORY 2: MODERN FLAT & FLUENT (35 Themes)
            // =========================================================================
            Register(new ThemeDefinition
            {
                Id = "obsidian-dark",
                Name = "Obsidian Dark",
                Category = "Modern Flat & Fluent",
                Description = "Deep pitch obsidian with ultra-clean modern slate accents",
                BackgroundColorHex = "#0B0C0E",
                SurfaceColorHex = "#15171C",
                HeaderColorHex = "#101115",
                BorderColorHex = "#242730",
                AccentColorHex = "#6366F1",
                AccentHoverColorHex = "#818CF8",
                ForegroundColorHex = "#F8FAFC",
                MutedForegroundColorHex = "#94A3B8",
                ControlBackgroundColorHex = "#1E2128",
                ControlHoverColorHex = "#282C36",
                SliderTrackColorHex = "#353A47",
                SliderThumbColorHex = "#6366F1",
                DialogBackgroundColorHex = "#131418"
            });

            Register(new ThemeDefinition
            {
                Id = "cyber-teal",
                Name = "Cyber Teal",
                Category = "Modern Flat & Fluent",
                Description = "Vibrant electric teal on dark carbon background",
                BackgroundColorHex = "#0E1517",
                SurfaceColorHex = "#162124",
                HeaderColorHex = "#111A1D",
                BorderColorHex = "#24373C",
                AccentColorHex = "#14B8A6",
                AccentHoverColorHex = "#2DD4BF",
                ForegroundColorHex = "#F0FDFA",
                MutedForegroundColorHex = "#5EEAD4",
                ControlBackgroundColorHex = "#1E2D31",
                ControlHoverColorHex = "#273A3F",
                SliderTrackColorHex = "#344D54",
                SliderThumbColorHex = "#14B8A6",
                DialogBackgroundColorHex = "#141D20"
            });

            Register(new ThemeDefinition
            {
                Id = "pure-white",
                Name = "Pure White Minimal",
                Category = "Modern Flat & Fluent",
                Description = "High-key crisp clean daylight theme with charcoal text",
                BackgroundColorHex = "#F8FAFC",
                SurfaceColorHex = "#FFFFFF",
                HeaderColorHex = "#F1F5F9",
                BorderColorHex = "#E2E8F0",
                AccentColorHex = "#2563EB",
                AccentHoverColorHex = "#1D4ED8",
                ForegroundColorHex = "#0F172A",
                MutedForegroundColorHex = "#64748B",
                ControlBackgroundColorHex = "#EDF2F7",
                ControlHoverColorHex = "#E2E8F0",
                SliderTrackColorHex = "#CBD5E1",
                SliderThumbColorHex = "#2563EB",
                DialogBackgroundColorHex = "#FFFFFF",
                IsDark = false
            });

            Register(new ThemeDefinition
            {
                Id = "slate-graphite",
                Name = "Slate Graphite",
                Category = "Modern Flat & Fluent",
                Description = "Neutral balanced industrial slate gray palette",
                BackgroundColorHex = "#1E293B",
                SurfaceColorHex = "#334155",
                HeaderColorHex = "#283548",
                BorderColorHex = "#475569",
                AccentColorHex = "#0EA5E9",
                AccentHoverColorHex = "#38BDF8",
                ForegroundColorHex = "#F8FAFC",
                MutedForegroundColorHex = "#94A3B8",
                ControlBackgroundColorHex = "#3B4B61",
                ControlHoverColorHex = "#475B75",
                SliderTrackColorHex = "#5A7292",
                SliderThumbColorHex = "#0EA5E9",
                DialogBackgroundColorHex = "#253346"
            });

            Register(new ThemeDefinition
            {
                Id = "tokyo-night",
                Name = "Tokyo Night",
                Category = "Modern Flat & Fluent",
                Description = "A clean dark theme celebrating the lights of Downtown 23-ku Tokyo",
                BackgroundColorHex = "#1A1B26",
                SurfaceColorHex = "#24283B",
                HeaderColorHex = "#1F2335",
                BorderColorHex = "#3B4261",
                AccentColorHex = "#7AA2F7",
                AccentHoverColorHex = "#BB9AF7",
                ForegroundColorHex = "#C0CAF5",
                MutedForegroundColorHex = "#7982A9",
                ControlBackgroundColorHex = "#2F354D",
                ControlHoverColorHex = "#3B4261",
                SliderTrackColorHex = "#4E577F",
                SliderThumbColorHex = "#7AA2F7",
                DialogBackgroundColorHex = "#202437"
            });

            Register(new ThemeDefinition
            {
                Id = "catppuccin-mocha",
                Name = "Catppuccin Mocha",
                Category = "Modern Flat & Fluent",
                Description = "Soothing pastel theme for high-spirited playback (Mocha)",
                BackgroundColorHex = "#1E1E2E",
                SurfaceColorHex = "#313244",
                HeaderColorHex = "#28293D",
                BorderColorHex = "#45475A",
                AccentColorHex = "#CBA6F7",
                AccentHoverColorHex = "#F5C2E7",
                ForegroundColorHex = "#CDD6F4",
                MutedForegroundColorHex = "#A6ADC8",
                ControlBackgroundColorHex = "#3B3D54",
                ControlHoverColorHex = "#45475A",
                SliderTrackColorHex = "#585B70",
                SliderThumbColorHex = "#CBA6F7",
                DialogBackgroundColorHex = "#26263B"
            });

            Register(new ThemeDefinition
            {
                Id = "catppuccin-macchiato",
                Name = "Catppuccin Macchiato",
                Category = "Modern Flat & Fluent",
                Description = "Smooth espresso and warm cream balance (Macchiato)",
                BackgroundColorHex = "#24273A",
                SurfaceColorHex = "#363A4F",
                HeaderColorHex = "#2D3146",
                BorderColorHex = "#494D64",
                AccentColorHex = "#8AADF4",
                AccentHoverColorHex = "#B7BDF8",
                ForegroundColorHex = "#CAD3F5",
                MutedForegroundColorHex = "#A5ADCB",
                ControlBackgroundColorHex = "#40455E",
                ControlHoverColorHex = "#494D64",
                SliderTrackColorHex = "#5B6078",
                SliderThumbColorHex = "#8AADF4",
                DialogBackgroundColorHex = "#2B2E45"
            });

            Register(new ThemeDefinition
            {
                Id = "catppuccin-frappe",
                Name = "Catppuccin Frappé",
                Category = "Modern Flat & Fluent",
                Description = "Muted medium-dark palette with lavender flair (Frappé)",
                BackgroundColorHex = "#303446",
                SurfaceColorHex = "#414559",
                HeaderColorHex = "#383C50",
                BorderColorHex = "#51576D",
                AccentColorHex = "#EA999C",
                AccentHoverColorHex = "#E78284",
                ForegroundColorHex = "#C6D0F5",
                MutedForegroundColorHex = "#A5ADCE",
                ControlBackgroundColorHex = "#4B5167",
                ControlHoverColorHex = "#51576D",
                SliderTrackColorHex = "#626880",
                SliderThumbColorHex = "#EA999C",
                DialogBackgroundColorHex = "#363A4D"
            });

            Register(new ThemeDefinition
            {
                Id = "catppuccin-latte",
                Name = "Catppuccin Latte",
                Category = "Modern Flat & Fluent",
                Description = "Delightful warm milk-and-coffee light theme (Latte)",
                BackgroundColorHex = "#EFF1F5",
                SurfaceColorHex = "#CCD0DA",
                HeaderColorHex = "#E6E9EF",
                BorderColorHex = "#BCC0CC",
                AccentColorHex = "#8839EF",
                AccentHoverColorHex = "#7287FD",
                ForegroundColorHex = "#4C4F69",
                MutedForegroundColorHex = "#6C6F85",
                ControlBackgroundColorHex = "#DCE0E8",
                ControlHoverColorHex = "#CCD0DA",
                SliderTrackColorHex = "#ACB0BE",
                SliderThumbColorHex = "#8839EF",
                DialogBackgroundColorHex = "#E6E9EF",
                IsDark = false
            });

            Register(new ThemeDefinition
            {
                Id = "nord-arctic",
                Name = "Nord Arctic",
                Category = "Modern Flat & Fluent",
                Description = "An arctic, north-bluish clean and elegant theme",
                BackgroundColorHex = "#2E3440",
                SurfaceColorHex = "#3B4252",
                HeaderColorHex = "#343A47",
                BorderColorHex = "#4C566A",
                AccentColorHex = "#88C0D0",
                AccentHoverColorHex = "#81A1C1",
                ForegroundColorHex = "#ECEFF4",
                MutedForegroundColorHex = "#D8DEE9",
                ControlBackgroundColorHex = "#434C5E",
                ControlHoverColorHex = "#4C566A",
                SliderTrackColorHex = "#5B677F",
                SliderThumbColorHex = "#88C0D0",
                DialogBackgroundColorHex = "#353C4A"
            });

            Register(new ThemeDefinition
            {
                Id = "dracula-official",
                Name = "Dracula Official",
                Category = "Modern Flat & Fluent",
                Description = "Vibrant dark theme with magenta, purple, and cyan highlights",
                BackgroundColorHex = "#282A36",
                SurfaceColorHex = "#343746",
                HeaderColorHex = "#2E303E",
                BorderColorHex = "#44475A",
                AccentColorHex = "#BD93F9",
                AccentHoverColorHex = "#FF79C6",
                ForegroundColorHex = "#F8F8F2",
                MutedForegroundColorHex = "#6272A4",
                ControlBackgroundColorHex = "#3B3E4F",
                ControlHoverColorHex = "#44475A",
                SliderTrackColorHex = "#55586E",
                SliderThumbColorHex = "#50FA7B",
                DialogBackgroundColorHex = "#2C2E3C"
            });

            Register(new ThemeDefinition
            {
                Id = "gruvbox-dark",
                Name = "Gruvbox Dark",
                Category = "Modern Flat & Fluent",
                Description = "Warm retro groove with earthy green, yellow, and red accents",
                BackgroundColorHex = "#282828",
                SurfaceColorHex = "#3C3836",
                HeaderColorHex = "#32302F",
                BorderColorHex = "#504945",
                AccentColorHex = "#FABD2F",
                AccentHoverColorHex = "#FE8019",
                ForegroundColorHex = "#EBDBB2",
                MutedForegroundColorHex = "#BDAE93",
                ControlBackgroundColorHex = "#45403D",
                ControlHoverColorHex = "#504945",
                SliderTrackColorHex = "#665C54",
                SliderThumbColorHex = "#FABD2F",
                DialogBackgroundColorHex = "#302E2C"
            });

            Register(new ThemeDefinition
            {
                Id = "gruvbox-light",
                Name = "Gruvbox Light",
                Category = "Modern Flat & Fluent",
                Description = "Soft parchment background with warm organic amber tones",
                BackgroundColorHex = "#FBF1C7",
                SurfaceColorHex = "#EBDBB2",
                HeaderColorHex = "#F2E5BC",
                BorderColorHex = "#D5C4A1",
                AccentColorHex = "#AF3A03",
                AccentHoverColorHex = "#B57614",
                ForegroundColorHex = "#3C3836",
                MutedForegroundColorHex = "#7C6F64",
                ControlBackgroundColorHex = "#E0D0A5",
                ControlHoverColorHex = "#D5C4A1",
                SliderTrackColorHex = "#BDAE93",
                SliderThumbColorHex = "#AF3A03",
                DialogBackgroundColorHex = "#F2E5BC",
                IsDark = false
            });

            Register(new ThemeDefinition
            {
                Id = "one-dark-pro",
                Name = "One Dark Pro",
                Category = "Modern Flat & Fluent",
                Description = "Atom & VS Code signature dark theme with crisp balanced hues",
                BackgroundColorHex = "#21252B",
                SurfaceColorHex = "#282C34",
                HeaderColorHex = "#24282F",
                BorderColorHex = "#3E4451",
                AccentColorHex = "#61AFEF",
                AccentHoverColorHex = "#98C379",
                ForegroundColorHex = "#ABB2BF",
                MutedForegroundColorHex = "#5C6370",
                ControlBackgroundColorHex = "#2F343E",
                ControlHoverColorHex = "#383E4A",
                SliderTrackColorHex = "#4B5363",
                SliderThumbColorHex = "#61AFEF",
                DialogBackgroundColorHex = "#252930"
            });

            Register(new ThemeDefinition
            {
                Id = "monokai-pro",
                Name = "Monokai Pro",
                Category = "Modern Flat & Fluent",
                Description = "Sublime Text classic high-contrast neon yellow and magenta",
                BackgroundColorHex = "#2D2A2E",
                SurfaceColorHex = "#3A363B",
                HeaderColorHex = "#332F34",
                BorderColorHex = "#4F4A50",
                AccentColorHex = "#FFD866",
                AccentHoverColorHex = "#FF6188",
                ForegroundColorHex = "#FCFCFA",
                MutedForegroundColorHex = "#727072",
                ControlBackgroundColorHex = "#423E44",
                ControlHoverColorHex = "#4F4A50",
                SliderTrackColorHex = "#605A62",
                SliderThumbColorHex = "#A9DC76",
                DialogBackgroundColorHex = "#322E33"
            });

            Register(new ThemeDefinition
            {
                Id = "github-dark",
                Name = "GitHub Dark Default",
                Category = "Modern Flat & Fluent",
                Description = "Official GitHub web and mobile dark interface aesthetic",
                BackgroundColorHex = "#0D1117",
                SurfaceColorHex = "#161B22",
                HeaderColorHex = "#11161E",
                BorderColorHex = "#30363D",
                AccentColorHex = "#2F81F7",
                AccentHoverColorHex = "#58A6FF",
                ForegroundColorHex = "#F0F6FC",
                MutedForegroundColorHex = "#8B949E",
                ControlBackgroundColorHex = "#21262D",
                ControlHoverColorHex = "#282E37",
                SliderTrackColorHex = "#3B434D",
                SliderThumbColorHex = "#2F81F7",
                DialogBackgroundColorHex = "#131820"
            });

            Register(new ThemeDefinition
            {
                Id = "material-deep-ocean",
                Name = "Material Deep Ocean",
                Category = "Modern Flat & Fluent",
                Description = "Submerged ocean blue with bright cyan highlights",
                BackgroundColorHex = "#0F111A",
                SurfaceColorHex = "#1A1C25",
                HeaderColorHex = "#141620",
                BorderColorHex = "#292D3E",
                AccentColorHex = "#80CBC4",
                AccentHoverColorHex = "#82AAFF",
                ForegroundColorHex = "#EEFFFF",
                MutedForegroundColorHex = "#717CB4",
                ControlBackgroundColorHex = "#232634",
                ControlHoverColorHex = "#2D3142",
                SliderTrackColorHex = "#3E435B",
                SliderThumbColorHex = "#80CBC4",
                DialogBackgroundColorHex = "#161823"
            });

            Register(new ThemeDefinition
            {
                Id = "palenight",
                Name = "Palenight",
                Category = "Modern Flat & Fluent",
                Description = "Soft purple-toned dark theme that is soothing on the eyes",
                BackgroundColorHex = "#292D3E",
                SurfaceColorHex = "#343A50",
                HeaderColorHex = "#2E3346",
                BorderColorHex = "#434A65",
                AccentColorHex = "#C792EA",
                AccentHoverColorHex = "#FF5370",
                ForegroundColorHex = "#ECEFF1",
                MutedForegroundColorHex = "#8796B0",
                ControlBackgroundColorHex = "#3E4560",
                ControlHoverColorHex = "#48506F",
                SliderTrackColorHex = "#576186",
                SliderThumbColorHex = "#C792EA",
                DialogBackgroundColorHex = "#2D3245"
            });

            Register(new ThemeDefinition
            {
                Id = "synthwave-84-flat",
                Name = "Synthwave '84 Flat",
                Category = "Modern Flat & Fluent",
                Description = "1980s neon sunsets, chrome and neon cyan/magenta flat palette",
                BackgroundColorHex = "#262335",
                SurfaceColorHex = "#342E48",
                HeaderColorHex = "#2C273D",
                BorderColorHex = "#463E60",
                AccentColorHex = "#FF7EDB",
                AccentHoverColorHex = "#36F9F6",
                ForegroundColorHex = "#FDFDFD",
                MutedForegroundColorHex = "#848BBD",
                ControlBackgroundColorHex = "#3D3654",
                ControlHoverColorHex = "#4A4266",
                SliderTrackColorHex = "#5E5482",
                SliderThumbColorHex = "#FF7EDB",
                DialogBackgroundColorHex = "#2E2840"
            });

            Register(new ThemeDefinition
            {
                Id = "horizon-dark",
                Name = "Horizon Dark",
                Category = "Modern Flat & Fluent",
                Description = "Warm sunset gradients fading into a tranquil midnight sky",
                BackgroundColorHex = "#1C1E26",
                SurfaceColorHex = "#232530",
                HeaderColorHex = "#1F212B",
                BorderColorHex = "#333747",
                AccentColorHex = "#E95678",
                AccentHoverColorHex = "#FAB795",
                ForegroundColorHex = "#D5D8DA",
                MutedForegroundColorHex = "#6C6F93",
                ControlBackgroundColorHex = "#2B2D3A",
                ControlHoverColorHex = "#353849",
                SliderTrackColorHex = "#43475D",
                SliderThumbColorHex = "#E95678",
                DialogBackgroundColorHex = "#20222C"
            });

            Register(new ThemeDefinition
            {
                Id = "ayu-dark",
                Name = "Ayu Dark",
                Category = "Modern Flat & Fluent",
                Description = "Crisp dark palette with golden amber accents",
                BackgroundColorHex = "#0B0E14",
                SurfaceColorHex = "#131721",
                HeaderColorHex = "#0F121A",
                BorderColorHex = "#202738",
                AccentColorHex = "#E6B450",
                AccentHoverColorHex = "#FFB454",
                ForegroundColorHex = "#BFBDB6",
                MutedForegroundColorHex = "#5C6773",
                ControlBackgroundColorHex = "#191F2C",
                ControlHoverColorHex = "#212839",
                SliderTrackColorHex = "#2D384E",
                SliderThumbColorHex = "#E6B450",
                DialogBackgroundColorHex = "#11151D"
            });

            Register(new ThemeDefinition
            {
                Id = "ayu-mirage",
                Name = "Ayu Mirage",
                Category = "Modern Flat & Fluent",
                Description = "Mid-tone bluish grey theme with warm coral highlights",
                BackgroundColorHex = "#1F2430",
                SurfaceColorHex = "#272D3B",
                HeaderColorHex = "#232835",
                BorderColorHex = "#3B4458",
                AccentColorHex = "#FF8F40",
                AccentHoverColorHex = "#F28779",
                ForegroundColorHex = "#CBCCC6",
                MutedForegroundColorHex = "#707A8C",
                ControlBackgroundColorHex = "#303748",
                ControlHoverColorHex = "#3A4358",
                SliderTrackColorHex = "#4A5670",
                SliderThumbColorHex = "#FF8F40",
                DialogBackgroundColorHex = "#242937"
            });

            Register(new ThemeDefinition
            {
                Id = "night-owl",
                Name = "Night Owl",
                Category = "Modern Flat & Fluent",
                Description = "Tuned for low-light late night video watching sessions",
                BackgroundColorHex = "#011627",
                SurfaceColorHex = "#0B253A",
                HeaderColorHex = "#061D30",
                BorderColorHex = "#1A3B57",
                AccentColorHex = "#82AAFF",
                AccentHoverColorHex = "#7FDBCA",
                ForegroundColorHex = "#D6DEEB",
                MutedForegroundColorHex = "#5F7E97",
                ControlBackgroundColorHex = "#11324D",
                ControlHoverColorHex = "#183F60",
                SliderTrackColorHex = "#22517B",
                SliderThumbColorHex = "#82AAFF",
                DialogBackgroundColorHex = "#081E32"
            });

            Register(new ThemeDefinition
            {
                Id = "cobalt2",
                Name = "Cobalt2",
                Category = "Modern Flat & Fluent",
                Description = "Rich deep navy and vibrant yellow highlights from Wes Bos",
                BackgroundColorHex = "#193549",
                SurfaceColorHex = "#1F4662",
                HeaderColorHex = "#1B3B53",
                BorderColorHex = "#2F658E",
                AccentColorHex = "#FFC600",
                AccentHoverColorHex = "#0088FF",
                ForegroundColorHex = "#FFFFFF",
                MutedForegroundColorHex = "#9FB0BE",
                ControlBackgroundColorHex = "#265476",
                ControlHoverColorHex = "#2E658E",
                SliderTrackColorHex = "#3D7EB0",
                SliderThumbColorHex = "#FFC600",
                DialogBackgroundColorHex = "#1D3E56"
            });

            Register(new ThemeDefinition
            {
                Id = "solarized-dark",
                Name = "Solarized Dark",
                Category = "Modern Flat & Fluent",
                Description = "Precision-engineered lab color space by Ethan Schoonover",
                BackgroundColorHex = "#002B36",
                SurfaceColorHex = "#073642",
                HeaderColorHex = "#04303C",
                BorderColorHex = "#0F4B5B",
                AccentColorHex = "#2AA198",
                AccentHoverColorHex = "#268BD2",
                ForegroundColorHex = "#839496",
                MutedForegroundColorHex = "#586E75",
                ControlBackgroundColorHex = "#0A404E",
                ControlHoverColorHex = "#0F4E5F",
                SliderTrackColorHex = "#166479",
                SliderThumbColorHex = "#2AA198",
                DialogBackgroundColorHex = "#05333F"
            });

            Register(new ThemeDefinition
            {
                Id = "solarized-light",
                Name = "Solarized Light",
                Category = "Modern Flat & Fluent",
                Description = "Calm solarized daylight palette for ambient lit rooms",
                BackgroundColorHex = "#FDF6E3",
                SurfaceColorHex = "#EEE8D5",
                HeaderColorHex = "#F6EED8",
                BorderColorHex = "#DDD6C1",
                AccentColorHex = "#268BD2",
                AccentHoverColorHex = "#2AA198",
                ForegroundColorHex = "#657B83",
                MutedForegroundColorHex = "#93A1A1",
                ControlBackgroundColorHex = "#E4DCC5",
                ControlHoverColorHex = "#DDD6C1",
                SliderTrackColorHex = "#CBBA97",
                SliderThumbColorHex = "#268BD2",
                DialogBackgroundColorHex = "#F6EED8",
                IsDark = false
            });

            Register(new ThemeDefinition
            {
                Id = "rose-pine",
                Name = "Rosé Pine",
                Category = "Modern Flat & Fluent",
                Description = "All natural pine, faux fur, and bougainvillea palette",
                BackgroundColorHex = "#191724",
                SurfaceColorHex = "#26233A",
                HeaderColorHex = "#211F31",
                BorderColorHex = "#3A3654",
                AccentColorHex = "#EB6F92",
                AccentHoverColorHex = "#F6C177",
                ForegroundColorHex = "#E0DEF4",
                MutedForegroundColorHex = "#908CAA",
                ControlBackgroundColorHex = "#2D2A45",
                ControlHoverColorHex = "#383455",
                SliderTrackColorHex = "#48436E",
                SliderThumbColorHex = "#EB6F92",
                DialogBackgroundColorHex = "#201D2E"
            });

            Register(new ThemeDefinition
            {
                Id = "rose-pine-moon",
                Name = "Rosé Pine Moon",
                Category = "Modern Flat & Fluent",
                Description = "Soft lunar dusk variation of the beloved Rosé Pine",
                BackgroundColorHex = "#232136",
                SurfaceColorHex = "#2A273F",
                HeaderColorHex = "#26243A",
                BorderColorHex = "#3F3C5E",
                AccentColorHex = "#EA9A97",
                AccentHoverColorHex = "#C4A7E7",
                ForegroundColorHex = "#E0DEF4",
                MutedForegroundColorHex = "#908CAA",
                ControlBackgroundColorHex = "#33304C",
                ControlHoverColorHex = "#3D395B",
                SliderTrackColorHex = "#4F4B75",
                SliderThumbColorHex = "#EA9A97",
                DialogBackgroundColorHex = "#27243B"
            });

            Register(new ThemeDefinition
            {
                Id = "kanagawa-wave",
                Name = "Kanagawa Wave",
                Category = "Modern Flat & Fluent",
                Description = "Japanese woodblock print inspired inks and sumi-e pigments",
                BackgroundColorHex = "#1F1F28",
                SurfaceColorHex = "#2A2A37",
                HeaderColorHex = "#252531",
                BorderColorHex = "#363646",
                AccentColorHex = "#7E9CD8",
                AccentHoverColorHex = "#98BB6C",
                ForegroundColorHex = "#DCD7BA",
                MutedForegroundColorHex = "#727169",
                ControlBackgroundColorHex = "#313140",
                ControlHoverColorHex = "#3B3B4D",
                SliderTrackColorHex = "#49495E",
                SliderThumbColorHex = "#7E9CD8",
                DialogBackgroundColorHex = "#242431"
            });

            Register(new ThemeDefinition
            {
                Id = "everforest-dark",
                Name = "Everforest Dark",
                Category = "Modern Flat & Fluent",
                Description = "Comfortable green forest tones pleasant to human eyes",
                BackgroundColorHex = "#2D353B",
                SurfaceColorHex = "#343F44",
                HeaderColorHex = "#303A3F",
                BorderColorHex = "#475258",
                AccentColorHex = "#A7C080",
                AccentHoverColorHex = "#83C092",
                ForegroundColorHex = "#D3C6AA",
                MutedForegroundColorHex = "#859289",
                ControlBackgroundColorHex = "#3B474D",
                ControlHoverColorHex = "#445258",
                SliderTrackColorHex = "#53636B",
                SliderThumbColorHex = "#A7C080",
                DialogBackgroundColorHex = "#313B40"
            });

            Register(new ThemeDefinition
            {
                Id = "moonlight-ocean",
                Name = "Moonlight Ocean",
                Category = "Modern Flat & Fluent",
                Description = "Cool moonlight shimmering across tranquil dark indigo waters",
                BackgroundColorHex = "#1E2030",
                SurfaceColorHex = "#222436",
                HeaderColorHex = "#202233",
                BorderColorHex = "#3A3E5B",
                AccentColorHex = "#82AAFF",
                AccentHoverColorHex = "#C8D3F5",
                ForegroundColorHex = "#C8D3F5",
                MutedForegroundColorHex = "#828BB8",
                ControlBackgroundColorHex = "#2A2D43",
                ControlHoverColorHex = "#343753",
                SliderTrackColorHex = "#44486D",
                SliderThumbColorHex = "#82AAFF",
                DialogBackgroundColorHex = "#212335"
            });

            Register(new ThemeDefinition
            {
                Id = "panda-theme",
                Name = "Panda Dark",
                Category = "Modern Flat & Fluent",
                Description = "Super-minimalist dark theme with mint green and soft red",
                BackgroundColorHex = "#292A2B",
                SurfaceColorHex = "#303133",
                HeaderColorHex = "#2C2D2E",
                BorderColorHex = "#434447",
                AccentColorHex = "#19F9D8",
                AccentHoverColorHex = "#FF4B82",
                ForegroundColorHex = "#E6E6E6",
                MutedForegroundColorHex = "#8F9094",
                ControlBackgroundColorHex = "#38393B",
                ControlHoverColorHex = "#434447",
                SliderTrackColorHex = "#55565A",
                SliderThumbColorHex = "#19F9D8",
                DialogBackgroundColorHex = "#2D2E2F"
            });

            Register(new ThemeDefinition
            {
                Id = "shades-of-purple",
                Name = "Shades of Purple",
                Category = "Modern Flat & Fluent",
                Description = "A professional theme with hand-picked shades of purple and gold",
                BackgroundColorHex = "#222244",
                SurfaceColorHex = "#2D2B55",
                HeaderColorHex = "#27254C",
                BorderColorHex = "#443F7A",
                AccentColorHex = "#FAD000",
                AccentHoverColorHex = "#B362FF",
                ForegroundColorHex = "#FFFFFF",
                MutedForegroundColorHex = "#A599E9",
                ControlBackgroundColorHex = "#363366",
                ControlHoverColorHex = "#403D78",
                SliderTrackColorHex = "#534F99",
                SliderThumbColorHex = "#FAD000",
                DialogBackgroundColorHex = "#28264D"
            });

            Register(new ThemeDefinition
            {
                Id = "poimandres-dark",
                Name = "Poimandres",
                Category = "Modern Flat & Fluent",
                Description = "Stormy grey-blue tones inspired by supernatural Scandinavian seas",
                BackgroundColorHex = "#1B1E2B",
                SurfaceColorHex = "#252B3B",
                HeaderColorHex = "#202433",
                BorderColorHex = "#373E54",
                AccentColorHex = "#5DE4C7",
                AccentHoverColorHex = "#89DDFF",
                ForegroundColorHex = "#E4F0FB",
                MutedForegroundColorHex = "#767C9D",
                ControlBackgroundColorHex = "#2D3447",
                ControlHoverColorHex = "#363E55",
                SliderTrackColorHex = "#475270",
                SliderThumbColorHex = "#5DE4C7",
                DialogBackgroundColorHex = "#222636"
            });

            Register(new ThemeDefinition
            {
                Id = "vesper-minimal",
                Name = "Vesper Minimal",
                Category = "Modern Flat & Fluent",
                Description = "Subtle dark slate with bright luminous amber highlights",
                BackgroundColorHex = "#101010",
                SurfaceColorHex = "#181818",
                HeaderColorHex = "#141414",
                BorderColorHex = "#282828",
                AccentColorHex = "#FFC799",
                AccentHoverColorHex = "#FFE5D0",
                ForegroundColorHex = "#FFFFFF",
                MutedForegroundColorHex = "#7E7E7E",
                ControlBackgroundColorHex = "#222222",
                ControlHoverColorHex = "#2C2C2C",
                SliderTrackColorHex = "#3D3D3D",
                SliderThumbColorHex = "#FFC799",
                DialogBackgroundColorHex = "#161616"
            });

            // =========================================================================
            // CATEGORY 3: STUDIO GLASS & ACRYLIC (25 Themes)
            // =========================================================================
            Register(new ThemeDefinition
            {
                Id = "win11-mica-glass",
                Name = "Windows 11 Mica Glass",
                Category = "Studio Glass & Acrylic",
                Description = "Native Windows 11 dynamic Mica material and rounded blur",
                BackgroundColorHex = "#18181C",
                SurfaceColorHex = "#202026",
                HeaderColorHex = "#1C1C22",
                BorderColorHex = "#32323D",
                AccentColorHex = "#60CDFF",
                AccentHoverColorHex = "#99EBFF",
                ForegroundColorHex = "#FFFFFF",
                MutedForegroundColorHex = "#9D9DA8",
                ControlBackgroundColorHex = "#282830",
                ControlHoverColorHex = "#33333E",
                SliderTrackColorHex = "#434352",
                SliderThumbColorHex = "#60CDFF",
                DialogBackgroundColorHex = "#1F1F25",
                GlassBlurOpacity = 0.82,
                CornerRadius = 10.0
            });

            Register(new ThemeDefinition
            {
                Id = "deep-frost-acrylic",
                Name = "Deep Frost Acrylic",
                Category = "Studio Glass & Acrylic",
                Description = "Crystalline frosted ice diffusion with sapphire glow",
                BackgroundColorHex = "#0C1420",
                SurfaceColorHex = "#142133",
                HeaderColorHex = "#101A29",
                BorderColorHex = "#233957",
                AccentColorHex = "#38BDF8",
                AccentHoverColorHex = "#7DD3FC",
                ForegroundColorHex = "#F0F9FF",
                MutedForegroundColorHex = "#7DD3FC",
                ControlBackgroundColorHex = "#1A2C44",
                ControlHoverColorHex = "#223957",
                SliderTrackColorHex = "#2E4D75",
                SliderThumbColorHex = "#38BDF8",
                DialogBackgroundColorHex = "#121E2E",
                GlassBlurOpacity = 0.78,
                CornerRadius = 8.0
            });

            Register(new ThemeDefinition
            {
                Id = "frosted-sapphire",
                Name = "Frosted Sapphire",
                Category = "Studio Glass & Acrylic",
                Description = "Translucent deep royal sapphire glass pane",
                BackgroundColorHex = "#0A1026",
                SurfaceColorHex = "#111B3D",
                HeaderColorHex = "#0E1532",
                BorderColorHex = "#20306B",
                AccentColorHex = "#6366F1",
                AccentHoverColorHex = "#818CF8",
                ForegroundColorHex = "#EEF2FF",
                MutedForegroundColorHex = "#A5B4FC",
                ControlBackgroundColorHex = "#172452",
                ControlHoverColorHex = "#1E2F6B",
                SliderTrackColorHex = "#2B4191",
                SliderThumbColorHex = "#6366F1",
                DialogBackgroundColorHex = "#0E1736",
                GlassBlurOpacity = 0.80
            });

            Register(new ThemeDefinition
            {
                Id = "smoked-quartz",
                Name = "Smoked Quartz",
                Category = "Studio Glass & Acrylic",
                Description = "Warm smoked translucent quartz crystal with bronze shimmer",
                BackgroundColorHex = "#1B1715",
                SurfaceColorHex = "#28221F",
                HeaderColorHex = "#211C1A",
                BorderColorHex = "#423833",
                AccentColorHex = "#D97706",
                AccentHoverColorHex = "#F59E0B",
                ForegroundColorHex = "#FEF3C7",
                MutedForegroundColorHex = "#D4B28C",
                ControlBackgroundColorHex = "#342C28",
                ControlHoverColorHex = "#423833",
                SliderTrackColorHex = "#544741",
                SliderThumbColorHex = "#D97706",
                DialogBackgroundColorHex = "#231E1C",
                GlassBlurOpacity = 0.85
            });

            Register(new ThemeDefinition
            {
                Id = "emerald-tint-glass",
                Name = "Emerald Tint",
                Category = "Studio Glass & Acrylic",
                Description = "Translucent forest emerald glass with jade reflections",
                BackgroundColorHex = "#0B1711",
                SurfaceColorHex = "#12251B",
                HeaderColorHex = "#0F1E16",
                BorderColorHex = "#1F3D2D",
                AccentColorHex = "#10B981",
                AccentHoverColorHex = "#34D399",
                ForegroundColorHex = "#ECFDF5",
                MutedForegroundColorHex = "#6EE7B7",
                ControlBackgroundColorHex = "#183225",
                ControlHoverColorHex = "#204231",
                SliderTrackColorHex = "#2B5741",
                SliderThumbColorHex = "#10B981",
                DialogBackgroundColorHex = "#102018",
                GlassBlurOpacity = 0.80
            });

            Register(new ThemeDefinition
            {
                Id = "amber-glow-acrylic",
                Name = "Amber Glow",
                Category = "Studio Glass & Acrylic",
                Description = "Rich golden honey acrylic diffusion with warm illumination",
                BackgroundColorHex = "#1A150A",
                SurfaceColorHex = "#292110",
                HeaderColorHex = "#221C0E",
                BorderColorHex = "#47391C",
                AccentColorHex = "#F59E0B",
                AccentHoverColorHex = "#FBBF24",
                ForegroundColorHex = "#FFFBEB",
                MutedForegroundColorHex = "#FDE68A",
                ControlBackgroundColorHex = "#352B15",
                ControlHoverColorHex = "#43361B",
                SliderTrackColorHex = "#574623",
                SliderThumbColorHex = "#F59E0B",
                DialogBackgroundColorHex = "#241D0E",
                GlassBlurOpacity = 0.82
            });

            Register(new ThemeDefinition
            {
                Id = "violet-mist-glass",
                Name = "Violet Mist",
                Category = "Studio Glass & Acrylic",
                Description = "Translucent purple amethyst with diffuse lavender glow",
                BackgroundColorHex = "#170F20",
                SurfaceColorHex = "#241733",
                HeaderColorHex = "#1E132A",
                BorderColorHex = "#3D2757",
                AccentColorHex = "#C084FC",
                AccentHoverColorHex = "#E879F9",
                ForegroundColorHex = "#FAF5FF",
                MutedForegroundColorHex = "#D8B4FE",
                ControlBackgroundColorHex = "#2E1E41",
                ControlHoverColorHex = "#3B2754",
                SliderTrackColorHex = "#4E336F",
                SliderThumbColorHex = "#C084FC",
                DialogBackgroundColorHex = "#20152D",
                GlassBlurOpacity = 0.80
            });

            Register(new ThemeDefinition
            {
                Id = "obsidian-frost",
                Name = "Obsidian Frost",
                Category = "Studio Glass & Acrylic",
                Description = "Semi-matte frosted volcanic glass with ice white edges",
                BackgroundColorHex = "#121214",
                SurfaceColorHex = "#1A1A1E",
                HeaderColorHex = "#16161A",
                BorderColorHex = "#2B2B33",
                AccentColorHex = "#E2E8F0",
                AccentHoverColorHex = "#FFFFFF",
                ForegroundColorHex = "#FFFFFF",
                MutedForegroundColorHex = "#94A3B8",
                ControlBackgroundColorHex = "#222228",
                ControlHoverColorHex = "#2D2D36",
                SliderTrackColorHex = "#3B3B47",
                SliderThumbColorHex = "#E2E8F0",
                DialogBackgroundColorHex = "#18181C",
                GlassBlurOpacity = 0.88
            });

            Register(new ThemeDefinition
            {
                Id = "arctic-blue-glass",
                Name = "Arctic Blue Glass",
                Category = "Studio Glass & Acrylic",
                Description = "Polar glacier acrylic glass with crisp cold blue glow",
                BackgroundColorHex = "#0C1724",
                SurfaceColorHex = "#132438",
                HeaderColorHex = "#101D2E",
                BorderColorHex = "#223F61",
                AccentColorHex = "#00D2FF",
                AccentHoverColorHex = "#70E5FF",
                ForegroundColorHex = "#F0F9FF",
                MutedForegroundColorHex = "#7DD3FC",
                ControlBackgroundColorHex = "#1A314D",
                ControlHoverColorHex = "#213E61",
                SliderTrackColorHex = "#2E5585",
                SliderThumbColorHex = "#00D2FF",
                DialogBackgroundColorHex = "#112032",
                GlassBlurOpacity = 0.80
            });

            Register(new ThemeDefinition
            {
                Id = "rose-quartz-glass",
                Name = "Rose Quartz Glass",
                Category = "Studio Glass & Acrylic",
                Description = "Soft pastel pink frosted gemstone translucency",
                BackgroundColorHex = "#1C1115",
                SurfaceColorHex = "#2B1A20",
                HeaderColorHex = "#24161B",
                BorderColorHex = "#472B35",
                AccentColorHex = "#F472B6",
                AccentHoverColorHex = "#FB7185",
                ForegroundColorHex = "#FDF2F8",
                MutedForegroundColorHex = "#F9A8D4",
                ControlBackgroundColorHex = "#372129",
                ControlHoverColorHex = "#452A34",
                SliderTrackColorHex = "#583643",
                SliderThumbColorHex = "#F472B6",
                DialogBackgroundColorHex = "#26171D",
                GlassBlurOpacity = 0.82
            });

            Register(new ThemeDefinition
            {
                Id = "liquid-chrome-glass",
                Name = "Liquid Chrome Glass",
                Category = "Studio Glass & Acrylic",
                Description = "Ultra-reflective metallic mirror glass with silver iridescence",
                BackgroundColorHex = "#17181A",
                SurfaceColorHex = "#232529",
                HeaderColorHex = "#1D1E22",
                BorderColorHex = "#3C4047",
                AccentColorHex = "#94A3B8",
                AccentHoverColorHex = "#CBD5E1",
                ForegroundColorHex = "#F8FAFC",
                MutedForegroundColorHex = "#64748B",
                ControlBackgroundColorHex = "#2D3035",
                ControlHoverColorHex = "#393C42",
                SliderTrackColorHex = "#4A4E57",
                SliderThumbColorHex = "#94A3B8",
                DialogBackgroundColorHex = "#202125",
                GlassBlurOpacity = 0.90
            });

            Register(new ThemeDefinition
            {
                Id = "aurora-glass",
                Name = "Aurora Borealis Glass",
                Category = "Studio Glass & Acrylic",
                Description = "Northern lights shimmering gradient across frosted glass",
                BackgroundColorHex = "#0B1519",
                SurfaceColorHex = "#122229",
                HeaderColorHex = "#0E1C21",
                BorderColorHex = "#1F3D4A",
                AccentColorHex = "#2DD4BF",
                AccentHoverColorHex = "#A855F7",
                ForegroundColorHex = "#F0FDFA",
                MutedForegroundColorHex = "#5EEAD4",
                ControlBackgroundColorHex = "#19303A",
                ControlHoverColorHex = "#213F4C",
                SliderTrackColorHex = "#2D5466",
                SliderThumbColorHex = "#2DD4BF",
                DialogBackgroundColorHex = "#101F25",
                GlassBlurOpacity = 0.80
            });

            Register(new ThemeDefinition
            {
                Id = "cyber-neon-glass",
                Name = "Cyberpunk Neon Glass",
                Category = "Studio Glass & Acrylic",
                Description = "Smoked acrylic with high-intensity neon magenta and cyan edging",
                BackgroundColorHex = "#110D1A",
                SurfaceColorHex = "#1C152B",
                HeaderColorHex = "#171123",
                BorderColorHex = "#372954",
                AccentColorHex = "#F43F5E",
                AccentHoverColorHex = "#06B6D4",
                ForegroundColorHex = "#FFF1F2",
                MutedForegroundColorHex = "#FDA4AF",
                ControlBackgroundColorHex = "#251B38",
                ControlHoverColorHex = "#31244A",
                SliderTrackColorHex = "#433266",
                SliderThumbColorHex = "#F43F5E",
                DialogBackgroundColorHex = "#181224",
                GlassBlurOpacity = 0.85
            });

            Register(new ThemeDefinition
            {
                Id = "sunset-tint-glass",
                Name = "Sunset Tint",
                Category = "Studio Glass & Acrylic",
                Description = "Warm twilight gradient on frosted translucent acrylic",
                BackgroundColorHex = "#1C1214",
                SurfaceColorHex = "#2B1B1E",
                HeaderColorHex = "#24171A",
                BorderColorHex = "#472D32",
                AccentColorHex = "#FB923C",
                AccentHoverColorHex = "#F43F5E",
                ForegroundColorHex = "#FFF7ED",
                MutedForegroundColorHex = "#FDBA74",
                ControlBackgroundColorHex = "#372327",
                ControlHoverColorHex = "#452C32",
                SliderTrackColorHex = "#58383F",
                SliderThumbColorHex = "#FB923C",
                DialogBackgroundColorHex = "#26181B",
                GlassBlurOpacity = 0.82
            });

            Register(new ThemeDefinition
            {
                Id = "champagne-ice",
                Name = "Champagne Ice Glass",
                Category = "Studio Glass & Acrylic",
                Description = "Pale effervescent golden crystal with sparkling diamond edges",
                BackgroundColorHex = "#191712",
                SurfaceColorHex = "#26231B",
                HeaderColorHex = "#201D17",
                BorderColorHex = "#423D2E",
                AccentColorHex = "#FDE047",
                AccentHoverColorHex = "#FEF08A",
                ForegroundColorHex = "#FEFCE8",
                MutedForegroundColorHex = "#EAB308",
                ControlBackgroundColorHex = "#312D23",
                ControlHoverColorHex = "#3E392C",
                SliderTrackColorHex = "#524B3B",
                SliderThumbColorHex = "#FDE047",
                DialogBackgroundColorHex = "#221F18",
                GlassBlurOpacity = 0.84
            });

            Register(new ThemeDefinition
            {
                Id = "titanium-brushed-glass",
                Name = "Titanium Brushed Glass",
                Category = "Studio Glass & Acrylic",
                Description = "Aero-grade titanium alloy backing with micro-frosted sapphire top",
                BackgroundColorHex = "#15181C",
                SurfaceColorHex = "#20252B",
                HeaderColorHex = "#1B1F24",
                BorderColorHex = "#343C46",
                AccentColorHex = "#38BDF8",
                AccentHoverColorHex = "#7DD3FC",
                ForegroundColorHex = "#F8FAFC",
                MutedForegroundColorHex = "#94A3B8",
                ControlBackgroundColorHex = "#293038",
                ControlHoverColorHex = "#343D47",
                SliderTrackColorHex = "#444F5C",
                SliderThumbColorHex = "#38BDF8",
                DialogBackgroundColorHex = "#1E2228",
                GlassBlurOpacity = 0.88
            });

            Register(new ThemeDefinition
            {
                Id = "glacial-white-glass",
                Name = "Glacial White Glass",
                Category = "Studio Glass & Acrylic",
                Description = "Pure arctic ice white with crystalline cyan refraction",
                BackgroundColorHex = "#EBF4F6",
                SurfaceColorHex = "#F5FAFA",
                HeaderColorHex = "#DFEFF2",
                BorderColorHex = "#B8DCE2",
                AccentColorHex = "#0284C7",
                AccentHoverColorHex = "#0369A1",
                ForegroundColorHex = "#0F172A",
                MutedForegroundColorHex = "#475569",
                ControlBackgroundColorHex = "#D2EBF0",
                ControlHoverColorHex = "#B8DCE2",
                SliderTrackColorHex = "#94C9D3",
                SliderThumbColorHex = "#0284C7",
                DialogBackgroundColorHex = "#F0F8FA",
                GlassBlurOpacity = 0.75,
                IsDark = false
            });

            Register(new ThemeDefinition
            {
                Id = "mystic-topaz-glass",
                Name = "Mystic Topaz Glass",
                Category = "Studio Glass & Acrylic",
                Description = "Iridescent shifting rainbow hues across dark faceted glass",
                BackgroundColorHex = "#13101C",
                SurfaceColorHex = "#1F1A2D",
                HeaderColorHex = "#191525",
                BorderColorHex = "#382F52",
                AccentColorHex = "#EC4899",
                AccentHoverColorHex = "#8B5CF6",
                ForegroundColorHex = "#FAF5FF",
                MutedForegroundColorHex = "#C084FC",
                ControlBackgroundColorHex = "#29223B",
                ControlHoverColorHex = "#352C4D",
                SliderTrackColorHex = "#483C69",
                SliderThumbColorHex = "#EC4899",
                DialogBackgroundColorHex = "#1B1727",
                GlassBlurOpacity = 0.82
            });

            Register(new ThemeDefinition
            {
                Id = "aquamarine-glass",
                Name = "Aquamarine Glass",
                Category = "Studio Glass & Acrylic",
                Description = "Gentle sea foam and clear tropical lagoon acrylic",
                BackgroundColorHex = "#0C1819",
                SurfaceColorHex = "#132729",
                HeaderColorHex = "#102021",
                BorderColorHex = "#214447",
                AccentColorHex = "#2DD4BF",
                AccentHoverColorHex = "#5EEAD4",
                ForegroundColorHex = "#F0FDFA",
                MutedForegroundColorHex = "#99F6E4",
                ControlBackgroundColorHex = "#1A3638",
                ControlHoverColorHex = "#23474A",
                SliderTrackColorHex = "#2E5E62",
                SliderThumbColorHex = "#2DD4BF",
                DialogBackgroundColorHex = "#112224",
                GlassBlurOpacity = 0.80
            });

            Register(new ThemeDefinition
            {
                Id = "coral-reef-glass",
                Name = "Coral Reef Glass",
                Category = "Studio Glass & Acrylic",
                Description = "Warm coral and peach highlights floating on sea glass",
                BackgroundColorHex = "#1B1213",
                SurfaceColorHex = "#2A1C1D",
                HeaderColorHex = "#231718",
                BorderColorHex = "#452E2F",
                AccentColorHex = "#F87171",
                AccentHoverColorHex = "#FB923C",
                ForegroundColorHex = "#FEF2F2",
                MutedForegroundColorHex = "#FCA5A5",
                ControlBackgroundColorHex = "#362425",
                ControlHoverColorHex = "#442D2F",
                SliderTrackColorHex = "#563A3C",
                SliderThumbColorHex = "#F87171",
                DialogBackgroundColorHex = "#251819",
                GlassBlurOpacity = 0.82
            });

            Register(new ThemeDefinition
            {
                Id = "phantom-velvet-glass",
                Name = "Phantom Velvet Glass",
                Category = "Studio Glass & Acrylic",
                Description = "Deep midnight velvet undertone beneath ultra-gloss glass",
                BackgroundColorHex = "#120F17",
                SurfaceColorHex = "#1D1826",
                HeaderColorHex = "#18141F",
                BorderColorHex = "#332A42",
                AccentColorHex = "#9333EA",
                AccentHoverColorHex = "#A855F7",
                ForegroundColorHex = "#FAF5FF",
                MutedForegroundColorHex = "#C084FC",
                ControlBackgroundColorHex = "#261F32",
                ControlHoverColorHex = "#312840",
                SliderTrackColorHex = "#423757",
                SliderThumbColorHex = "#9333EA",
                DialogBackgroundColorHex = "#1A1521",
                GlassBlurOpacity = 0.86
            });

            Register(new ThemeDefinition
            {
                Id = "opal-iridescent",
                Name = "Opal Iridescent",
                Category = "Studio Glass & Acrylic",
                Description = "Precious mineral opal featuring spectral prism refractions",
                BackgroundColorHex = "#161922",
                SurfaceColorHex = "#222735",
                HeaderColorHex = "#1C202C",
                BorderColorHex = "#394157",
                AccentColorHex = "#38BDF8",
                AccentHoverColorHex = "#F472B6",
                ForegroundColorHex = "#F8FAFC",
                MutedForegroundColorHex = "#94A3B8",
                ControlBackgroundColorHex = "#2B3244",
                ControlHoverColorHex = "#374057",
                SliderTrackColorHex = "#475270",
                SliderThumbColorHex = "#38BDF8",
                DialogBackgroundColorHex = "#1F2330",
                GlassBlurOpacity = 0.83
            });

            Register(new ThemeDefinition
            {
                Id = "midnight-frost",
                Name = "Midnight Frost",
                Category = "Studio Glass & Acrylic",
                Description = "Sub-zero ice crystals forming on pitch black window pane",
                BackgroundColorHex = "#080B12",
                SurfaceColorHex = "#101624",
                HeaderColorHex = "#0C111C",
                BorderColorHex = "#1D2840",
                AccentColorHex = "#60A5FA",
                AccentHoverColorHex = "#93C5FD",
                ForegroundColorHex = "#F8FAFC",
                MutedForegroundColorHex = "#64748B",
                ControlBackgroundColorHex = "#162033",
                ControlHoverColorHex = "#1F2C47",
                SliderTrackColorHex = "#2A3C61",
                SliderThumbColorHex = "#60A5FA",
                DialogBackgroundColorHex = "#0E1420",
                GlassBlurOpacity = 0.85
            });

            Register(new ThemeDefinition
            {
                Id = "citrine-warm-glass",
                Name = "Citrine Warm Glass",
                Category = "Studio Glass & Acrylic",
                Description = "Sunny citrine gemstone glow through frosted crystal",
                BackgroundColorHex = "#1A170F",
                SurfaceColorHex = "#282317",
                HeaderColorHex = "#221E13",
                BorderColorHex = "#453C27",
                AccentColorHex = "#EAB308",
                AccentHoverColorHex = "#FACC15",
                ForegroundColorHex = "#FEFCE8",
                MutedForegroundColorHex = "#CA8A04",
                ControlBackgroundColorHex = "#342E1E",
                ControlHoverColorHex = "#423B27",
                SliderTrackColorHex = "#574E33",
                SliderThumbColorHex = "#EAB308",
                DialogBackgroundColorHex = "#231F14",
                GlassBlurOpacity = 0.82
            });

            Register(new ThemeDefinition
            {
                Id = "cobalt-crystal",
                Name = "Cobalt Crystal Glass",
                Category = "Studio Glass & Acrylic",
                Description = "Deep laboratory cobalt glass bottle with high-clarity blue refraction",
                BackgroundColorHex = "#091224",
                SurfaceColorHex = "#101E3C",
                HeaderColorHex = "#0C1830",
                BorderColorHex = "#1D356A",
                AccentColorHex = "#2563EB",
                AccentHoverColorHex = "#60A5FA",
                ForegroundColorHex = "#EFF6FF",
                MutedForegroundColorHex = "#93C5FD",
                ControlBackgroundColorHex = "#162952",
                ControlHoverColorHex = "#1E366B",
                SliderTrackColorHex = "#2A4B94",
                SliderThumbColorHex = "#2563EB",
                DialogBackgroundColorHex = "#0E1A34",
                GlassBlurOpacity = 0.80
            });

            // =========================================================================
            // CATEGORY 4: HIGH-CONTRAST & GAMER SKINS (25 Themes)
            // =========================================================================
            Register(new ThemeDefinition
            {
                Id = "neon-cyberpunk",
                Name = "Neon Cyberpunk",
                Category = "High-Contrast & Gamer Skins",
                Description = "Night City maximum voltage neon yellow, hot pink, and cyan",
                BackgroundColorHex = "#08060D",
                SurfaceColorHex = "#140F22",
                HeaderColorHex = "#0F0B1A",
                BorderColorHex = "#331E5E",
                AccentColorHex = "#FFE600",
                AccentHoverColorHex = "#FF0055",
                ForegroundColorHex = "#00F0FF",
                MutedForegroundColorHex = "#FF70A6",
                ControlBackgroundColorHex = "#21163A",
                ControlHoverColorHex = "#2F1E54",
                SliderTrackColorHex = "#442B7A",
                SliderThumbColorHex = "#FFE600",
                DialogBackgroundColorHex = "#110D1E",
                GlowEffect = true,
                GlowColorHex = "#FFE600"
            });

            Register(new ThemeDefinition
            {
                Id = "synthwave-84-highvolt",
                Name = "Synthwave 84 High-Volt",
                Category = "High-Contrast & Gamer Skins",
                Description = "Glow-in-the-dark neon magenta and electric violet laser grid",
                BackgroundColorHex = "#120B20",
                SurfaceColorHex = "#201238",
                HeaderColorHex = "#190E2C",
                BorderColorHex = "#4D268A",
                AccentColorHex = "#FF007F",
                AccentHoverColorHex = "#00F0FF",
                ForegroundColorHex = "#FFFFFF",
                MutedForegroundColorHex = "#E080FF",
                ControlBackgroundColorHex = "#2D1850",
                ControlHoverColorHex = "#3C206B",
                SliderTrackColorHex = "#552D96",
                SliderThumbColorHex = "#FF007F",
                DialogBackgroundColorHex = "#1A0F2E",
                GlowEffect = true,
                GlowColorHex = "#FF007F"
            });

            Register(new ThemeDefinition
            {
                Id = "matrix-emerald-terminal",
                Name = "Matrix Emerald",
                Category = "High-Contrast & Gamer Skins",
                Description = "Authentic falling digital green code on pure black CRT",
                BackgroundColorHex = "#000501",
                SurfaceColorHex = "#001405",
                HeaderColorHex = "#000D03",
                BorderColorHex = "#004010",
                AccentColorHex = "#00FF41",
                AccentHoverColorHex = "#33FF66",
                ForegroundColorHex = "#00FF41",
                MutedForegroundColorHex = "#008F11",
                ControlBackgroundColorHex = "#002409",
                ControlHoverColorHex = "#00360D",
                SliderTrackColorHex = "#005214",
                SliderThumbColorHex = "#00FF41",
                DialogBackgroundColorHex = "#001004",
                GlowEffect = true,
                GlowColorHex = "#00FF41"
            });

            Register(new ThemeDefinition
            {
                Id = "blood-dragon",
                Name = "Blood Dragon",
                Category = "High-Contrast & Gamer Skins",
                Description = "Futuristic 80s VHS cyber war neon crimson and cyan",
                BackgroundColorHex = "#0F0008",
                SurfaceColorHex = "#210012",
                HeaderColorHex = "#18000D",
                BorderColorHex = "#570030",
                AccentColorHex = "#FF0055",
                AccentHoverColorHex = "#00FFFF",
                ForegroundColorHex = "#FFFFFF",
                MutedForegroundColorHex = "#FF66A3",
                ControlBackgroundColorHex = "#33001C",
                ControlHoverColorHex = "#470027",
                SliderTrackColorHex = "#660037",
                SliderThumbColorHex = "#FF0055",
                DialogBackgroundColorHex = "#1D0010",
                GlowEffect = true,
                GlowColorHex = "#FF0055"
            });

            Register(new ThemeDefinition
            {
                Id = "solarized-high-contrast",
                Name = "Solarized High-Contrast",
                Category = "High-Contrast & Gamer Skins",
                Description = "Maximum legibility solarized cyan and amber for bright glare",
                BackgroundColorHex = "#001E26",
                SurfaceColorHex = "#002B36",
                HeaderColorHex = "#00252F",
                BorderColorHex = "#073642",
                AccentColorHex = "#2AA198",
                AccentHoverColorHex = "#B58900",
                ForegroundColorHex = "#FDF6E3",
                MutedForegroundColorHex = "#93A1A1",
                ControlBackgroundColorHex = "#053745",
                ControlHoverColorHex = "#084455",
                SliderTrackColorHex = "#0E586D",
                SliderThumbColorHex = "#2AA198",
                DialogBackgroundColorHex = "#00242D"
            });

            Register(new ThemeDefinition
            {
                Id = "hyper-neon-pink",
                Name = "Hyper Neon Pink",
                Category = "High-Contrast & Gamer Skins",
                Description = "Blinding neon magenta laser on dark carbon background",
                BackgroundColorHex = "#12020A",
                SurfaceColorHex = "#240414",
                HeaderColorHex = "#1A030E",
                BorderColorHex = "#520A2F",
                AccentColorHex = "#FF1493",
                AccentHoverColorHex = "#FF69B4",
                ForegroundColorHex = "#FFF0F5",
                MutedForegroundColorHex = "#FF80BF",
                ControlBackgroundColorHex = "#36061F",
                ControlHoverColorHex = "#4A082A",
                SliderTrackColorHex = "#690C3C",
                SliderThumbColorHex = "#FF1493",
                DialogBackgroundColorHex = "#1F0311",
                GlowEffect = true,
                GlowColorHex = "#FF1493"
            });

            Register(new ThemeDefinition
            {
                Id = "toxic-lime",
                Name = "Toxic Lime",
                Category = "High-Contrast & Gamer Skins",
                Description = "Biohazard glowing lime green on pitch obsidian",
                BackgroundColorHex = "#050C03",
                SurfaceColorHex = "#0C1A07",
                HeaderColorHex = "#081305",
                BorderColorHex = "#1E4212",
                AccentColorHex = "#39FF14",
                AccentHoverColorHex = "#70FF54",
                ForegroundColorHex = "#F4FFF0",
                MutedForegroundColorHex = "#80E667",
                ControlBackgroundColorHex = "#142B0C",
                ControlHoverColorHex = "#1E3D12",
                SliderTrackColorHex = "#2D591B",
                SliderThumbColorHex = "#39FF14",
                DialogBackgroundColorHex = "#091705",
                GlowEffect = true,
                GlowColorHex = "#39FF14"
            });

            Register(new ThemeDefinition
            {
                Id = "acid-yellow-hazard",
                Name = "Acid Yellow Hazard",
                Category = "High-Contrast & Gamer Skins",
                Description = "High-voltage electric laser yellow industrial hazard theme",
                BackgroundColorHex = "#0D0C02",
                SurfaceColorHex = "#1D1A05",
                HeaderColorHex = "#151303",
                BorderColorHex = "#47400C",
                AccentColorHex = "#FFEA00",
                AccentHoverColorHex = "#FFF266",
                ForegroundColorHex = "#FFFFF0",
                MutedForegroundColorHex = "#D4C53B",
                ControlBackgroundColorHex = "#2E2A08",
                ControlHoverColorHex = "#3E380B",
                SliderTrackColorHex = "#574F10",
                SliderThumbColorHex = "#FFEA00",
                DialogBackgroundColorHex = "#191604",
                GlowEffect = true,
                GlowColorHex = "#FFEA00"
            });

            Register(new ThemeDefinition
            {
                Id = "electric-blue-lightning",
                Name = "Electric Blue Lightning",
                Category = "High-Contrast & Gamer Skins",
                Description = "Pure ion electric arc blue sparking against deep space black",
                BackgroundColorHex = "#020712",
                SurfaceColorHex = "#05112B",
                HeaderColorHex = "#030C1E",
                BorderColorHex = "#0D2C6E",
                AccentColorHex = "#00D4FF",
                AccentHoverColorHex = "#66E5FF",
                ForegroundColorHex = "#F0FBFF",
                MutedForegroundColorHex = "#66BFFF",
                ControlBackgroundColorHex = "#081B45",
                ControlHoverColorHex = "#0B2661",
                SliderTrackColorHex = "#10378A",
                SliderThumbColorHex = "#00D4FF",
                DialogBackgroundColorHex = "#040F24",
                GlowEffect = true,
                GlowColorHex = "#00D4FF"
            });

            Register(new ThemeDefinition
            {
                Id = "laser-amber-grid",
                Name = "Laser Amber Grid",
                Category = "High-Contrast & Gamer Skins",
                Description = "1970s mainframe amber phosphor CRT monitor emulation",
                BackgroundColorHex = "#0D0701",
                SurfaceColorHex = "#1F1103",
                HeaderColorHex = "#160C02",
                BorderColorHex = "#4A2807",
                AccentColorHex = "#FFB000",
                AccentHoverColorHex = "#FFC84D",
                ForegroundColorHex = "#FFF5E0",
                MutedForegroundColorHex = "#D49400",
                ControlBackgroundColorHex = "#311B05",
                ControlHoverColorHex = "#452607",
                SliderTrackColorHex = "#5E340A",
                SliderThumbColorHex = "#FFB000",
                DialogBackgroundColorHex = "#1A0E02",
                GlowEffect = true,
                GlowColorHex = "#FFB000"
            });

            Register(new ThemeDefinition
            {
                Id = "cyber-sunset-outrun",
                Name = "Cyber Sunset Outrun",
                Category = "High-Contrast & Gamer Skins",
                Description = "Outrun arcade sunset orange, violet, and deep magenta",
                BackgroundColorHex = "#140817",
                SurfaceColorHex = "#240E29",
                HeaderColorHex = "#1C0B20",
                BorderColorHex = "#521E5E",
                AccentColorHex = "#FF5E00",
                AccentHoverColorHex = "#E00070",
                ForegroundColorHex = "#FFF0F5",
                MutedForegroundColorHex = "#FF94B8",
                ControlBackgroundColorHex = "#36153E",
                ControlHoverColorHex = "#471C52",
                SliderTrackColorHex = "#632773",
                SliderThumbColorHex = "#FF5E00",
                DialogBackgroundColorHex = "#1E0C22",
                GlowEffect = true,
                GlowColorHex = "#FF5E00"
            });

            Register(new ThemeDefinition
            {
                Id = "radioactive-green",
                Name = "Radioactive Green",
                Category = "High-Contrast & Gamer Skins",
                Description = "Uranium glass fluorescence with intense bright lime glow",
                BackgroundColorHex = "#020A03",
                SurfaceColorHex = "#071709",
                HeaderColorHex = "#041006",
                BorderColorHex = "#133E19",
                AccentColorHex = "#00FF66",
                AccentHoverColorHex = "#66FF99",
                ForegroundColorHex = "#E6FFED",
                MutedForegroundColorHex = "#4DFF88",
                ControlBackgroundColorHex = "#0D2610",
                ControlHoverColorHex = "#123617",
                SliderTrackColorHex = "#1B4F22",
                SliderThumbColorHex = "#00FF66",
                DialogBackgroundColorHex = "#051307",
                GlowEffect = true,
                GlowColorHex = "#00FF66"
            });

            Register(new ThemeDefinition
            {
                Id = "oled-pure-black",
                Name = "OLED Pure Black",
                Category = "High-Contrast & Gamer Skins",
                Description = "Absolute zero true black #000000 pixels with crisp white text for OLED panels",
                BackgroundColorHex = "#000000",
                SurfaceColorHex = "#080808",
                HeaderColorHex = "#040404",
                BorderColorHex = "#202020",
                AccentColorHex = "#3B82F6",
                AccentHoverColorHex = "#60A5FA",
                ForegroundColorHex = "#FFFFFF",
                MutedForegroundColorHex = "#888888",
                ControlBackgroundColorHex = "#121212",
                ControlHoverColorHex = "#1A1A1A",
                SliderTrackColorHex = "#282828",
                SliderThumbColorHex = "#3B82F6",
                DialogBackgroundColorHex = "#0A0A0A"
            });

            Register(new ThemeDefinition
            {
                Id = "viper-crimson",
                Name = "Viper Crimson",
                Category = "High-Contrast & Gamer Skins",
                Description = "Aggressive esports gaming red and pitch black carbon chassis",
                BackgroundColorHex = "#0F0203",
                SurfaceColorHex = "#1F0407",
                HeaderColorHex = "#170305",
                BorderColorHex = "#4D0B12",
                AccentColorHex = "#FF0033",
                AccentHoverColorHex = "#FF4D73",
                ForegroundColorHex = "#FFF0F2",
                MutedForegroundColorHex = "#FF8099",
                ControlBackgroundColorHex = "#30070B",
                ControlHoverColorHex = "#420A10",
                SliderTrackColorHex = "#5E0E17",
                SliderThumbColorHex = "#FF0033",
                DialogBackgroundColorHex = "#1B0306",
                GlowEffect = true,
                GlowColorHex = "#FF0033"
            });

            Register(new ThemeDefinition
            {
                Id = "tron-cyan-grid",
                Name = "Tron Legacy Cyan",
                Category = "High-Contrast & Gamer Skins",
                Description = "The Grid: high-contrast neon cyan circuit traces on jet black",
                BackgroundColorHex = "#01070A",
                SurfaceColorHex = "#03131A",
                HeaderColorHex = "#020D12",
                BorderColorHex = "#0A3345",
                AccentColorHex = "#00E5FF",
                AccentHoverColorHex = "#66F0FF",
                ForegroundColorHex = "#E0FBFF",
                MutedForegroundColorHex = "#00B8D4",
                ControlBackgroundColorHex = "#06202B",
                ControlHoverColorHex = "#082C3C",
                SliderTrackColorHex = "#0D3F57",
                SliderThumbColorHex = "#00E5FF",
                DialogBackgroundColorHex = "#031017",
                GlowEffect = true,
                GlowColorHex = "#00E5FF"
            });

            Register(new ThemeDefinition
            {
                Id = "quantum-violet-laser",
                Name = "Quantum Violet",
                Category = "High-Contrast & Gamer Skins",
                Description = "Deep ultraviolet energy plasma for dark viewing rooms",
                BackgroundColorHex = "#0D0317",
                SurfaceColorHex = "#1A062E",
                HeaderColorHex = "#130422",
                BorderColorHex = "#430E75",
                AccentColorHex = "#BF00FF",
                AccentHoverColorHex = "#D966FF",
                ForegroundColorHex = "#F9F0FF",
                MutedForegroundColorHex = "#B366FF",
                ControlBackgroundColorHex = "#290A47",
                ControlHoverColorHex = "#370D5E",
                SliderTrackColorHex = "#4F1387",
                SliderThumbColorHex = "#BF00FF",
                DialogBackgroundColorHex = "#160526",
                GlowEffect = true,
                GlowColorHex = "#BF00FF"
            });

            Register(new ThemeDefinition
            {
                Id = "reactor-core-cyan",
                Name = "Cherenkov Reactor Core",
                Category = "High-Contrast & Gamer Skins",
                Description = "Eerie Cherenkov radiation blue glow deep under heavy water",
                BackgroundColorHex = "#010A14",
                SurfaceColorHex = "#03152B",
                HeaderColorHex = "#020F1F",
                BorderColorHex = "#0B376E",
                AccentColorHex = "#00B4D8",
                AccentHoverColorHex = "#90E0EF",
                ForegroundColorHex = "#CAF0F8",
                MutedForegroundColorHex = "#0077B6",
                ControlBackgroundColorHex = "#062142",
                ControlHoverColorHex = "#092C59",
                SliderTrackColorHex = "#0D3E7D",
                SliderThumbColorHex = "#00B4D8",
                DialogBackgroundColorHex = "#021224",
                GlowEffect = true,
                GlowColorHex = "#00B4D8"
            });

            Register(new ThemeDefinition
            {
                Id = "hazard-yellow-stripes",
                Name = "Industrial Hazard",
                Category = "High-Contrast & Gamer Skins",
                Description = "Extreme warning sign black and canary yellow contrast",
                BackgroundColorHex = "#0A0A0A",
                SurfaceColorHex = "#141414",
                HeaderColorHex = "#0F0F0F",
                BorderColorHex = "#333333",
                AccentColorHex = "#FFCC00",
                AccentHoverColorHex = "#FFE066",
                ForegroundColorHex = "#FFFFFF",
                MutedForegroundColorHex = "#FFCC00",
                ControlBackgroundColorHex = "#1F1F1F",
                ControlHoverColorHex = "#292929",
                SliderTrackColorHex = "#383838",
                SliderThumbColorHex = "#FFCC00",
                DialogBackgroundColorHex = "#121212"
            });

            Register(new ThemeDefinition
            {
                Id = "ultraviolet-fluorescence",
                Name = "Ultraviolet Blacklight",
                Category = "High-Contrast & Gamer Skins",
                Description = "Fluorescent neon glow beneath 365nm UV blacklight",
                BackgroundColorHex = "#0A0012",
                SurfaceColorHex = "#170129",
                HeaderColorHex = "#10011C",
                BorderColorHex = "#3F026E",
                AccentColorHex = "#E000FF",
                AccentHoverColorHex = "#00FFCC",
                ForegroundColorHex = "#F8F0FF",
                MutedForegroundColorHex = "#D466FF",
                ControlBackgroundColorHex = "#260242",
                ControlHoverColorHex = "#330359",
                SliderTrackColorHex = "#4A0480",
                SliderThumbColorHex = "#E000FF",
                GlowEffect = true,
                GlowColorHex = "#E000FF"
            });

            Register(new ThemeDefinition
            {
                Id = "magma-inferno",
                Name = "Magma Inferno",
                Category = "High-Contrast & Gamer Skins",
                Description = "Molten basalt volcanic lava red, fiery orange, and ash",
                BackgroundColorHex = "#120300",
                SurfaceColorHex = "#240700",
                HeaderColorHex = "#1A0500",
                BorderColorHex = "#5E1100",
                AccentColorHex = "#FF3700",
                AccentHoverColorHex = "#FF8400",
                ForegroundColorHex = "#FFF2F0",
                MutedForegroundColorHex = "#FF7B59",
                ControlBackgroundColorHex = "#380A00",
                ControlHoverColorHex = "#4C0E00",
                SliderTrackColorHex = "#6B1400",
                SliderThumbColorHex = "#FF3700",
                DialogBackgroundColorHex = "#1E0600",
                GlowEffect = true,
                GlowColorHex = "#FF3700"
            });

            Register(new ThemeDefinition
            {
                Id = "deep-space-supernova",
                Name = "Deep Space Supernova",
                Category = "High-Contrast & Gamer Skins",
                Description = "Stellar explosion cosmic purple and blinding starlight",
                BackgroundColorHex = "#0A0514",
                SurfaceColorHex = "#160C2B",
                HeaderColorHex = "#100920",
                BorderColorHex = "#391D6E",
                AccentColorHex = "#7928CA",
                AccentHoverColorHex = "#FF0080",
                ForegroundColorHex = "#FAF5FF",
                MutedForegroundColorHex = "#B87CF8",
                ControlBackgroundColorHex = "#231345",
                ControlHoverColorHex = "#2F1A5C",
                SliderTrackColorHex = "#432582",
                SliderThumbColorHex = "#FF0080",
                DialogBackgroundColorHex = "#130A24",
                GlowEffect = true,
                GlowColorHex = "#FF0080"
            });

            Register(new ThemeDefinition
            {
                Id = "glitch-blue-rgb",
                Name = "Glitch Art RGB",
                Category = "High-Contrast & Gamer Skins",
                Description = "Digital chromatic aberration blue, red, and neon cyan",
                BackgroundColorHex = "#020612",
                SurfaceColorHex = "#050F29",
                HeaderColorHex = "#030A1C",
                BorderColorHex = "#0D2566",
                AccentColorHex = "#0066FF",
                AccentHoverColorHex = "#FF003C",
                ForegroundColorHex = "#F0F5FF",
                MutedForegroundColorHex = "#66A3FF",
                ControlBackgroundColorHex = "#081840",
                ControlHoverColorHex = "#0C2259",
                SliderTrackColorHex = "#123282",
                SliderThumbColorHex = "#0066FF",
                DialogBackgroundColorHex = "#040C21",
                GlowEffect = true,
                GlowColorHex = "#0066FF"
            });

            Register(new ThemeDefinition
            {
                Id = "crimson-stealth",
                Name = "Crimson Stealth",
                Category = "High-Contrast & Gamer Skins",
                Description = "Tactical night-vision red for zero light disturbance",
                BackgroundColorHex = "#0D0000",
                SurfaceColorHex = "#1A0000",
                HeaderColorHex = "#140000",
                BorderColorHex = "#400000",
                AccentColorHex = "#D00000",
                AccentHoverColorHex = "#FF2020",
                ForegroundColorHex = "#FFCCCC",
                MutedForegroundColorHex = "#990000",
                ControlBackgroundColorHex = "#290000",
                ControlHoverColorHex = "#380000",
                SliderTrackColorHex = "#520000",
                SliderThumbColorHex = "#D00000",
                DialogBackgroundColorHex = "#170000"
            });

            Register(new ThemeDefinition
            {
                Id = "golden-aegis",
                Name = "Golden Aegis",
                Category = "High-Contrast & Gamer Skins",
                Description = "Mythic championship esports gold and onyx armor",
                BackgroundColorHex = "#0F0D06",
                SurfaceColorHex = "#1F1B0D",
                HeaderColorHex = "#17140A",
                BorderColorHex = "#473D1E",
                AccentColorHex = "#FFD700",
                AccentHoverColorHex = "#FFE55C",
                ForegroundColorHex = "#FFFDF0",
                MutedForegroundColorHex = "#D4AF37",
                ControlBackgroundColorHex = "#302B15",
                ControlHoverColorHex = "#40391C",
                SliderTrackColorHex = "#574E27",
                SliderThumbColorHex = "#FFD700",
                DialogBackgroundColorHex = "#1B170B",
                GlowEffect = true,
                GlowColorHex = "#FFD700"
            });

            Register(new ThemeDefinition
            {
                Id = "monochrome-high-contrast",
                Name = "Monochrome Accessibility",
                Category = "High-Contrast & Gamer Skins",
                Description = "Certified WCAG AAA 21:1 pure black and white high-contrast skin",
                BackgroundColorHex = "#000000",
                SurfaceColorHex = "#111111",
                HeaderColorHex = "#080808",
                BorderColorHex = "#FFFFFF",
                AccentColorHex = "#FFFFFF",
                AccentHoverColorHex = "#DDDDDD",
                ForegroundColorHex = "#FFFFFF",
                MutedForegroundColorHex = "#CCCCCC",
                ControlBackgroundColorHex = "#1A1A1A",
                ControlHoverColorHex = "#2E2E2E",
                SliderTrackColorHex = "#555555",
                SliderThumbColorHex = "#FFFFFF",
                DialogBackgroundColorHex = "#0A0A0A"
            });
        }
    }
}
