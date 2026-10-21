param(
    [string]$OutputDirectory = "P:\prabhu\projects\git\omni-player\src\OmniPlayer.UI\Themes\Packs"
)

if (-not (Test-Path $OutputDirectory)) {
    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
}

$themes = @(
    # Category 1: Classic Player Archetypes (25)
    @{ Id="kmplayer-carbon"; Name="KMPlayer Carbon"; Cat="Classic Player Archetypes"; Bg="#141416"; Surface="#1D1E22"; Accent="#805AD5"; Fg="#E2E8F0" },
    @{ Id="potplayer-black"; Name="PotPlayer Black"; Cat="Classic Player Archetypes"; Bg="#0C0C0D"; Surface="#161618"; Accent="#E53E3E"; Fg="#F7FAFC" },
    @{ Id="vlc-modern-orange"; Name="VLC Modern Orange"; Cat="Classic Player Archetypes"; Bg="#18181B"; Surface="#27272A"; Accent="#F97316"; Fg="#FAFAFA" },
    @{ Id="gom-minimal-orange"; Name="GOM Minimal"; Cat="Classic Player Archetypes"; Bg="#17181A"; Surface="#222428"; Accent="#FF8C00"; Fg="#ECEFF4" },
    @{ Id="mpc-retro-silver"; Name="MPC Retro Silver"; Cat="Classic Player Archetypes"; Bg="#2B2D30"; Surface="#383B40"; Accent="#4A90E2"; Fg="#F0F2F5" },
    @{ Id="winamp-classic-gold"; Name="Winamp Classic Gold"; Cat="Classic Player Archetypes"; Bg="#0F1210"; Surface="#1A1F1B"; Accent="#10B981"; Fg="#E6FFFA" },
    @{ Id="jetaudio-titanium"; Name="JetAudio Titanium"; Cat="Classic Player Archetypes"; Bg="#1B1D20"; Surface="#24272C"; Accent="#06B6D4"; Fg="#F1F5F9" },
    @{ Id="quicktime-dark-onyx"; Name="QuickTime Dark Onyx"; Cat="Classic Player Archetypes"; Bg="#101012"; Surface="#1B1B1E"; Accent="#3B82F6"; Fg="#F8FAFC" },
    @{ Id="bsplayer-slate"; Name="BSPlayer Slate"; Cat="Classic Player Archetypes"; Bg="#151B24"; Surface="#1E2733"; Accent="#22C55E"; Fg="#F0FDF4" },
    @{ Id="realplayer-neo-cyan"; Name="RealPlayer Neo Cyan"; Cat="Classic Player Archetypes"; Bg="#0E171E"; Surface="#16232D"; Accent="#14B8A6"; Fg="#F0FDFA" },
    @{ Id="powerdvd-cinematic"; Name="CyberLink PowerDVD Dark"; Cat="Classic Player Archetypes"; Bg="#0A0E17"; Surface="#121A29"; Accent="#2563EB"; Fg="#EFF6FF" },
    @{ Id="foobar-vintage-black"; Name="Foobar Vintage"; Cat="Classic Player Archetypes"; Bg="#121212"; Surface="#1C1C1C"; Accent="#94A3B8"; Fg="#FFFFFF" },
    @{ Id="aimp-obsidian-amber"; Name="AIMP Obsidian"; Cat="Classic Player Archetypes"; Bg="#131416"; Surface="#1C1E22"; Accent="#D97706"; Fg="#FFFBEB" },
    @{ Id="mediamonkey-gold"; Name="MediaMonkey Gold"; Cat="Classic Player Archetypes"; Bg="#141210"; Surface="#1F1C18"; Accent="#EAB308"; Fg="#FEF9C3" },
    @{ Id="smplayer-fusion"; Name="SMPlayer Fusion"; Cat="Classic Player Archetypes"; Bg="#1F232A"; Surface="#2A303A"; Accent="#38BDF8"; Fg="#F0F9FF" },
    @{ Id="zoomplayer-graphite"; Name="ZoomPlayer Graphite"; Cat="Classic Player Archetypes"; Bg="#181A1D"; Surface="#22252A"; Accent="#EA580C"; Fg="#FFF7ED" },
    @{ Id="divx-player-pro"; Name="DivX Player Pro"; Cat="Classic Player Archetypes"; Bg="#0C131F"; Surface="#142033"; Accent="#0284C7"; Fg="#F0F9FF" },
    @{ Id="wmp11-onyx"; Name="Windows Media Player 11 Onyx"; Cat="Classic Player Archetypes"; Bg="#0F1115"; Surface="#181C22"; Accent="#00D2FF"; Fg="#FFFFFF" },
    @{ Id="wmp12-aero"; Name="WMP 12 Aero Glass"; Cat="Classic Player Archetypes"; Bg="#1A222C"; Surface="#24303E"; Accent="#3B82F6"; Fg="#F8FAFC" },
    @{ Id="corel-windvd"; Name="Corel WinDVD Cinema"; Cat="Classic Player Archetypes"; Bg="#180F12"; Surface="#26171C"; Accent="#E11D48"; Fg="#FFF1F2" },
    @{ Id="itunes-retro-dark"; Name="iTunes Retro Dark"; Cat="Classic Player Archetypes"; Bg="#1A1A1A"; Surface="#262626"; Accent="#EC4899"; Fg="#FDF2F8" },
    @{ Id="songbird-dark"; Name="Songbird Dusk"; Cat="Classic Player Archetypes"; Bg="#16121C"; Surface="#231C2D"; Accent="#A855F7"; Fg="#FAF5FF" },
    @{ Id="clementine-dusk"; Name="Clementine Dusk"; Cat="Classic Player Archetypes"; Bg="#121820"; Surface="#1B2430"; Accent="#F97316"; Fg="#FFF7ED" },
    @{ Id="audacious-classic"; Name="Audacious Classic"; Cat="Classic Player Archetypes"; Bg="#1E1E1E"; Surface="#282828"; Accent="#60A5FA"; Fg="#FFFFFF" },
    @{ Id="xmplay-matrix"; Name="XMPlay Matrix"; Cat="Classic Player Archetypes"; Bg="#08100A"; Surface="#0F1E13"; Accent="#22C55E"; Fg="#DCFCE7" },

    # Category 2: Modern Flat & Fluent (35)
    @{ Id="obsidian-dark"; Name="Obsidian Dark"; Cat="Modern Flat & Fluent"; Bg="#0B0C0E"; Surface="#15171C"; Accent="#6366F1"; Fg="#F8FAFC" },
    @{ Id="cyber-teal"; Name="Cyber Teal"; Cat="Modern Flat & Fluent"; Bg="#0E1517"; Surface="#162124"; Accent="#14B8A6"; Fg="#F0FDFA" },
    @{ Id="pure-white"; Name="Pure White Minimal"; Cat="Modern Flat & Fluent"; Bg="#F8FAFC"; Surface="#FFFFFF"; Accent="#2563EB"; Fg="#0F172A" },
    @{ Id="slate-graphite"; Name="Slate Graphite"; Cat="Modern Flat & Fluent"; Bg="#1E293B"; Surface="#334155"; Accent="#0EA5E9"; Fg="#F8FAFC" },
    @{ Id="tokyo-night"; Name="Tokyo Night"; Cat="Modern Flat & Fluent"; Bg="#1A1B26"; Surface="#24283B"; Accent="#7AA2F7"; Fg="#C0CAF5" },
    @{ Id="catppuccin-mocha"; Name="Catppuccin Mocha"; Cat="Modern Flat & Fluent"; Bg="#1E1E2E"; Surface="#313244"; Accent="#CBA6F7"; Fg="#CDD6F4" },
    @{ Id="catppuccin-macchiato"; Name="Catppuccin Macchiato"; Cat="Modern Flat & Fluent"; Bg="#24273A"; Surface="#363A4F"; Accent="#8AADF4"; Fg="#CAD3F5" },
    @{ Id="catppuccin-frappe"; Name="Catppuccin Frappé"; Cat="Modern Flat & Fluent"; Bg="#303446"; Surface="#414559"; Accent="#EA999C"; Fg="#C6D0F5" },
    @{ Id="catppuccin-latte"; Name="Catppuccin Latte"; Cat="Modern Flat & Fluent"; Bg="#EFF1F5"; Surface="#CCD0DA"; Accent="#8839EF"; Fg="#4C4F69" },
    @{ Id="nord-arctic"; Name="Nord Arctic"; Cat="Modern Flat & Fluent"; Bg="#2E3440"; Surface="#3B4252"; Accent="#88C0D0"; Fg="#ECEFF4" },
    @{ Id="dracula-official"; Name="Dracula Official"; Cat="Modern Flat & Fluent"; Bg="#282A36"; Surface="#343746"; Accent="#BD93F9"; Fg="#F8F8F2" },
    @{ Id="gruvbox-dark"; Name="Gruvbox Dark"; Cat="Modern Flat & Fluent"; Bg="#282828"; Surface="#3C3836"; Accent="#FABD2F"; Fg="#EBDBB2" },
    @{ Id="gruvbox-light"; Name="Gruvbox Light"; Cat="Modern Flat & Fluent"; Bg="#FBF1C7"; Surface="#EBDBB2"; Accent="#AF3A03"; Fg="#3C3836" },
    @{ Id="one-dark-pro"; Name="One Dark Pro"; Cat="Modern Flat & Fluent"; Bg="#21252B"; Surface="#282C34"; Accent="#61AFEF"; Fg="#ABB2BF" },
    @{ Id="monokai-pro"; Name="Monokai Pro"; Cat="Modern Flat & Fluent"; Bg="#2D2A2E"; Surface="#3A363B"; Accent="#FFD866"; Fg="#FCFCFA" },
    @{ Id="github-dark"; Name="GitHub Dark Default"; Cat="Modern Flat & Fluent"; Bg="#0D1117"; Surface="#161B22"; Accent="#2F81F7"; Fg="#F0F6FC" },
    @{ Id="material-deep-ocean"; Name="Material Deep Ocean"; Cat="Modern Flat & Fluent"; Bg="#0F111A"; Surface="#1A1C25"; Accent="#80CBC4"; Fg="#EEFFFF" },
    @{ Id="palenight"; Name="Palenight"; Cat="Modern Flat & Fluent"; Bg="#292D3E"; Surface="#343A50"; Accent="#C792EA"; Fg="#ECEFF1" },
    @{ Id="synthwave-84-flat"; Name="Synthwave '84 Flat"; Cat="Modern Flat & Fluent"; Bg="#262335"; Surface="#342E48"; Accent="#FF7EDB"; Fg="#FDFDFD" },
    @{ Id="horizon-dark"; Name="Horizon Dark"; Cat="Modern Flat & Fluent"; Bg="#1C1E26"; Surface="#232530"; Accent="#E95678"; Fg="#D5D8DA" },
    @{ Id="ayu-dark"; Name="Ayu Dark"; Cat="Modern Flat & Fluent"; Bg="#0B0E14"; Surface="#131721"; Accent="#E6B450"; Fg="#BFBDB6" },
    @{ Id="ayu-mirage"; Name="Ayu Mirage"; Cat="Modern Flat & Fluent"; Bg="#1F2430"; Surface="#272D3B"; Accent="#FF8F40"; Fg="#CBCCC6" },
    @{ Id="night-owl"; Name="Night Owl"; Cat="Modern Flat & Fluent"; Bg="#011627"; Surface="#0B253A"; Accent="#82AAFF"; Fg="#D6DEEB" },
    @{ Id="cobalt2"; Name="Cobalt2"; Cat="Modern Flat & Fluent"; Bg="#193549"; Surface="#1F4662"; Accent="#FFC600"; Fg="#FFFFFF" },
    @{ Id="solarized-dark"; Name="Solarized Dark"; Cat="Modern Flat & Fluent"; Bg="#002B36"; Surface="#073642"; Accent="#2AA198"; Fg="#839496" },
    @{ Id="solarized-light"; Name="Solarized Light"; Cat="Modern Flat & Fluent"; Bg="#FDF6E3"; Surface="#EEE8D5"; Accent="#268BD2"; Fg="#657B83" },
    @{ Id="rose-pine"; Name="Rosé Pine"; Cat="Modern Flat & Fluent"; Bg="#191724"; Surface="#26233A"; Accent="#EB6F92"; Fg="#E0DEF4" },
    @{ Id="rose-pine-moon"; Name="Rosé Pine Moon"; Cat="Modern Flat & Fluent"; Bg="#232136"; Surface="#2A273F"; Accent="#EA9A97"; Fg="#E0DEF4" },
    @{ Id="kanagawa-wave"; Name="Kanagawa Wave"; Cat="Modern Flat & Fluent"; Bg="#1F1F28"; Surface="#2A2A37"; Accent="#7E9CD8"; Fg="#DCD7BA" },
    @{ Id="everforest-dark"; Name="Everforest Dark"; Cat="Modern Flat & Fluent"; Bg="#2D353B"; Surface="#343F44"; Accent="#A7C080"; Fg="#D3C6AA" },
    @{ Id="moonlight-ocean"; Name="Moonlight Ocean"; Cat="Modern Flat & Fluent"; Bg="#1E2030"; Surface="#222436"; Accent="#82AAFF"; Fg="#C8D3F5" },
    @{ Id="panda-theme"; Name="Panda Dark"; Cat="Modern Flat & Fluent"; Bg="#292A2B"; Surface="#303133"; Accent="#19F9D8"; Fg="#E6E6E6" },
    @{ Id="shades-of-purple"; Name="Shades of Purple"; Cat="Modern Flat & Fluent"; Bg="#222244"; Surface="#2D2B55"; Accent="#FAD000"; Fg="#FFFFFF" },
    @{ Id="poimandres-dark"; Name="Poimandres"; Cat="Modern Flat & Fluent"; Bg="#1B1E2B"; Surface="#252B3B"; Accent="#5DE4C7"; Fg="#E4F0FB" },
    @{ Id="vesper-minimal"; Name="Vesper Minimal"; Cat="Modern Flat & Fluent"; Bg="#101010"; Surface="#181818"; Accent="#FFC799"; Fg="#FFFFFF" },

    # Category 3: Studio Glass & Acrylic (25)
    @{ Id="win11-mica-glass"; Name="Windows 11 Mica Glass"; Cat="Studio Glass & Acrylic"; Bg="#18181C"; Surface="#202026"; Accent="#60CDFF"; Fg="#FFFFFF" },
    @{ Id="deep-frost-acrylic"; Name="Deep Frost Acrylic"; Cat="Studio Glass & Acrylic"; Bg="#0C1420"; Surface="#142133"; Accent="#38BDF8"; Fg="#F0F9FF" },
    @{ Id="frosted-sapphire"; Name="Frosted Sapphire"; Cat="Studio Glass & Acrylic"; Bg="#0A1026"; Surface="#111B3D"; Accent="#6366F1"; Fg="#EEF2FF" },
    @{ Id="smoked-quartz"; Name="Smoked Quartz"; Cat="Studio Glass & Acrylic"; Bg="#1B1715"; Surface="#28221F"; Accent="#D97706"; Fg="#FEF3C7" },
    @{ Id="emerald-tint-glass"; Name="Emerald Tint"; Cat="Studio Glass & Acrylic"; Bg="#0B1711"; Surface="#12251B"; Accent="#10B981"; Fg="#ECFDF5" },
    @{ Id="amber-glow-acrylic"; Name="Amber Glow"; Cat="Studio Glass & Acrylic"; Bg="#1A150A"; Surface="#292110"; Accent="#F59E0B"; Fg="#FFFBEB" },
    @{ Id="violet-mist-glass"; Name="Violet Mist"; Cat="Studio Glass & Acrylic"; Bg="#170F20"; Surface="#241733"; Accent="#C084FC"; Fg="#FAF5FF" },
    @{ Id="obsidian-frost"; Name="Obsidian Frost"; Cat="Studio Glass & Acrylic"; Bg="#121214"; Surface="#1A1A1E"; Accent="#E2E8F0"; Fg="#FFFFFF" },
    @{ Id="arctic-blue-glass"; Name="Arctic Blue Glass"; Cat="Studio Glass & Acrylic"; Bg="#0C1724"; Surface="#132438"; Accent="#00D2FF"; Fg="#F0F9FF" },
    @{ Id="rose-quartz-glass"; Name="Rose Quartz Glass"; Cat="Studio Glass & Acrylic"; Bg="#1C1115"; Surface="#2B1A20"; Accent="#F472B6"; Fg="#FDF2F8" },
    @{ Id="liquid-chrome-glass"; Name="Liquid Chrome Glass"; Cat="Studio Glass & Acrylic"; Bg="#17181A"; Surface="#232529"; Accent="#94A3B8"; Fg="#F8FAFC" },
    @{ Id="aurora-glass"; Name="Aurora Borealis Glass"; Cat="Studio Glass & Acrylic"; Bg="#0B1519"; Surface="#122229"; Accent="#2DD4BF"; Fg="#F0FDFA" },
    @{ Id="cyber-neon-glass"; Name="Cyberpunk Neon Glass"; Cat="Studio Glass & Acrylic"; Bg="#110D1A"; Surface="#1C152B"; Accent="#F43F5E"; Fg="#FFF1F2" },
    @{ Id="sunset-tint-glass"; Name="Sunset Tint"; Cat="Studio Glass & Acrylic"; Bg="#1C1214"; Surface="#2B1B1E"; Accent="#FB923C"; Fg="#FFF7ED" },
    @{ Id="champagne-ice"; Name="Champagne Ice Glass"; Cat="Studio Glass & Acrylic"; Bg="#191712"; Surface="#26231B"; Accent="#FDE047"; Fg="#FEFCE8" },
    @{ Id="titanium-brushed-glass"; Name="Titanium Brushed Glass"; Cat="Studio Glass & Acrylic"; Bg="#15181C"; Surface="#20252B"; Accent="#38BDF8"; Fg="#F8FAFC" },
    @{ Id="glacial-white-glass"; Name="Glacial White Glass"; Cat="Studio Glass & Acrylic"; Bg="#EBF4F6"; Surface="#F5FAFA"; Accent="#0284C7"; Fg="#0F172A" },
    @{ Id="mystic-topaz-glass"; Name="Mystic Topaz Glass"; Cat="Studio Glass & Acrylic"; Bg="#13101C"; Surface="#1F1A2D"; Accent="#EC4899"; Fg="#FAF5FF" },
    @{ Id="aquamarine-glass"; Name="Aquamarine Glass"; Cat="Studio Glass & Acrylic"; Bg="#0C1819"; Surface="#132729"; Accent="#2DD4BF"; Fg="#F0FDFA" },
    @{ Id="coral-reef-glass"; Name="Coral Reef Glass"; Cat="Studio Glass & Acrylic"; Bg="#1B1213"; Surface="#2A1C1D"; Accent="#F87171"; Fg="#FEF2F2" },
    @{ Id="phantom-velvet-glass"; Name="Phantom Velvet Glass"; Cat="Studio Glass & Acrylic"; Bg="#120F17"; Surface="#1D1826"; Accent="#9333EA"; Fg="#FAF5FF" },
    @{ Id="opal-iridescent"; Name="Opal Iridescent"; Cat="Studio Glass & Acrylic"; Bg="#161922"; Surface="#222735"; Accent="#38BDF8"; Fg="#F8FAFC" },
    @{ Id="midnight-frost"; Name="Midnight Frost"; Cat="Studio Glass & Acrylic"; Bg="#080B12"; Surface="#101624"; Accent="#60A5FA"; Fg="#F8FAFC" },
    @{ Id="citrine-warm-glass"; Name="Citrine Warm Glass"; Cat="Studio Glass & Acrylic"; Bg="#1A170F"; Surface="#282317"; Accent="#EAB308"; Fg="#FEFCE8" },
    @{ Id="cobalt-crystal"; Name="Cobalt Crystal Glass"; Cat="Studio Glass & Acrylic"; Bg="#091224"; Surface="#101E3C"; Accent="#2563EB"; Fg="#EFF6FF" },

    # Category 4: High-Contrast & Gamer Skins (25)
    @{ Id="neon-cyberpunk"; Name="Neon Cyberpunk"; Cat="High-Contrast & Gamer Skins"; Bg="#08060D"; Surface="#140F22"; Accent="#FFE600"; Fg="#00F0FF" },
    @{ Id="synthwave-84-highvolt"; Name="Synthwave 84 High-Volt"; Cat="High-Contrast & Gamer Skins"; Bg="#120B20"; Surface="#201238"; Accent="#FF007F"; Fg="#FFFFFF" },
    @{ Id="matrix-emerald-terminal"; Name="Matrix Emerald"; Cat="High-Contrast & Gamer Skins"; Bg="#000501"; Surface="#001405"; Accent="#00FF41"; Fg="#00FF41" },
    @{ Id="blood-dragon"; Name="Blood Dragon"; Cat="High-Contrast & Gamer Skins"; Bg="#0F0008"; Surface="#210012"; Accent="#FF0055"; Fg="#FFFFFF" },
    @{ Id="solarized-high-contrast"; Name="Solarized High-Contrast"; Cat="High-Contrast & Gamer Skins"; Bg="#001E26"; Surface="#002B36"; Accent="#2AA198"; Fg="#FDF6E3" },
    @{ Id="hyper-neon-pink"; Name="Hyper Neon Pink"; Cat="High-Contrast & Gamer Skins"; Bg="#12020A"; Surface="#240414"; Accent="#FF1493"; Fg="#FFF0F5" },
    @{ Id="toxic-lime"; Name="Toxic Lime"; Cat="High-Contrast & Gamer Skins"; Bg="#050C03"; Surface="#0C1A07"; Accent="#39FF14"; Fg="#F4FFF0" },
    @{ Id="acid-yellow-hazard"; Name="Acid Yellow Hazard"; Cat="High-Contrast & Gamer Skins"; Bg="#0D0C02"; Surface="#1D1A05"; Accent="#FFEA00"; Fg="#FFFFF0" },
    @{ Id="electric-blue-lightning"; Name="Electric Blue Lightning"; Cat="High-Contrast & Gamer Skins"; Bg="#020712"; Surface="#05112B"; Accent="#00D4FF"; Fg="#F0FBFF" },
    @{ Id="laser-amber-grid"; Name="Laser Amber Grid"; Cat="High-Contrast & Gamer Skins"; Bg="#0D0701"; Surface="#1F1103"; Accent="#FFB000"; Fg="#FFF5E0" },
    @{ Id="cyber-sunset-outrun"; Name="Cyber Sunset Outrun"; Cat="High-Contrast & Gamer Skins"; Bg="#140817"; Surface="#240E29"; Accent="#FF5E00"; Fg="#FFF0F5" },
    @{ Id="radioactive-green"; Name="Radioactive Green"; Cat="High-Contrast & Gamer Skins"; Bg="#020A03"; Surface="#071709"; Accent="#00FF66"; Fg="#E6FFED" },
    @{ Id="oled-pure-black"; Name="OLED Pure Black"; Cat="High-Contrast & Gamer Skins"; Bg="#000000"; Surface="#080808"; Accent="#3B82F6"; Fg="#FFFFFF" },
    @{ Id="viper-crimson"; Name="Viper Crimson"; Cat="High-Contrast & Gamer Skins"; Bg="#0F0203"; Surface="#1F0407"; Accent="#FF0033"; Fg="#FFF0F2" },
    @{ Id="tron-cyan-grid"; Name="Tron Legacy Cyan"; Cat="High-Contrast & Gamer Skins"; Bg="#01070A"; Surface="#03131A"; Accent="#00E5FF"; Fg="#E0FBFF" },
    @{ Id="quantum-violet-laser"; Name="Quantum Violet"; Cat="High-Contrast & Gamer Skins"; Bg="#0D0317"; Surface="#1A062E"; Accent="#BF00FF"; Fg="#F9F0FF" },
    @{ Id="reactor-core-cyan"; Name="Cherenkov Reactor Core"; Cat="High-Contrast & Gamer Skins"; Bg="#010A14"; Surface="#03152B"; Accent="#00B4D8"; Fg="#CAF0F8" },
    @{ Id="hazard-yellow-stripes"; Name="Industrial Hazard"; Cat="High-Contrast & Gamer Skins"; Bg="#0A0A0A"; Surface="#141414"; Accent="#FFCC00"; Fg="#FFFFFF" },
    @{ Id="ultraviolet-fluorescence"; Name="Ultraviolet Blacklight"; Cat="High-Contrast & Gamer Skins"; Bg="#0A0012"; Surface="#170129"; Accent="#E000FF"; Fg="#F8F0FF" },
    @{ Id="magma-inferno"; Name="Magma Inferno"; Cat="High-Contrast & Gamer Skins"; Bg="#120300"; Surface="#240700"; Accent="#FF3700"; Fg="#FFF2F0" },
    @{ Id="deep-space-supernova"; Name="Deep Space Supernova"; Cat="High-Contrast & Gamer Skins"; Bg="#0A0514"; Surface="#160C2B"; Accent="#7928CA"; Fg="#FAF5FF" },
    @{ Id="glitch-blue-rgb"; Name="Glitch Art RGB"; Cat="High-Contrast & Gamer Skins"; Bg="#020612"; Surface="#050F29"; Accent="#0066FF"; Fg="#F0F5FF" },
    @{ Id="crimson-stealth"; Name="Crimson Stealth"; Cat="High-Contrast & Gamer Skins"; Bg="#0D0000"; Surface="#1A0000"; Accent="#D00000"; Fg="#FFCCCC" },
    @{ Id="golden-aegis"; Name="Golden Aegis"; Cat="High-Contrast & Gamer Skins"; Bg="#0F0D06"; Surface="#1F1B0D"; Accent="#FFD700"; Fg="#FFFDF0" },
    @{ Id="monochrome-high-contrast"; Name="Monochrome Accessibility"; Cat="High-Contrast & Gamer Skins"; Bg="#000000"; Surface="#111111"; Accent="#FFFFFF"; Fg="#FFFFFF" }
)

