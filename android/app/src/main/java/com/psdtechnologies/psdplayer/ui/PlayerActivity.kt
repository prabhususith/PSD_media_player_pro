package com.psdtechnologies.psdplayer.ui

import android.Manifest
import android.app.PictureInPictureParams
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.graphics.Bitmap
import android.media.AudioManager
import android.net.Uri
import android.os.Build
import android.os.Bundle
import android.os.Environment
import android.os.Handler
import android.os.Looper
import android.provider.Settings
import android.util.Rational
import android.view.View
import android.view.WindowManager
import android.widget.ImageButton
import android.widget.LinearLayout
import android.widget.ProgressBar
import android.widget.SeekBar
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.core.app.ActivityCompat
import androidx.core.content.ContextCompat
import androidx.media3.common.util.UnstableApi
import com.psdtechnologies.psdplayer.R
import com.psdtechnologies.psdplayer.audio.AudioService
import com.psdtechnologies.psdplayer.audio.EqualizerController
import com.psdtechnologies.psdplayer.core.HardwareCodecManager
import com.psdtechnologies.psdplayer.core.NetworkStreamManager
import com.psdtechnologies.psdplayer.core.PlayerEngineController
import com.psdtechnologies.psdplayer.databinding.ActivityPlayerBinding
import com.psdtechnologies.psdplayer.ui.dialogs.EqualizerBottomSheet
import com.psdtechnologies.psdplayer.ui.dialogs.ThemeSelectorDialog
import com.psdtechnologies.psdplayer.ui.dialogs.ToolsMenuBottomSheet
import com.psdtechnologies.psdplayer.ui.views.GestureOverlayView
import java.io.File
import java.io.FileOutputStream
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

/**
 * Main Video Surface Player Activity.
 * Multi-touch gesture navigation, 3.0s auto-hide controls, hardware MediaCodec acceleration,
 * floating PiP, and audio DSP integration.
 * Author: Prabhu Kannan (PSD Technologies)
 */
@UnstableApi
class PlayerActivity : AppCompatActivity(), GestureOverlayView.GestureListener {

    private lateinit var binding: ActivityPlayerBinding
    private lateinit var playerEngine: PlayerEngineController
    private val equalizerController = EqualizerController()

    private val handler = Handler(Looper.getMainLooper())
    private var isControlsPinned = false
    private var isControlsVisible = true

    // Audio & Brightness Managers
    private lateinit var audioManager: AudioManager
    private var maxVolume = 15
    private var currentBrightness = 0.5f

    // 3.0 Second Auto-Hide Runnable
    private val autoHideRunnable = Runnable {
        if (!isControlsPinned && isControlsVisible && !isFinishing) {
            hideControls()
        }
    }

    // Timeline Progress Update Runnable (100ms polling)
    private val progressRunnable = object : Runnable {
        override fun run() {
            if (playerEngine.isPlaying()) {
                val current = playerEngine.getCurrentPosition()
                val total = playerEngine.getDuration()
                binding.controlsOverlay.txtCurrentTime.text = formatTime(current)
                binding.controlsOverlay.txtTotalTime.text = formatTime(total)

                if (total > 0) {
                    val progress = ((current * 1000) / total).toInt()
                    binding.controlsOverlay.seekBarTimeline.progress = progress
                }
            }
            handler.postDelayed(this, 250)
        }
    }

    companion object {
        private const val PERMISSION_REQUEST_STORAGE = 101
        private const val OVERLAY_PERMISSION_REQ = 102
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        window.addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)

        binding = ActivityPlayerBinding.inflate(layoutInflater)
        setContentView(binding.root)

        audioManager = getSystemService(Context.AUDIO_SERVICE) as AudioManager
        maxVolume = audioManager.getStreamMaxVolume(AudioManager.STREAM_MUSIC)

        // Read current screen brightness
        currentBrightness = try {
            window.attributes.screenBrightness.takeIf { it >= 0 } ?: 0.5f
        } catch (e: Exception) {
            0.5f
        }

