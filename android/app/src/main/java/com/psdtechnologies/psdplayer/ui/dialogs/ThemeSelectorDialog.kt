package com.psdtechnologies.psdplayer.ui.dialogs

import android.graphics.Color
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.google.android.material.bottomsheet.BottomSheetDialogFragment
import com.psdtechnologies.psdplayer.R
import com.psdtechnologies.psdplayer.ui.themes.ThemeRegistry

/**
 * Bottom Sheet Modal for selecting from 110+ theme skins.
 * Author: Prabhu Kannan (PSD Technologies)
 */
class ThemeSelectorDialog : BottomSheetDialogFragment() {

    var onThemeSelected: ((ThemeRegistry.ThemeItem) -> Unit)? = null

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View? {
        return inflater.inflate(R.layout.dialog_theme_selector, container, false)
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        val recycler = view.findViewById<RecyclerView>(R.id.recyclerThemes)
        recycler.layoutManager = LinearLayoutManager(requireContext())
        recycler.adapter = ThemeAdapter(ThemeRegistry.themes) { theme ->
            onThemeSelected?.invoke(theme)
            dismiss()
        }
    }

    private class ThemeAdapter(
        private val list: List<ThemeRegistry.ThemeItem>,
        private val onClick: (ThemeRegistry.ThemeItem) -> Unit
    ) : RecyclerView.Adapter<ThemeViewHolder>() {

        override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ThemeViewHolder {
            val v = LayoutInflater.from(parent.context).inflate(R.layout.item_theme_card, parent, false)
            return ThemeViewHolder(v)
        }

        override fun onBindViewHolder(holder: ThemeViewHolder, position: Int) {
            val item = list[position]
            holder.txtName.text = item.name
            holder.txtCategory.text = item.category
            holder.txtAccentCode.text = item.accentHex
            try {
                holder.viewSwatch.setBackgroundColor(Color.parseColor(item.accentHex))
            } catch (e: Exception) {
                holder.viewSwatch.setBackgroundColor(Color.CYAN)
            }
            holder.itemView.setOnClickListener { onClick(item) }
        }

        override fun getItemCount(): Int = list.size
    }

    private class ThemeViewHolder(view: View) : RecyclerView.ViewHolder(view) {
        val txtName: TextView = view.findViewById(R.id.txtThemeName)
        val txtCategory: TextView = view.findViewById(R.id.txtThemeCategory)
        val txtAccentCode: TextView = view.findViewById(R.id.txtThemeAccentCode)
        val viewSwatch: View = view.findViewById(R.id.viewThemeSwatch)
    }
}
