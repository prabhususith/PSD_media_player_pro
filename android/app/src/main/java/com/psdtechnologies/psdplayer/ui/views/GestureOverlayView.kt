package com.psdtechnologies.psdplayer.ui.views

import android.content.Context
import android.util.AttributeSet
import android.view.GestureDetector
import android.view.MotionEvent
import android.view.ScaleGestureDetector
import android.view.View
import kotlin.math.abs

/**
 * Multi-touch gesture detector surface:
 * - Left swipe: Brightness
 * - Right swipe: Volume
 * - Center horizontal swipe: Seek
 * - Double tap: Skip 10s
 * - Pinch: Zoom / Aspect Ratio
 * Author: Prabhu Kannan (PSD Technologies)
 */
class GestureOverlayView @JvmOverloads constructor(
    context: Context,
    attrs: AttributeSet? = null,
    defStyleAttr: Int = 0
) : View(context, attrs, defStyleAttr) {

    interface GestureListener {
        fun onSingleTap()
        fun onDoubleTapLeft()
        fun onDoubleTapRight()
        fun onBrightnessScroll(fraction: Float)
        fun onVolumeScroll(fraction: Float)
        fun onHorizontalSeek(deltaMs: Long)
        fun onGestureEnd()
        fun onPinchZoom(scaleFactor: Float)
    }

    var listener: GestureListener? = null

    private enum class ScrollMode {
        NONE, BRIGHTNESS, VOLUME, SEEK
    }

    private var currentScrollMode = ScrollMode.NONE

    private val gestureDetector = GestureDetector(context, object : GestureDetector.SimpleOnGestureListener() {
        override fun onSingleTapConfirmed(e: MotionEvent): Boolean {
            listener?.onSingleTap()
            return true
        }

        override fun onDoubleTap(e: MotionEvent): Boolean {
            val width = width.toFloat()
            if (e.x < width / 2f) {
                listener?.onDoubleTapLeft()
            } else {
                listener?.onDoubleTapRight()
            }
            return true
        }

        override fun onScroll(
            e1: MotionEvent?,
            e2: MotionEvent,
            distanceX: Float,
            distanceY: Float
        ): Boolean {
            if (e1 == null) return false

            val dx = e2.x - e1.x
            val dy = e1.y - e2.y // Invert so upward drag increases value
            val screenWidth = width.toFloat()
            val screenHeight = height.toFloat()

            if (currentScrollMode == ScrollMode.NONE) {
                if (abs(dx) > abs(dy) && abs(dx) > 30f) {
                    currentScrollMode = ScrollMode.SEEK
                } else if (abs(dy) > abs(dx) && abs(dy) > 30f) {
                    currentScrollMode = if (e1.x < screenWidth / 2f) {
                        ScrollMode.BRIGHTNESS
                    } else {
                        ScrollMode.VOLUME
                    }
                }
            }

            when (currentScrollMode) {
                ScrollMode.BRIGHTNESS -> {
                    val fraction = dy / screenHeight
                    listener?.onBrightnessScroll(fraction)
                }
                ScrollMode.VOLUME -> {
                    val fraction = dy / screenHeight
                    listener?.onVolumeScroll(fraction)
                }
                ScrollMode.SEEK -> {
                    val deltaMs = ((dx / screenWidth) * 90000).toLong() // Max 90s seek across screen width
                    listener?.onHorizontalSeek(deltaMs)
                }
                ScrollMode.NONE -> { }
            }
            return true
        }
    })

    private val scaleDetector = ScaleGestureDetector(context, object : ScaleGestureDetector.SimpleOnScaleGestureListener() {
        override fun onScale(detector: ScaleGestureDetector): Boolean {
            listener?.onPinchZoom(detector.scaleFactor)
            return true
        }
    })

    override fun onTouchEvent(event: MotionEvent): Boolean {
        scaleDetector.onTouchEvent(event)
        val handled = gestureDetector.onTouchEvent(event)

        if (event.action == MotionEvent.ACTION_UP || event.action == MotionEvent.ACTION_CANCEL) {
            if (currentScrollMode != ScrollMode.NONE) {
                currentScrollMode = ScrollMode.NONE
                listener?.onGestureEnd()
            }
        }
        return handled || true
    }
}
