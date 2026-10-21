package com.psdtechnologies.psdplayer.core

import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import okhttp3.OkHttpClient
import okhttp3.Request
import java.io.BufferedReader
import java.io.StringReader
import java.util.concurrent.TimeUnit

/**
 * Handles IPTV M3U / M3U8 catalog parsing and network streaming connections.
 * Author: Prabhu Kannan (PSD Technologies)
 */
object NetworkStreamManager {

    data class LiveChannel(
        val name: String,
        val streamUrl: String,
        val logoUrl: String? = null,
        val category: String = "General",
        val resolution: String = "1080p"
    )

    private val httpClient = OkHttpClient.Builder()
        .connectTimeout(15, TimeUnit.SECONDS)
        .readTimeout(20, TimeUnit.SECONDS)
        .retryOnConnectionFailure(true)
        .build()

    const val USER_AGENT = "PSD-Media-Player-Android/2.5.0 (Linux; Android; Mobile; Redmi Note 4)"

    suspend fun fetchPlaylist(m3uUrl: String): List<LiveChannel> = withContext(Dispatchers.IO) {
        val request = Request.Builder()
            .url(m3uUrl)
            .header("User-Agent", USER_AGENT)
            .build()

        try {
            val response = httpClient.newCall(request).execute()
            if (!response.isSuccessful) return@withContext getCuratedFallbackCatalog()
            val content = response.body?.string() ?: return@withContext getCuratedFallbackCatalog()
            parseM3u(content)
        } catch (e: Exception) {
            getCuratedFallbackCatalog()
        }
    }

    fun parseM3u(m3uContent: String): List<LiveChannel> {
        val channels = mutableListOf<LiveChannel>()
        val reader = BufferedReader(StringReader(m3uContent))
        var line: String?
        var currentName = "Live Stream"
        var currentLogo: String? = null
        var currentGroup = "General"

        while (reader.readLine().also { line = it } != null) {
            val trimmed = line!!.trim()
            if (trimmed.startsWith("#EXTINF:")) {
                // Parse channel name
                val commaIndex = trimmed.lastIndexOf(',')
                if (commaIndex != -1 && commaIndex < trimmed.length - 1) {
                    currentName = trimmed.substring(commaIndex + 1).trim()
                }
                // Parse group-title
                val groupMatch = Regex("group-title=\"([^\"]+)\"").find(trimmed)
                if (groupMatch != null) {
                    currentGroup = groupMatch.groupValues[1]
                }
                // Parse tvg-logo
                val logoMatch = Regex("tvg-logo=\"([^\"]+)\"").find(trimmed)
                if (logoMatch != null) {
                    currentLogo = logoMatch.groupValues[1]
                }
            } else if (trimmed.startsWith("http://") || trimmed.startsWith("https://") || trimmed.startsWith("rtsp://")) {
                channels.add(
                    LiveChannel(
                        name = currentName,
                        streamUrl = trimmed,
                        logoUrl = currentLogo,
                        category = currentGroup
                    )
                )
                currentName = "Live Stream"
                currentLogo = null
                currentGroup = "General"
            }
        }
        return if (channels.isEmpty()) getCuratedFallbackCatalog() else channels
    }

    fun getCuratedFallbackCatalog(): List<LiveChannel> {
        return listOf(
            LiveChannel("Aaj Tak HD", "https://feeds.intoday.in/aajtak/api/livetv/master.m3u8", null, "Indian National", "1080p"),
            LiveChannel("India TV Live", "https://livetv.indiatvnews.com/c3pool1/indiatv/indiatv_live.m3u8", null, "Indian National", "1080p"),
            LiveChannel("NDTV 24x7", "https://ndtv24x7elemaster.akamaized.net/hls/live/2003678/ndtv24x7/master.m3u8", null, "Indian National", "1080p"),
            LiveChannel("Puthiya Thalaimurai", "https://ptlive.puthiyathalaimurai.com/hls/live/2042858/ptlive/master.m3u8", null, "Tamil Regional", "1080p"),
            LiveChannel("Polimer News", "https://polimernews.akamaized.net/hls/live/2034567/polimer/master.m3u8", null, "Tamil Regional", "720p"),
            LiveChannel("TV9 Telugu", "https://tv9telugu.akamaized.net/hls/live/2033441/tv9telugu/master.m3u8", null, "Telugu Regional", "1080p"),
            LiveChannel("Asianet News", "https://asianetnews.akamaized.net/hls/live/2033442/asianet/master.m3u8", null, "Malayalam Regional", "1080p"),
            LiveChannel("Public TV Kannada", "https://publictv.akamaized.net/hls/live/2033443/publictv/master.m3u8", null, "Kannada Regional", "720p"),
            LiveChannel("ABP Ananda", "https://abpananda.akamaized.net/hls/live/2033444/ananda/master.m3u8", null, "Bengali Regional", "1080p"),
            LiveChannel("Bloomberg Global", "https://liveprodupmx.akamaized.net/us/Channel-HD-AWS-virginia-1/master.m3u8", null, "World News", "1080p"),
            LiveChannel("Al Jazeera English", "https://live-hls-web-aje.getaj.net/AJE/03.m3u8", null, "World News", "1080p"),
            LiveChannel("DW English", "https://dwamdstream102.akamaized.net/hls/live/2015525/dwstream102/index.m3u8", null, "World News", "720p"),
            LiveChannel("Red Bull TV", "https://rbmn-live.akamaized.net/hls/live/590964/BoRB-AT/master.m3u8", null, "Sports & Action", "1080p")
        )
    }
}
