package com.psdtechnologies.psdplayer.core

import android.content.Context
import android.net.Uri
import androidx.media3.common.C
import androidx.media3.common.MediaItem
import androidx.media3.common.PlaybackParameters
import androidx.media3.common.Player
import androidx.media3.common.VideoSize
import androidx.media3.common.util.UnstableApi
import androidx.media3.datasource.DefaultDataSource
import androidx.media3.datasource.okhttp.OkHttpDataSource
import androidx.media3.exoplayer.DefaultLoadControl
import androidx.media3.exoplayer.ExoPlayer
import androidx.media3.exoplayer.source.DefaultMediaSourceFactory
import androidx.media3.ui.AspectRatioFrameLayout
import androidx.media3.ui.PlayerView
import okhttp3.OkHttpClient
import java.util.concurrent.TimeUnit

/**
 * High-Performance Media Playback Engine Controller.
 * Manages zero-copy SurfaceView presentation, MediaCodec decoders, and buffer streaming.
 * Author: Prabhu Kannan (PSD Technologies)
 */
@UnstableApi
class PlayerEngineController(private val context: Context) {

    private var exoPlayer: ExoPlayer? = null
    var onVideoSizeChanged: ((width: Int, height: Int) -> Unit)? = null
    var onPlaybackStateChanged: ((isPlaying: Boolean, isBuffering: Boolean) -> Unit)? = null
    var onPositionDiscontinuity: ((positionMs: Long, durationMs: Long) -> Unit)? = null

    // A-B Repeat Loop Range
    var loopPointA: Long = -1L
    var loopPointB: Long = -1L
    var isAbRepeatActive: Boolean = false

    // Aspect Ratio Mode
    enum class AspectRatioMode {
        FIT, CROP, STRETCH, RATIO_16_9, RATIO_4_3, RATIO_21_9
    }
    var currentAspectRatioMode: AspectRatioMode = AspectRatioMode.FIT

    init {
        initializePlayer()
    }

    private fun initializePlayer() {
        // High-precision low-memory LoadControl: 3000ms network caching, 2000ms live caching
        val loadControl = DefaultLoadControl.Builder()
            .setBufferDurationsMs(
                /* minBufferMs = */ 3000,
                /* maxBufferMs = */ 15000,
                /* bufferForPlaybackMs = */ 1500,
                /* bufferForPlaybackAfterRebufferMs = */ 2000
            )
            .setPrioritizeTimeOverSizeThresholds(true)
            .build()

        val httpDataSourceFactory = OkHttpDataSource.Factory(NetworkStreamManager.sharedHttpClient)
            .setUserAgent(NetworkStreamManager.USER_AGENT)

        val dataSourceFactory = DefaultDataSource.Factory(context, httpDataSourceFactory)
        val mediaSourceFactory = DefaultMediaSourceFactory(dataSourceFactory)

        exoPlayer = ExoPlayer.Builder(context)
            .setMediaSourceFactory(mediaSourceFactory)
            .setLoadControl(loadControl)
            .setSeekBackIncrementMs(10000)
            .setSeekForwardIncrementMs(10000)
            .build().apply {
                playWhenReady = true
                addListener(object : Player.Listener {
                    override fun onVideoSizeChanged(videoSize: VideoSize) {
                        onVideoSizeChanged?.invoke(videoSize.width, videoSize.height)
                    }

                    override fun onPlaybackStateChanged(playbackState: Int) {
                        val isBuffering = playbackState == Player.STATE_BUFFERING
                        onPlaybackStateChanged?.invoke(isPlaying, isBuffering)
                    }

                    override fun onIsPlayingChanged(isPlaying: Boolean) {
                        onPlaybackStateChanged?.invoke(isPlaying, false)
                    }

                    override fun onPositionDiscontinuity(
                        oldPosition: Player.PositionInfo,
                        newPosition: Player.PositionInfo,
                        reason: Int
                    ) {
                        checkAbRepeat(newPosition.positionMs)
                    }
                })
            }
    }

    fun attachToView(playerView: PlayerView) {
        playerView.player = exoPlayer
        applyAspectRatio(playerView, currentAspectRatioMode)
    }

    fun playUri(uri: Uri, title: String = "") {
        val mediaItem = MediaItem.Builder()
            .setUri(uri)
            .setMediaId(title)
            .build()

        exoPlayer?.apply {
            setMediaItem(mediaItem)
            prepare()
            play()
        }
    }