Write-Host "Exporting $($themes.Count) theme manifest files to $OutputDirectory..."

foreach ($t in $themes) {
    $catEscaped = $t.Cat.Replace('&', '&amp;')
    $nameEscaped = $t.Name.Replace('&', '&amp;')
    $content = @"
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:sys="clr-namespace:System;assembly=mscorlib">
    <!-- Omni Player Theme Manifest: $nameEscaped ($catEscaped) -->
    <sys:String x:Key="ThemeId">$($t.Id)</sys:String>
    <sys:String x:Key="ThemeName">$nameEscaped</sys:String>
    <sys:String x:Key="ThemeCategory">$catEscaped</sys:String>
    <CornerRadius x:Key="OmniCornerRadius">6</CornerRadius>
    <sys:Double x:Key="GlassBlurOpacity">0.85</sys:Double>

    <SolidColorBrush x:Key="OmniBackgroundBrush" Color="$($t.Bg)" />
    <SolidColorBrush x:Key="OmniSurfaceBrush" Color="$($t.Surface)" />
    <SolidColorBrush x:Key="OmniHeaderBrush" Color="$($t.Bg)" />
    <SolidColorBrush x:Key="OmniBorderBrush" Color="#333333" />
    <SolidColorBrush x:Key="OmniAccentBrush" Color="$($t.Accent)" />
    <SolidColorBrush x:Key="OmniAccentHoverBrush" Color="$($t.Accent)" />
    <SolidColorBrush x:Key="OmniForegroundBrush" Color="$($t.Fg)" />
    <SolidColorBrush x:Key="OmniMutedForegroundBrush" Color="#888888" />
    <SolidColorBrush x:Key="OmniControlBackgroundBrush" Color="$($t.Surface)" />
    <SolidColorBrush x:Key="OmniControlHoverBrush" Color="#3A3A3A" />
    <SolidColorBrush x:Key="OmniSliderTrackBrush" Color="#444444" />
    <SolidColorBrush x:Key="OmniSliderThumbBrush" Color="$($t.Accent)" />
    <SolidColorBrush x:Key="OmniDialogBackgroundBrush" Color="$($t.Surface)" />
</ResourceDictionary>
"@
    $targetPath = Join-Path $OutputDirectory "$($t.Id).xaml"
    [System.IO.File]::WriteAllText($targetPath, $content)
}

Write-Host "Successfully generated $($themes.Count) theme XAML packs in $OutputDirectory!"
