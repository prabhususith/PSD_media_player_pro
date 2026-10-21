package com.psdtechnologies.psdplayer.core

import android.media.MediaCodecInfo
import android.media.MediaCodecList
import android.os.Build

/**
 * Manages MediaCodec hardware acceleration detection and capabilities
 * optimized for Qualcomm Snapdragon 625 (Adreno 506) and modern SoCs.
 * Author: Prabhu Kannan (PSD Technologies)
 */
object HardwareCodecManager {

    data class CodecCapability(
        val codecName: String,
        val mimeType: String,
        val isHardwareAccelerated: Boolean,
        val maxResolution: String
    )

    fun getHardwareCapabilities(): List<CodecCapability> {
        val list = mutableListOf<CodecCapability>()
        val codecInfos = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
            MediaCodecList(MediaCodecList.REGULAR_CODECS).codecInfos
        } else {
            @Suppress("DEPRECATION")
            (0 until MediaCodecList.getCodecCount()).map { MediaCodecList.getCodecInfoAt(it) }.toTypedArray()
        }

        for (info in codecInfos) {
            if (info.isEncoder) continue

            val supportedTypes = info.supportedTypes
            for (type in supportedTypes) {
                if (type.startsWith("video/")) {
                    val isHw = isHardwareCodec(info)
                    val caps = try {
                        info.getCapabilitiesForType(type)
                    } catch (e: Exception) {
                        null
                    }
                    val videoCaps = caps?.videoCapabilities
                    val maxRes = if (videoCaps != null) {
                        "${videoCaps.supportedWidths.upper}x${videoCaps.supportedHeights.upper}"
                    } else {
                        "Auto"
                    }

                    list.add(
                        CodecCapability(
                            codecName = info.name,
                            mimeType = type,
                            isHardwareAccelerated = isHw,
                            maxResolution = maxRes
                        )
                    )
                }
            }
        }
        return list
    }

    fun isHardwareCodec(info: MediaCodecInfo): Boolean {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
            return info.isHardwareAccelerated
        }
        val name = info.name.lowercase()
        return !name.startsWith("omx.google.") &&
                !name.startsWith("c2.android.") &&
                !name.contains("sw") &&
                !name.contains("software") &&
                (name.startsWith("omx.qcom.") ||
                        name.startsWith("omx.exynos.") ||
                        name.startsWith("omx.mtk.") ||
                        name.startsWith("omx.intel.") ||
                        name.startsWith("c2.qti."))
    }

    fun getDeviceCodecSummary(): String {
        val sb = StringBuilder()
        sb.append("Device: ${Build.MANUFACTURER} ${Build.MODEL} (${Build.HARDWARE})\n")
        sb.append("OS: Android ${Build.VERSION.RELEASE} (API ${Build.VERSION.SDK_INT})\n")
        val caps = getHardwareCapabilities()
        val hwVideo = caps.filter { it.isHardwareAccelerated }
        sb.append("HW Decoders Detected: ${hwVideo.size}\n")
        hwVideo.take(4).forEach {
            sb.append(" - ${it.codecName} [${it.mimeType.substringAfterLast("/")}]\n")
        }
        return sb.toString()
    }
}