    fun togglePlayPause(): Boolean {
        exoPlayer?.let {
            if (it.isPlaying) {
                it.pause()
            } else {
                it.play()
            }
            return it.isPlaying
        }
        return false
    }

    fun pause() {
        exoPlayer?.pause()
    }

    fun play() {
        exoPlayer?.play()
    }

    fun seekTo(positionMs: Long) {
        exoPlayer?.seekTo(positionMs.coerceAtLeast(0L))
    }

    fun seekForward(deltaMs: Long = 10000) {
        exoPlayer?.let {
            val target = (it.currentPosition + deltaMs).coerceAtMost(it.duration.coerceAtLeast(0L))
            it.seekTo(target)
        }
    }

    fun seekBackward(deltaMs: Long = 10000) {
        exoPlayer?.let {
            val target = (it.currentPosition - deltaMs).coerceAtLeast(0L)
            it.seekTo(target)
        }
    }

    fun setPlaybackSpeed(speed: Float) {
        // Pitch-preserved time stretching
        val clampedSpeed = speed.coerceIn(0.25f, 4.0f)
        exoPlayer?.playbackParameters = PlaybackParameters(clampedSpeed, 1.0f)
    }

    fun getPlaybackSpeed(): Float {
        return exoPlayer?.playbackParameters?.speed ?: 1.0f
    }

    fun cycleAspectRatio(playerView: PlayerView): AspectRatioMode {
        val modes = AspectRatioMode.values()
        val nextIndex = (currentAspectRatioMode.ordinal + 1) % modes.size
        currentAspectRatioMode = modes[nextIndex]
        applyAspectRatio(playerView, currentAspectRatioMode)
        return currentAspectRatioMode
    }

    fun applyAspectRatio(playerView: PlayerView, mode: AspectRatioMode) {
        currentAspectRatioMode = mode
        when (mode) {
            AspectRatioMode.FIT -> playerView.resizeMode = AspectRatioFrameLayout.RESIZE_MODE_FIT
            AspectRatioMode.CROP -> playerView.resizeMode = AspectRatioFrameLayout.RESIZE_MODE_ZOOM
            AspectRatioMode.STRETCH -> playerView.resizeMode = AspectRatioFrameLayout.RESIZE_MODE_FILL
            AspectRatioMode.RATIO_16_9 -> {
                playerView.resizeMode = AspectRatioFrameLayout.RESIZE_MODE_FIXED_WIDTH
            }
            AspectRatioMode.RATIO_4_3 -> {
                playerView.resizeMode = AspectRatioFrameLayout.RESIZE_MODE_FIXED_HEIGHT
            }
            AspectRatioMode.RATIO_21_9 -> {
                playerView.resizeMode = AspectRatioFrameLayout.RESIZE_MODE_ZOOM
            }
        }
    }

    fun setPointA() {
        loopPointA = exoPlayer?.currentPosition ?: -1L
        if (loopPointB != -1L && loopPointB > loopPointA) {
            isAbRepeatActive = true
        }
    }

    fun setPointB() {
        val current = exoPlayer?.currentPosition ?: -1L
        if (loopPointA != -1L && current > loopPointA) {
            loopPointB = current
            isAbRepeatActive = true
        }
    }

    fun clearAbRepeat() {
        loopPointA = -1L
        loopPointB = -1L
        isAbRepeatActive = false
    }

    fun checkAbRepeat(currentMs: Long) {
        if (isAbRepeatActive && loopPointA >= 0 && loopPointB > loopPointA) {
            if (currentMs >= loopPointB || currentMs < loopPointA) {
                exoPlayer?.seekTo(loopPointA)
            }
        }
    }

    fun getCurrentPosition(): Long = exoPlayer?.currentPosition ?: 0L
    fun getDuration(): Long = (exoPlayer?.duration ?: 0L).coerceAtLeast(0L)
    fun isPlaying(): Boolean = exoPlayer?.isPlaying ?: false
    fun getAudioSessionId(): Int = exoPlayer?.audioSessionId ?: C.AUDIO_SESSION_ID_UNSET

    fun getPlayer(): ExoPlayer? = exoPlayer

    fun release() {
        exoPlayer?.release()
        exoPlayer = null
    }
}
