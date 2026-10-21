package com.psdtechnologies.psdplayer.ui.dialogs

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.Button
import android.widget.SeekBar
import android.widget.TextView
import android.widget.Toast
import com.google.android.material.bottomsheet.BottomSheetDialogFragment
import com.psdtechnologies.psdplayer.R
import com.psdtechnologies.psdplayer.core.HardwareCodecManager
import com.psdtechnologies.psdplayer.core.PlayerEngineController

/**
 * Bottom Sheet Modal for Tools, Playback Speed, A-B Repeat, and Stream Telemetry.
 * Author: Prabhu Kannan (PSD Technologies)
 */
class ToolsMenuBottomSheet : BottomSheetDialogFragment() {

    var playerEngine: PlayerEngineController? = null
    var onTakeScreenshot: (() -> Unit)? = null
    var onSleepTimerSet: ((minutes: Int) -> Unit)? = null

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View? {
        return inflater.inflate(R.layout.dialog_tools_menu, container, false)
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        val engine = playerEngine ?: return

        val txtSpeed = view.findViewById<TextView>(R.id.txtSpeedLabel)
        val seekSpeed = view.findViewById<SeekBar>(R.id.seekBarSpeed)

        val btnPointA = view.findViewById<Button>(R.id.btnSetPointA)
        val btnPointB = view.findViewById<Button>(R.id.btnSetPointB)
        val btnClearAB = view.findViewById<Button>(R.id.btnClearAB)

        val txtAudioSync = view.findViewById<TextView>(R.id.txtAudioSyncLabel)
        val seekAudioSync = view.findViewById<SeekBar>(R.id.seekBarAudioSync)

        val btnScreenshot = view.findViewById<Button>(R.id.btnTakeScreenshot)
        val btnSleepTimer = view.findViewById<Button>(R.id.btnSleepTimer)
        val txtTelemetry = view.findViewById<TextView>(R.id.txtTelemetryInfo)

        // Setup Speed Slider: Progress 0 to 375 maps to 0.25x to 4.0x
        val currentSpeed = engine.getPlaybackSpeed()
        seekSpeed.progress = ((currentSpeed - 0.25f) * 100).toInt()
        txtSpeed.text = "Speed: ${String.format("%.2f", currentSpeed)}x"

        seekSpeed.setOnSeekBarChangeListener(object : SeekBar.OnSeekBarChangeListener {
            override fun onProgressChanged(seekBar: SeekBar?, progress: Int, fromUser: Boolean) {
                if (fromUser) {
                    val speed = 0.25f + (progress / 100f)
                    engine.setPlaybackSpeed(speed)
                    txtSpeed.text = "Speed: ${String.format("%.2f", speed)}x"
                }
            }
            override fun onStartTrackingTouch(seekBar: SeekBar?) {}
            override fun onStopTrackingTouch(seekBar: SeekBar?) {}
        })

        // Setup A-B Repeat Loop
        btnPointA.setOnClickListener {
            engine.setPointA()
            Toast.makeText(context, "Loop Point A set at: ${formatTime(engine.loopPointA)}", Toast.LENGTH_SHORT).show()
        }

        btnPointB.setOnClickListener {
            engine.setPointB()
            Toast.makeText(context, "Loop Point B set at: ${formatTime(engine.loopPointB)} (Loop Active)", Toast.LENGTH_SHORT).show()
        }

        btnClearAB.setOnClickListener {
            engine.clearAbRepeat()
            Toast.makeText(context, "A-B Repeat Cleared", Toast.LENGTH_SHORT).show()
        }

        // Setup Audio Sync Slider: 0 to 10000 maps to -5000ms to +5000ms
        seekAudioSync.setOnSeekBarChangeListener(object : SeekBar.OnSeekBarChangeListener {
            override fun onProgressChanged(seekBar: SeekBar?, progress: Int, fromUser: Boolean) {
                val offsetMs = progress - 5000
                txtAudioSync.text = "Audio Sync: ${offsetMs} ms"
            }
            override fun onStartTrackingTouch(seekBar: SeekBar?) {}
            override fun onStopTrackingTouch(seekBar: SeekBar?) {}
        })

        // Screenshot
        btnScreenshot.setOnClickListener {
            onTakeScreenshot?.invoke()
            dismiss()
        }

        // Sleep Timer
        var sleepIndex = 0
        val sleepMinutes = listOf(15, 30, 45, 60, 0)
        btnSleepTimer.setOnClickListener {
            val mins = sleepMinutes[sleepIndex % sleepMinutes.size]
            sleepIndex++
            if (mins > 0) {
                btnSleepTimer.text = "Sleep: ${mins}m"
                onSleepTimerSet?.invoke(mins)
                Toast.makeText(context, "Playback will halt in $mins minutes", Toast.LENGTH_SHORT).show()
            } else {
                btnSleepTimer.text = "Sleep Timer"
                onSleepTimerSet?.invoke(0)
                Toast.makeText(context, "Sleep Timer canceled", Toast.LENGTH_SHORT).show()
            }
        }

        // Display Telemetry
        val summary = HardwareCodecManager.getDeviceCodecSummary()
        txtTelemetry.text = summary
    }

    private fun formatTime(millis: Long): String {
        if (millis <= 0) return "00:00"
        val totalSec = millis / 1000
        val min = totalSec / 60
        val sec = totalSec % 60
        return String.format("%02d:%02d", min, sec)
    }
}
