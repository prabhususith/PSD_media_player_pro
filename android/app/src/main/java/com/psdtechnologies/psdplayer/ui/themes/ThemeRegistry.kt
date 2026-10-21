package com.psdtechnologies.psdplayer.ui.themes

import com.psdtechnologies.psdplayer.R

/**
 * Registry of 110+ Theme Skins ported to Android.
 * Author: Prabhu Kannan (PSD Technologies)
 */
object ThemeRegistry {

    data class ThemeItem(
        val id: String,
        val name: String,
        val category: String,
        val accentHex: String,
        val styleResId: Int
    )

    val themes: List<ThemeItem> = listOf(
        // Classic Player Archetypes
        ThemeItem("kmplayer_carbon", "KMPlayer Carbon", "Classic Archetype", "#E67E22", R.style.Theme_PSDPlayer_KmplayerCarbon),
        ThemeItem("potplayer_black", "PotPlayer Black", "Classic Archetype", "#F1C40F", R.style.Theme_PSDPlayer_PotPlayerBlack),
        ThemeItem("vlc_orange", "VLC Modern Orange", "Classic Archetype", "#FF8800", R.style.Theme_PSDPlayer_VlcOrange),
        ThemeItem("gom_minimal", "GOM Minimal Orange", "Classic Archetype", "#FF6B00", R.style.Theme_PSDPlayer_VlcOrange),
        ThemeItem("mpc_retro", "MPC Retro Silver", "Classic Archetype", "#A0A0A0", R.style.Theme_PSDPlayer_KmplayerCarbon),
        ThemeItem("winamp_gold", "Winamp Classic Gold", "Classic Archetype", "#FFD700", R.style.Theme_PSDPlayer_PotPlayerBlack),
        ThemeItem("jetaudio_titanium", "JetAudio Titanium", "Classic Archetype", "#B0C4DE", R.style.Theme_PSDPlayer_KmplayerCarbon),
        ThemeItem("quicktime_onyx", "QuickTime Dark Onyx", "Classic Archetype", "#4682B4", R.style.Theme_PSDPlayer_KmplayerCarbon),
        ThemeItem("bsplayer_slate", "BSPlayer Slate", "Classic Archetype", "#708090", R.style.Theme_PSDPlayer_KmplayerCarbon),
        ThemeItem("realplayer_cyan", "RealPlayer Neo Cyan", "Classic Archetype", "#00FFFF", R.style.Theme_PSDPlayer_CyberTeal),

        // Modern Flat & Fluent
        ThemeItem("obsidian_dark", "Obsidian Dark", "Modern Flat", "#66FCF1", R.style.Theme_PSDPlayer_ObsidianDark),
        ThemeItem("oled_black", "OLED Pure Black", "Modern Flat", "#00E5FF", R.style.Theme_PSDPlayer_OledBlack),
        ThemeItem("cyber_teal", "Cyber Teal", "Modern Flat", "#00F5D4", R.style.Theme_PSDPlayer_CyberTeal),
        ThemeItem("tokyo_night", "Tokyo Night", "Modern Flat", "#7AA2F7", R.style.Theme_PSDPlayer_TokyoNight),
        ThemeItem("dracula", "Dracula Official", "Modern Flat", "#BD93F9", R.style.Theme_PSDPlayer_Dracula),
        ThemeItem("catppuccin_mocha", "Catppuccin Mocha", "Modern Flat", "#F5C2E7", R.style.Theme_PSDPlayer_Dracula),
        ThemeItem("nord_arctic", "Nord Arctic", "Modern Flat", "#88C0D0", R.style.Theme_PSDPlayer_TokyoNight),
        ThemeItem("gruvbox_dark", "Gruvbox Dark", "Modern Flat", "#FE8019", R.style.Theme_PSDPlayer_KmplayerCarbon),
        ThemeItem("one_dark_pro", "One Dark Pro", "Modern Flat", "#61AFEF", R.style.Theme_PSDPlayer_TokyoNight),
        ThemeItem("monokai_pro", "Monokai Pro", "Modern Flat", "#FFD866", R.style.Theme_PSDPlayer_PotPlayerBlack),

        // Studio Glass & Acrylic
        ThemeItem("win11_mica", "Windows 11 Mica Glass", "Studio Glass", "#38BDF8", R.style.Theme_PSDPlayer_ObsidianDark),
        ThemeItem("deep_frost", "Deep Frost Acrylic", "Studio Glass", "#67E8F9", R.style.Theme_PSDPlayer_CyberTeal),
        ThemeItem("frosted_sapphire", "Frosted Sapphire", "Studio Glass", "#3B82F6", R.style.Theme_PSDPlayer_TokyoNight),
        ThemeItem("smoked_quartz", "Smoked Quartz", "Studio Glass", "#D1D5DB", R.style.Theme_PSDPlayer_KmplayerCarbon),
        ThemeItem("emerald_tint", "Emerald Tint Glass", "Studio Glass", "#10B981", R.style.Theme_PSDPlayer_MatrixEmerald),
        ThemeItem("amber_glow", "Amber Glow Glass", "Studio Glass", "#F59E0B", R.style.Theme_PSDPlayer_KmplayerCarbon),

        // Gamer High-Contrast
        ThemeItem("synthwave_84", "Synthwave '84", "Gamer High-Contrast", "#FF2A85", R.style.Theme_PSDPlayer_Synthwave84),
        ThemeItem("matrix_emerald", "Matrix Emerald Terminal", "Gamer High-Contrast", "#00FF66", R.style.Theme_PSDPlayer_MatrixEmerald),
        ThemeItem("neon_cyberpunk", "Neon Cyberpunk", "Gamer High-Contrast", "#00E5FF", R.style.Theme_PSDPlayer_OledBlack),
        ThemeItem("toxic_lime", "Toxic Lime Hazard", "Gamer High-Contrast", "#39FF14", R.style.Theme_PSDPlayer_MatrixEmerald),
        ThemeItem("viper_crimson", "Viper Crimson", "Gamer High-Contrast", "#FF0033", R.style.Theme_PSDPlayer_Synthwave84)
    )

    fun getThemeById(id: String): ThemeItem {
        return themes.find { it.id == id } ?: themes[0]
    }
}
