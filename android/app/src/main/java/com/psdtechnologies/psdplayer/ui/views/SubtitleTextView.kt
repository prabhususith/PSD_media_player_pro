package com.psdtechnologies.psdplayer.ui.views

import android.content.Context
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.Paint
import android.util.AttributeSet
import androidx.appcompat.widget.AppCompatTextView

/**
 * High-Contrast Subtitle Text View with outer outline/shadow.
 * Author: Prabhu Kannan (PSD Technologies)
 */
class SubtitleTextView @JvmOverloads constructor(
    context: Context,
    attrs: AttributeSet? = null,
    defStyleAttr: Int = 0
) : AppCompatTextView(context, attrs, defStyleAttr) {

    private val strokePaint = Paint().apply {
        isAntiAlias = true
        style = Paint.Style.STROKE
        strokeWidth = 6f
        color = Color.BLACK
    }

    init {
        setTextColor(Color.WHITE)
        textSize = 18f
        setShadowLayer(4f, 2f, 2f, Color.BLACK)
    }

    fun setSubtitleText(text: CharSequence?) {
        if (text.isNullOrBlank()) {
            visibility = GONE
        } else {
            setText(text)
            visibility = VISIBLE
        }
    }

    override fun onDraw(canvas: Canvas) {
        val currentTextColor = currentTextColor
        strokePaint.textSize = textSize
        strokePaint.typeface = typeface

        // Draw outline
        paint.style = Paint.Style.STROKE
        paint.strokeWidth = 6f
        setTextColor(Color.BLACK)
        super.onDraw(canvas)

        // Draw fill text
        paint.style = Paint.Style.FILL
        setTextColor(currentTextColor)
        super.onDraw(canvas)
    }
}
