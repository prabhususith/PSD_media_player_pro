package com.psdtechnologies.psdplayer

import android.app.Application
import android.app.NotificationChannel
import android.app.NotificationManager
import android.content.Context
import android.os.Build
import androidx.multidex.MultiDex

/**
 * Application Entry Point for PSD Media Player Pro (Android Edition).
 * Author: Prabhu Kannan (PSD Technologies)
 */
class PSDPlayerApplication : Application() {

    companion object {
        const val PLAYBACK_CHANNEL_ID = "psd_playback_channel"
        lateinit var instance: PSDPlayerApplication
            private set
    }

    override fun attachBaseContext(base: Context?) {
        super.attachBaseContext(base)
        MultiDex.install(this)
    }

    override fun onCreate() {
        super.onCreate()
        instance = this
        createNotificationChannel()
    }

    private fun createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val name = getString(R.string.notification_channel_name)
            val descriptionText = getString(R.string.notification_channel_desc)
            val importance = NotificationManager.IMPORTANCE_LOW
            val channel = NotificationChannel(PLAYBACK_CHANNEL_ID, name, importance).apply {
                description = descriptionText
                setShowBadge(false)
            }
            val notificationManager = getSystemService(NotificationManager::class.java)
            notificationManager?.createNotificationChannel(channel)
        }
    }
}
