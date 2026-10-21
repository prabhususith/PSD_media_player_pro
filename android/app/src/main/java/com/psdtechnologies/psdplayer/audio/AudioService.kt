package com.psdtechnologies.psdplayer.audio

import android.app.Notification
import android.app.PendingIntent
import android.app.Service
import android.content.Context
import android.content.Intent
import android.os.Binder
import android.os.IBinder
import android.support.v4.media.session.MediaSessionCompat
import android.support.v4.media.session.PlaybackStateCompat
import androidx.core.app.NotificationCompat
import com.psdtechnologies.psdplayer.PSDPlayerApplication
import com.psdtechnologies.psdplayer.R
import com.psdtechnologies.psdplayer.ui.PlayerActivity

/**
 * Foreground Audio Playback Service.
 * Manages background playback, lockscreen controls, and media notifications.
 * Author: Prabhu Kannan (PSD Technologies)
 */
class AudioService : Service() {

    private val binder = LocalBinder()
    private var mediaSession: MediaSessionCompat? = null

    companion object {
        const val NOTIFICATION_ID = 2501
        const val ACTION_PLAY = "com.psdtechnologies.psdplayer.ACTION_PLAY"
        const val ACTION_PAUSE = "com.psdtechnologies.psdplayer.ACTION_PAUSE"
        const val ACTION_STOP = "com.psdtechnologies.psdplayer.ACTION_STOP"
        const val ACTION_FAST_FORWARD = "com.psdtechnologies.psdplayer.ACTION_FORWARD"
        const val ACTION_REWIND = "com.psdtechnologies.psdplayer.ACTION_REWIND"

        fun start(context: Context) {
            val intent = Intent(context, AudioService::class.java)
            context.startService(intent)
        }

        fun stop(context: Context) {
            val intent = Intent(context, AudioService::class.java)
            context.stopService(intent)
        }
    }

    inner class LocalBinder : Binder() {
        fun getService(): AudioService = this@AudioService
    }

    override fun onBind(intent: Intent?): IBinder = binder

    override fun onCreate() {
        super.onCreate()
        setupMediaSession()
        startForeground(NOTIFICATION_ID, buildNotification("PSD Media Player", isPlaying = true))
    }

    private fun setupMediaSession() {
        mediaSession = MediaSessionCompat(this, "PSDAudioServiceSession").apply {
            setFlags(
                MediaSessionCompat.FLAG_HANDLES_MEDIA_BUTTONS or
                        MediaSessionCompat.FLAG_HANDLES_TRANSPORT_CONTROLS
            )
            setPlaybackState(
                PlaybackStateCompat.Builder()
                    .setActions(
                        PlaybackStateCompat.ACTION_PLAY or
                                PlaybackStateCompat.ACTION_PAUSE or
                                PlaybackStateCompat.ACTION_SKIP_TO_NEXT or
                                PlaybackStateCompat.ACTION_SKIP_TO_PREVIOUS or
                                PlaybackStateCompat.ACTION_STOP
                    )
                    .setState(PlaybackStateCompat.STATE_PLAYING, 0, 1.0f)
                    .build()
            )
            isActive = true
        }
    }

    fun buildNotification(title: String, isPlaying: Boolean): Notification {
        val openIntent = Intent(this, PlayerActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_SINGLE_TOP
        }
        val pendingOpen = PendingIntent.getActivity(
            this, 0, openIntent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
        )

        val playPauseIcon = if (isPlaying) R.drawable.ic_pause else R.drawable.ic_play
        val playPauseAction = if (isPlaying) ACTION_PAUSE else ACTION_PLAY

        val playPauseIntent = Intent(this, AudioService::class.java).apply { action = playPauseAction }
        val pendingPlayPause = PendingIntent.getService(
            this, 1, playPauseIntent, PendingIntent.FLAG_IMMUTABLE
        )

        val rewindIntent = Intent(this, AudioService::class.java).apply { action = ACTION_REWIND }
        val pendingRewind = PendingIntent.getService(
            this, 2, rewindIntent, PendingIntent.FLAG_IMMUTABLE
        )

        val forwardIntent = Intent(this, AudioService::class.java).apply { action = ACTION_FAST_FORWARD }
        val pendingForward = PendingIntent.getService(
            this, 3, forwardIntent, PendingIntent.FLAG_IMMUTABLE
        )

        return NotificationCompat.Builder(this, PSDPlayerApplication.PLAYBACK_CHANNEL_ID)
            .setContentTitle(title)
            .setContentText("PSD Media Player Pro — Background Playback Active")
            .setSmallIcon(R.drawable.ic_play)
            .setContentIntent(pendingOpen)
            .setVisibility(NotificationCompat.VISIBILITY_PUBLIC)
            .setOngoing(isPlaying)
            .addAction(R.drawable.ic_replay_10, "-10s", pendingRewind)
            .addAction(playPauseIcon, if (isPlaying) "Pause" else "Play", pendingPlayPause)
            .addAction(R.drawable.ic_forward_10, "+10s", pendingForward)
            .setStyle(
                androidx.media.app.NotificationCompat.MediaStyle()
                    .setMediaSession(mediaSession?.sessionToken)
                    .setShowActionsInCompactView(0, 1, 2)
            )
            .build()
    }

    override fun onDestroy() {
        mediaSession?.release()
        super.onDestroy()
    }
}
