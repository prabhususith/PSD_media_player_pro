package com.psdtechnologies.psdplayer.ui.dialogs

import android.os.Bundle
import android.view.Gravity
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.AdapterView
import android.widget.ArrayAdapter
import android.widget.Button
import android.widget.LinearLayout
import android.widget.SeekBar
import android.widget.Spinner
import android.widget.Switch
import android.widget.TextView
import com.google.android.material.bottomsheet.BottomSheetDialogFragment
import com.psdtechnologies.psdplayer.R
import com.psdtechnologies.psdplayer.audio.EqualizerController

/**
 * Bottom Sheet Modal for 10/31-band Equalizer and Audio DSP adjustments.
 * Author: Prabhu Kannan (PSD Technologies)
 */
class EqualizerBottomSheet : BottomSheetDialogFragment() {

    var equalizerController: EqualizerController? = null

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View? {
        return inflater.inflate(R.layout.dialog_equalizer, container, false)
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        val eq = equalizerController ?: return

        val switchEnable = view.findViewById<Switch>(R.id.switchEqualizerEnable)
        val spinnerPresets = view.findViewById<Spinner>(R.id.spinnerPresets)
        val layoutBands = view.findViewById<LinearLayout>(R.id.layoutEqBands)
        val seekBass = view.findViewById<SeekBar>(R.id.seekBarBassBoost)
        val txtBass = view.findViewById<TextView>(R.id.txtBassBoostLabel)
        val seekVirt = view.findViewById<SeekBar>(R.id.seekBarVirtualizer)
        val txtVirt = view.findViewById<TextView>(R.id.txtVirtualizerLabel)
        val btnReset = view.findViewById<Button>(R.id.btnResetEqualizer)

        switchEnable.isChecked = eq.isEnabled
        switchEnable.setOnCheckedChangeListener { _, isChecked ->
            eq.isEnabled = isChecked
        }

        // Setup Presets Spinner
        val adapter = ArrayAdapter(requireContext(), android.R.layout.simple_spinner_dropdown_item, eq.presetNames)
        spinnerPresets.adapter = adapter
        spinnerPresets.onItemSelectedListener = object : AdapterView.OnItemSelectedListener {
            override fun onItemSelected(parent: AdapterView<*>?, v: View?, position: Int, id: Long) {
                eq.applyPreset(position)
                refreshBandSliders(layoutBands, eq)
                seekBass.progress = eq.getBassBoostStrength().toInt()
                seekVirt.progress = eq.getVirtualizerStrength().toInt()
            }
            override fun onNothingSelected(parent: AdapterView<*>?) {}
        }

        // Setup Bass Boost and Virtualizer
        seekBass.progress = eq.getBassBoostStrength().toInt()
        seekBass.setOnSeekBarChangeListener(object : SeekBar.OnSeekBarChangeListener {
            override fun onProgressChanged(seekBar: SeekBar?, progress: Int, fromUser: Boolean) {
                if (fromUser) {
                    eq.setBassBoostStrength(progress.toShort())
                    txtBass.text = "Bass Boost: ${progress / 10}%"
                }
            }
            override fun onStartTrackingTouch(seekBar: SeekBar?) {}
            override fun onStopTrackingTouch(seekBar: SeekBar?) {}
        })

        seekVirt.progress = eq.getVirtualizerStrength().toInt()
        seekVirt.setOnSeekBarChangeListener(object : SeekBar.OnSeekBarChangeListener {
            override fun onProgressChanged(seekBar: SeekBar?, progress: Int, fromUser: Boolean) {
                if (fromUser) {
                    eq.setVirtualizerStrength(progress.toShort())
                    txtVirt.text = "3D Spatial Virtualizer: ${progress / 10}%"
                }
            }
            override fun onStartTrackingTouch(seekBar: SeekBar?) {}
            override fun onStopTrackingTouch(seekBar: SeekBar?) {}
        })

        btnReset.setOnClickListener {
            eq.resetToFlat()
            refreshBandSliders(layoutBands, eq)
            seekBass.progress = 0
            seekVirt.progress = 0
            txtBass.text = "Bass Boost: 0%"
            txtVirt.text = "3D Spatial Virtualizer: 0%"
            spinnerPresets.setSelection(0)
        }

        setupBandSliders(layoutBands, eq)
    }

    private fun setupBandSliders(container: LinearLayout, eq: EqualizerController) {
        container.removeAllViews()
        val bandCount = eq.getBandCount()
        val (minLevel, maxLevel) = eq.getBandLevelRange()
        val range = (maxLevel - minLevel).toInt()

        for (i in 0 until bandCount) {
            val bandIndex = i.toShort()
            val freqHz = eq.getBandFrequency(bandIndex)
            val currentLevel = eq.getBandLevel(bandIndex)

            val bandColumn = LinearLayout(requireContext()).apply {
                orientation = LinearLayout.VERTICAL
                gravity = Gravity.CENTER_HORIZONTAL
                layoutParams = LinearLayout.LayoutParams(110, ViewGroup.LayoutParams.MATCH_PARENT).apply {
                    setMargins(8, 0, 8, 0)
                }
            }

            val txtGain = TextView(requireContext()).apply {
                text = "${currentLevel / 100}dB"
                textSize = 10f
                setTextColor(resources.getColor(R.color.text_secondary))
            }

            val seekBar = SeekBar(requireContext()).apply {
                layoutParams = LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MATCH_PARENT,
                    0,
                    1f
                )
                max = range
                progress = (currentLevel - minLevel)
                setOnSeekBarChangeListener(object : SeekBar.OnSeekBarChangeListener {
                    override fun onProgressChanged(sb: SeekBar?, progress: Int, fromUser: Boolean) {
                        if (fromUser) {
                            val newLevel = (progress + minLevel).toShort()
                            eq.setBandLevel(bandIndex, newLevel)
                            txtGain.text = "${newLevel / 100}dB"
                        }
                    }
                    override fun onStartTrackingTouch(sb: SeekBar?) {}
                    override fun onStopTrackingTouch(sb: SeekBar?) {}
                })
            }

            val txtFreq = TextView(requireContext()).apply {
                text = if (freqHz >= 1000) "${freqHz / 1000}k" else "${freqHz}"
                textSize = 11f
                setTextColor(resources.getColor(R.color.text_primary))
            }

            bandColumn.addView(txtGain)
            bandColumn.addView(seekBar)
            bandColumn.addView(txtFreq)
            container.addView(bandColumn)
        }
    }

    private fun refreshBandSliders(container: LinearLayout, eq: EqualizerController) {
        setupBandSliders(container, eq)
    }
}
