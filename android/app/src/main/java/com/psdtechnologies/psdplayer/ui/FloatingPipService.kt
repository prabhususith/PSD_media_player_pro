package com.psdtechnologies.psdplayer.ui

import android.app.Service
import android.content.Context
import android.content.Intent
import android.graphics.PixelFormat
import android.os.Build
import android.os.IBinder
import android.view.Gravity
import android.view.LayoutInflater
import android.view.MotionEvent
import android.view.View
import android.view.WindowManager
import android.widget.ImageButton
import com.psdtechnologies.psdplayer.R

/**
 * Floating Window Picture-in-Picture Service for Android 6.0 and 7.0 (Redmi Note 4).
 * Uses SYSTEM_ALERT_WINDOW overlay parameters.
 * Author: Prabhu Kannan (PSD Technologies)
 */
class FloatingPipService : Service() {

    private var windowManager: WindowManager? = null
    private var floatingView: View? = null

    companion object {
        fun start(context: Context) {
            val intent = Intent(context, FloatingPipService::class.java)
            context.startService(intent)
        }

        fun stop(context: Context) {
            val intent = Intent(context, FloatingPipService::class.java)
            context.stopService(intent)
        }
    }

    override fun onBind(intent: Intent?): IBinder? = null

    override fun onCreate() {
        super.onCreate()
        windowManager = getSystemService(Context.WINDOW_SERVICE) as WindowManager

        val layoutType = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            WindowManager.LayoutParams.TYPE_APPLICATION_OVERLAY
        } else {
            @Suppress("DEPRECATION")
            WindowManager.LayoutParams.TYPE_PHONE
        }

        val params = WindowManager.LayoutParams(
            WindowManager.LayoutParams.WRAP_CONTENT,
            WindowManager.LayoutParams.WRAP_CONTENT,
            layoutType,
            WindowManager.LayoutParams.FLAG_NOT_FOCUSABLE,
            PixelFormat.TRANSLUCENT
        ).apply {
            gravity = Gravity.TOP or Gravity.START
            x = 100
            y = 100
        }

        floatingView = LayoutInflater.from(this).inflate(R.layout.view_floating_pip, null)

        val btnClose = floatingView?.findViewById<ImageButton>(R.id.btnPipClose)
        val btnMaximize = floatingView?.findViewById<ImageButton>(R.id.btnPipMaximize)

        btnClose?.setOnClickListener {
            stopSelf()
        }

        btnMaximize?.setOnClickListener {
            val intent = Intent(this, PlayerActivity::class.java).apply {
                flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_SINGLE_TOP
            }
            startActivity(intent)
            stopSelf()
        }

        // Touch listener for dragging the floating window
        floatingView?.setOnTouchListener(object : View.OnTouchListener {
            private var initialX = 0
            private var initialY = 0
            private var initialTouchX = 0f
            private var initialTouchY = 0f

            override fun onTouch(v: View, event: MotionEvent): Boolean {
                when (event.action) {
                    MotionEvent.ACTION_DOWN -> {
                        initialX = params.x
                        initialY = params.y
                        initialTouchX = event.rawX
                        initialTouchY = event.rawY
                        return true
                    }
                    MotionEvent.ACTION_MOVE -> {
                        params.x = initialX + (event.rawX - initialTouchX).toInt()
                        params.y = initialY + (event.rawY - initialTouchY).toInt()
                        windowManager?.updateViewLayout(floatingView, params)
                        return true
                    }
                }
                return false
            }
        })

        try {
            windowManager?.addView(floatingView, params)
        } catch (e: Exception) {
            stopSelf()
        }
    }

    override fun onDestroy() {
        super.onDestroy()
        if (floatingView != null) {
            windowManager?.removeView(floatingView)
            floatingView = null
        }
    }
}