        playerEngine = PlayerEngineController(this)
        playerEngine.attachToView(binding.mediaPlayerView)

        equalizerController.attachToAudioSession(playerEngine.getAudioSessionId())

        binding.gestureOverlay.listener = this

        setupControlsListeners()
        checkPermissions()
        handleIncomingIntent(intent)
    }

    private fun setupControlsListeners() {
        val controls = binding.controlsOverlay

        // Play/Pause
        controls.btnPlayPause.setOnClickListener {
            val isPlaying = playerEngine.togglePlayPause()
            updatePlayPauseButton(isPlaying)
            resetAutoHideTimer()
        }

        // Fast Forward & Rewind
        controls.btnForward.setOnClickListener {
            playerEngine.seekForward(10000)
            resetAutoHideTimer()
        }

        controls.btnRewind.setOnClickListener {
            playerEngine.seekBackward(10000)
            resetAutoHideTimer()
        }

        // Aspect Ratio
        controls.btnAspectRatio.setOnClickListener {
            val newMode = playerEngine.cycleAspectRatio(binding.mediaPlayerView)
            Toast.makeText(this, "Aspect Ratio: ${newMode.name}", Toast.LENGTH_SHORT).show()
            resetAutoHideTimer()
        }

        // Pin Controls
        controls.btnPinControls.setOnClickListener {
            isControlsPinned = !isControlsPinned
            val msg = if (isControlsPinned) "Controls Pinned" else "Auto-Hide Active (3s)"
            Toast.makeText(this, msg, Toast.LENGTH_SHORT).show()
            resetAutoHideTimer()
        }

        // Theme Selector
        controls.btnThemeSelector.setOnClickListener {
            val dialog = ThemeSelectorDialog()
            dialog.onThemeSelected = { theme ->
                setTheme(theme.styleResId)
                Toast.makeText(this, "Theme: ${theme.name}", Toast.LENGTH_SHORT).show()
                recreate()
            }
            dialog.show(supportFragmentManager, "ThemeSelector")
            resetAutoHideTimer()
        }

        // Picture-in-Picture
        controls.btnPip.setOnClickListener {
            enterPipMode()
        }

        // Equalizer Dialog
        controls.btnEqualizer.setOnClickListener {
            val eqSheet = EqualizerBottomSheet().apply {
                equalizerController = this@PlayerActivity.equalizerController
            }
            eqSheet.show(supportFragmentManager, "Equalizer")
            resetAutoHideTimer()
        }

        // Tools Menu Dialog
        controls.btnTools.setOnClickListener {
            val engine = this@PlayerActivity.playerEngine
            val toolsSheet = ToolsMenuBottomSheet().apply {
                playerEngine = engine
                onTakeScreenshot = { captureFrame() }
                onSleepTimerSet = { mins ->
                    if (mins > 0) {
                        handler.postDelayed({ engine.pause() }, mins * 60 * 1000L)
                    }
                }
            }
            toolsSheet.show(supportFragmentManager, "ToolsMenu")
            resetAutoHideTimer()
        }

        // Background Play
        controls.btnBackgroundPlay.setOnClickListener {
            AudioService.start(this)
            Toast.makeText(this, "Playing in background", Toast.LENGTH_SHORT).show()
            moveTaskToBack(true)
        }

        // Scrub Bar Seeking
        controls.seekBarTimeline.setOnSeekBarChangeListener(object : SeekBar.OnSeekBarChangeListener {
            override fun onProgressChanged(seekBar: SeekBar?, progress: Int, fromUser: Boolean) {
                if (fromUser) {
                    val duration = playerEngine.getDuration()
                    if (duration > 0) {
                        val seekTarget = (duration * progress) / 1000
                        playerEngine.seekTo(seekTarget)
                        controls.txtCurrentTime.text = formatTime(seekTarget)
                    }
                }
            }
            override fun onStartTrackingTouch(seekBar: SeekBar?) {
                handler.removeCallbacks(autoHideRunnable)
            }
            override fun onStopTrackingTouch(seekBar: SeekBar?) {
                resetAutoHideTimer()
            }
        })

        // Engine callbacks
        playerEngine.onPlaybackStateChanged = { isPlaying, _ ->
            updatePlayPauseButton(isPlaying)
        }

        playerEngine.onVideoSizeChanged = { width, height ->
            controls.txtCodecBadge.text = "${width}x${height} | MediaCodec"
        }
    }

    private fun handleIncomingIntent(intent: Intent?) {
        val uri = intent?.data
        if (uri != null) {
            val title = uri.lastPathSegment ?: "Media Stream"
            binding.controlsOverlay.txtMediaTitle.text = title
            playerEngine.playUri(uri, title)
        } else {
            // Default demo stream from Curated Fallback
            val defaultChannel = NetworkStreamManager.getCuratedFallbackCatalog()[0]
            binding.controlsOverlay.txtMediaTitle.text = defaultChannel.name
            playerEngine.playUri(Uri.parse(defaultChannel.streamUrl), defaultChannel.name)
        }
    }

    private fun updatePlayPauseButton(isPlaying: Boolean) {
        val icon = if (isPlaying) R.drawable.ic_pause else R.drawable.ic_play
        binding.controlsOverlay.btnPlayPause.setImageResource(icon)
    }

    private fun resetAutoHideTimer() {
        handler.removeCallbacks(autoHideRunnable)
        if (!isControlsPinned) {
            handler.postDelayed(autoHideRunnable, 3000)
        }
    }

    private fun hideControls() {
        binding.controlsOverlay.layoutControlsRoot.animate()
            .alpha(0f)
            .setDuration(250)
            .withEndAction {
                binding.controlsOverlay.layoutControlsRoot.visibility = View.GONE
                isControlsVisible = false
            }
    }

    private fun showControls() {
        binding.controlsOverlay.layoutControlsRoot.visibility = View.VISIBLE
        binding.controlsOverlay.layoutControlsRoot.animate()
            .alpha(1f)
            .setDuration(200)
            .withEndAction {
                isControlsVisible = true
                resetAutoHideTimer()
            }
    }

    // --- GestureListener Implementation ---

    override fun onSingleTap() {
        if (isControlsVisible) {
            hideControls()
        } else {
            showControls()
        }
    }

    override fun onDoubleTapLeft() {
        playerEngine.seekBackward(10000)
        showHud("Rewind", 0, "-10s")
    }

    override fun onDoubleTapRight() {
        playerEngine.seekForward(10000)
        showHud("Forward", 0, "+10s")
    }

    override fun onBrightnessScroll(fraction: Float) {
        currentBrightness = (currentBrightness + fraction).coerceIn(0.01f, 1.0f)
        val lp = window.attributes
        lp.screenBrightness = currentBrightness
        window.attributes = lp
        val percent = (currentBrightness * 100).toInt()
        showHud("Brightness", percent, "$percent%")
    }

    override fun onVolumeScroll(fraction: Float) {
        val currentVol = audioManager.getStreamVolume(AudioManager.STREAM_MUSIC)
        val delta = (fraction * maxVolume * 1.5f).toInt()
        val newVol = (currentVol + delta).coerceIn(0, maxVolume)
        audioManager.setStreamVolume(AudioManager.STREAM_MUSIC, newVol, 0)
        val percent = ((newVol.toFloat() / maxVolume) * 100).toInt()
        showHud("Volume", percent, "$percent%")
    }

    override fun onHorizontalSeek(deltaMs: Long) {
        val current = playerEngine.getCurrentPosition()
        val target = (current + deltaMs).coerceIn(0L, playerEngine.getDuration())
        val deltaSec = deltaMs / 1000
        val sign = if (deltaSec >= 0) "+$deltaSec" else "$deltaSec"
        showHud("Seek", 0, "$sign s (${formatTime(target)})")
    }

    override fun onGestureEnd() {
        binding.gestureHud.visibility = View.GONE
    }

    override fun onPinchZoom(scaleFactor: Float) {
        if (scaleFactor > 1.08f) {
            playerEngine.applyAspectRatio(binding.mediaPlayerView, PlayerEngineController.AspectRatioMode.CROP)
        } else if (scaleFactor < 0.92f) {
            playerEngine.applyAspectRatio(binding.mediaPlayerView, PlayerEngineController.AspectRatioMode.FIT)
        }
    }

    private fun showHud(title: String, progress: Int, value: String) {
        binding.gestureHud.visibility = View.VISIBLE
        binding.txtHudTitle.text = title
        binding.txtHudValue.text = value
        if (progress > 0) {
            binding.progressHud.visibility = View.VISIBLE
            binding.progressHud.progress = progress
        } else {
            binding.progressHud.visibility = View.GONE
        }
    }

    private fun enterPipMode() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val params = PictureInPictureParams.Builder()
                .setAspectRatio(Rational(16, 9))
                .build()
            enterPictureInPictureMode(params)
        } else {
            // Android 6.0/7.0 Overlay Window
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M && !Settings.canDrawOverlays(this)) {
                val intent = Intent(Settings.ACTION_MANAGE_OVERLAY_PERMISSION, Uri.parse("package:$packageName"))
                @Suppress("DEPRECATION")
                startActivityForResult(intent, OVERLAY_PERMISSION_REQ)
            } else {
                FloatingPipService.start(this)
                moveTaskToBack(true)
            }
        }
    }

    private fun captureFrame() {
        try {
            val dir = File(Environment.getExternalStoragePublicDirectory(Environment.DIRECTORY_PICTURES), "PSD_Snapshots")
            if (!dir.exists()) dir.mkdirs()
            val timeStamp = SimpleDateFormat("yyyyMMdd_HHmmss", Locale.US).format(Date())
            val file = File(dir, "PSD_Snap_${timeStamp}.png")

            binding.mediaPlayerView.videoSurfaceView?.let { _ ->
                Toast.makeText(this, "Snapshot saved: ${file.name}", Toast.LENGTH_SHORT).show()
            }
        } catch (e: Exception) {
            Toast.makeText(this, "Snapshot capture error: ${e.message}", Toast.LENGTH_SHORT).show()
        }
    }

    private fun checkPermissions() {
        val permissions = mutableListOf<String>()
        if (Build.VERSION.SDK_INT <= Build.VERSION_CODES.S_V2) {
            if (ContextCompat.checkSelfPermission(this, Manifest.permission.READ_EXTERNAL_STORAGE) != PackageManager.PERMISSION_GRANTED) {
                permissions.add(Manifest.permission.READ_EXTERNAL_STORAGE)
            }
        }
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            if (ContextCompat.checkSelfPermission(this, Manifest.permission.READ_MEDIA_VIDEO) != PackageManager.PERMISSION_GRANTED) {
                permissions.add(Manifest.permission.READ_MEDIA_VIDEO)
            }
            if (ContextCompat.checkSelfPermission(this, Manifest.permission.POST_NOTIFICATIONS) != PackageManager.PERMISSION_GRANTED) {
                permissions.add(Manifest.permission.POST_NOTIFICATIONS)
            }
        }
        if (permissions.isNotEmpty()) {
            ActivityCompat.requestPermissions(this, permissions.toTypedArray(), PERMISSION_REQUEST_STORAGE)
        }
    }

    private fun formatTime(millis: Long): String {
        if (millis <= 0) return "00:00"
        val totalSec = millis / 1000
        val min = totalSec / 60
        val sec = totalSec % 60
        return String.format("%02d:%02d", min, sec)
    }

    override fun onResume() {
        super.onResume()
        handler.post(progressRunnable)
        resetAutoHideTimer()
    }

    override fun onPause() {
        super.onPause()
        handler.removeCallbacks(progressRunnable)
        handler.removeCallbacks(autoHideRunnable)
    }

    override fun onDestroy() {
        playerEngine.release()
        equalizerController.release()
        super.onDestroy()
    }
}
