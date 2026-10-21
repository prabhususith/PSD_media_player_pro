package com.psdtechnologies.psdplayer.subtitles

import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import okhttp3.OkHttpClient
import okhttp3.Request
import java.io.File
import java.io.FileInputStream
import java.nio.ByteBuffer
import java.nio.ByteOrder
import java.nio.channels.FileChannel

/**
 * OpenSubtitles Hash Calculator & Subtitle Fetcher.
 * Author: Prabhu Kannan (PSD Technologies)
 */
object SubtitleDownloader {

    private val httpClient = OkHttpClient()

    /**
     * Calculates the 64-bit checksum used by OpenSubtitles protocol:
     * file size + 64k chunks from beginning and end of file.
     */
    fun computeOpenSubtitlesHash(file: File): String {
        val size = file.length()
        val chunkSize = 65536L // 64k
        if (size < chunkSize) return ""

        var headHash = 0L
        var tailHash = 0L

        FileInputStream(file).use { fis ->
            val channel = fis.channel
            val buffer = ByteBuffer.allocate(chunkSize.toInt()).order(ByteOrder.LITTLE_ENDIAN)

            // Read head 64k
            channel.read(buffer)
            buffer.flip()
            while (buffer.hasRemaining()) {
                headHash += buffer.long
            }

            // Read tail 64k
            buffer.clear()
            channel.position((size - chunkSize).coerceAtLeast(0))
            channel.read(buffer)
            buffer.flip()
            while (buffer.hasRemaining()) {
                tailHash += buffer.long
            }
        }

        val totalHash = size + headHash + tailHash
        return String.format("%016x", totalHash)
    }

    suspend fun downloadSubtitle(url: String, destFile: File): Boolean = withContext(Dispatchers.IO) {
        val request = Request.Builder().url(url).build()
        try {
            val response = httpClient.newCall(request).execute()
            if (response.isSuccessful) {
                response.body?.byteStream()?.use { input ->
                    destFile.outputStream().use { output ->
                        input.copyTo(output)
                    }
                }
                return@withContext true
            }
        } catch (e: Exception) { }
        return@withContext false
    }
}
