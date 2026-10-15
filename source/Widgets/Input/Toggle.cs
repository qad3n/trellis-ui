using System.Collections.Generic;
using UnityEngine;
using static Trellis.MenuHost;

namespace Trellis
{
	internal static partial class Widgets
	{
		internal const float kFieldGap = 6f;
		internal const int kKeyMix = 397;
		private const float kToggleSize = 20f;
		private const float kToggleGap = 8f;
		private const float kToggleAdvance = 30f;
		private const float kToggleNudgeY = 1f;
		private const float kToggleSlack = 1f;

		private static readonly Dictionary<string, BoldLabelEntry> kBoldLabels = new Dictionary<string, BoldLabelEntry>();

		private static int m_boldVersion = -1;

		private struct BoldLabelEntry
		{
			internal GUIContent m_content;
			internal float m_width;
		}

		internal static bool drawToggle(ref float y, float w, bool value, string label, float reserveRight = 0f, string tooltip = null, bool disabled = false)
		{
			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, RowPrefix.kToggle, label);
			value = Widgets.drawToggleCore(MenuHost.m_blockOffset.x, MenuHost.m_blockOffset.y + y, w, value, label, reserveRight, null, tooltip, disabled, out float rowH);
			y += rowH + (Widgets.kToggleAdvance - Widgets.kToggleSize);
			Widgets.endRow(ref y, ref row);
			return value;
		}

		internal static float toggleWidth(string label) => Widgets.kToggleSize + Widgets.kToggleGap + Widgets.boldLabel(label).m_width + Widgets.kToggleSlack;
		internal static bool drawToggleAt(float x, float y, float w, bool value, string label, string animId, string tooltip = null, bool disabled = false) => Widgets.drawToggleCore(x, y, w, value, label, 0f, animId, tooltip, disabled, out float rowH);

		internal static bool drawToggleColor(ref float y, float w, bool value, string label, string pickerId, ref Color color, string tooltip = null)
		{
			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, RowPrefix.kToggle, label);

			float startY = y;

			bool render = Widgets.inlineReveal(label, value, out float reveal);
			value = Widgets.drawToggle(ref y, w, value, label, render ? MenuHost.kSwatchW : 0f, tooltip);
			if (!render)
			{
				Widgets.endRow(ref y, ref row);
				return value;
			}

			Widgets.beginReveal(reveal, value, out Color prevColor, out bool prevEnabled);

			ColorPicker.drawInline(pickerId, w - MenuHost.kSwatchW, startY, w, ref color);

			Widgets.endReveal(prevColor, prevEnabled);

			Widgets.endRow(ref y, ref row);
			return value;
		}

		internal static bool drawToggleDropdown(ref float y, float w, bool value, string label, string[] options, ref int styleIndex, string tooltip = null)
		{
			Color noColor = default;

			Color noSecondary = default;

			bool[] noSelection = null;
			return Widgets.drawToggleInline(ref y, w, ref value, label, null, ref noColor, null, ref noSecondary, options, ref styleIndex, ref noSelection, false, tooltip);
		}

		internal static bool drawToggleMultiSelectColor(ref float y, float w, bool value, string label, string pickerId, ref Color color, string[] options, ref bool[] selected, string tooltip = null, bool requireOne = false, MultiSelectItemHint[] hints = null)
		{
			Color noSecondary = default;

			int noStyle = 0;
			return Widgets.drawToggleInline(ref y, w, ref value, label, pickerId, ref color, null, ref noSecondary, options, ref noStyle, ref selected, true, tooltip, requireOne, hints);
		}

		internal static bool drawToggleMultiSelect(ref float y, float w, bool value, string label, string[] options, ref bool[] selected, string tooltip = null, bool requireOne = false, MultiSelectItemHint[] hints = null)
		{
			Color noColor = default;

			Color noSecondary = default;

			int noStyle = 0;
			return Widgets.drawToggleInline(ref y, w, ref value, label, null, ref noColor, null, ref noSecondary, options, ref noStyle, ref selected, true, tooltip, requireOne, hints);
		}

		private static int countSelected(bool[] selected)
		{
			int count = 0;
			foreach (bool selectedItem in selected)
			{
				if (!selectedItem)
					continue;

				count++;
			}
			return count;
		}

		private static BoldLabelEntry boldLabel(string label)
		{
			int currentVersion = Theme.layoutVersion;
			if (Widgets.m_boldVersion != currentVersion)
			{
				Widgets.kBoldLabels.Clear();

				Widgets.m_boldVersion = currentVersion;
			}

			if (Widgets.kBoldLabels.TryGetValue(label, out BoldLabelEntry cached))
				return cached;

			BoldLabelEntry entry;
			entry.m_content = new GUIContent($"<b>{label}</b>");
			entry.m_width = Theme.label.CalcSize(entry.m_content).x;
			Widgets.kBoldLabels[label] = entry;
			return entry;
		}

		private static void beginReveal(float reveal, bool value, out Color prevColor, out bool prevEnabled)
		{
			prevColor = Gui.color;
			prevEnabled = Gui.enabled;
			Gui.color = ColorMath.fade(prevColor, reveal);
			if (!value)
				Gui.enabled = false;
		}

		private static void endReveal(Color prevColor, bool prevEnabled)
		{
			Gui.enabled = prevEnabled;
			Gui.color = prevColor;
		}

		private static bool inlineReveal(string label, bool value, out float reveal)
		{
			string id = Widgets.widgetId("inlrev", label);
			if (value)
				MenuAnimation.noteOpened(id);
			else
				MenuAnimation.close(id);

			reveal = MenuAnimation.alphaOf(id);

			bool render = value || MenuAnimation.shouldRender(id);
			if (!render)
				MenuAnimation.consumeClosed(id);
			return render;
		}

		private static bool drawToggleCore(float baseX, float baseY, float w, bool value, string label, float reserveRight, string animId, string tooltip, bool disabled, out float rowH)
		{
			Rect boxRect = RectMath.create(baseX, baseY + Widgets.kToggleNudgeY, Widgets.kToggleSize, Widgets.kToggleSize);

			bool scopeDisabled = !Gui.enabled;

			bool blocked = disabled || scopeDisabled;

			Widgets.beginDisabled(disabled);

			BoldLabelEntry labelEntry = Widgets.boldLabel(label);

			GUIContent labelContent = labelEntry.m_content;

			float labelWidth = labelEntry.m_width;

			float maxW = FastMath.max(0f, w - Widgets.kToggleSize - Widgets.kToggleGap - reserveRight);

			bool wraps = labelWidth > maxW || label.IndexOf('\n') >= 0;

			GUIStyle labelStyle = wraps ? Theme.toggleLabelWrap : Theme.toggleLabel;

			float labelW = wraps ? FastMath.min(labelWidth, maxW) : labelWidth;

			float labelH = wraps ? Widgets.measuredHeight(labelStyle, label, labelContent, labelW) : Widgets.kToggleSize;
			rowH = FastMath.max(Widgets.kToggleSize, labelH);

			Rect labelRect = RectMath.create(baseX + Widgets.kToggleSize + Widgets.kToggleGap, baseY + Widgets.kToggleNudgeY, labelW, labelH);

			Rect clickRect = RectMath.create(baseX, baseY, Widgets.kToggleSize + Widgets.kToggleGap + labelW, rowH);

			bool rowHovered = !blocked && RectMath.contains(clickRect, MenuHost.scrollMouse);

			if (!blocked)
				MenuHost.hoverTooltip(clickRect, tooltip);

			GUIStyle drawStyle = rowHovered ? (wraps ? Theme.toggleLabelWrapHover : Theme.toggleLabelHover) : labelStyle;

			MenuAnimation.drawCheckbox(boxRect, value, rowHovered, animId ?? Widgets.widgetId("tgl", label));

			Gui.label(labelRect, labelContent, drawStyle);

			if (rowHovered && MenuHost.mouseDown0)
			{
				value = !value;
				UiLog.debug($"[Widgets.drawToggleCore] {label} = {(value ? "ON" : "OFF")}");
			}

			Widgets.endDisabled();
			return value;
		}

		internal static bool drawToggleDropdownColor(ref float y, float w, bool value, string label, string pickerId, ref Color color, string[] options, ref int styleIndex, string tooltip = null)
		{
			Color noSecondary = default;

			bool[] noSelection = null;
			return Widgets.drawToggleInline(ref y, w, ref value, label, pickerId, ref color, null, ref noSecondary, options, ref styleIndex, ref noSelection, false, tooltip);
		}

		internal static bool drawToggleDropdownColorPair(ref float y, float w, bool value, string label, string pickerId, ref Color color, string secondaryId, ref Color secondaryColor, string[] options, ref int styleIndex, string tooltip = null)
		{
			bool[] noSelection = null;
			return Widgets.drawToggleInline(ref y, w, ref value, label, pickerId, ref color, secondaryId, ref secondaryColor, options, ref styleIndex, ref noSelection, false, tooltip);
		}

		private static bool drawToggleInline(ref float y, float w, ref bool value, string label, string pickerId, ref Color color, string secondaryId, ref Color secondaryColor, string[] options, ref int styleIndex, ref bool[] selected, bool multiSelect, string tooltip, bool requireOne = false, MultiSelectItemHint[] hints = null)
		{
			// open the row before reading the block offset since a revealing row rebases it
			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, RowPrefix.kToggle, label);

			float offsetX = MenuHost.m_blockOffset.x;

			float offsetY = MenuHost.m_blockOffset.y;

			bool colored = pickerId != null;

			bool secondary = colored && secondaryId != null;

			const float kSwatchGap = 6f;

			float secondaryReserve = secondary ? MenuHost.kSwatchW + kSwatchGap : 0f;

			float swatchReserve = colored ? MenuHost.kSwatchW + secondaryReserve : 0f;

			const float kInlineGap = 10f;

			float swatchGap = colored ? kInlineGap : 0f;

			float buttonW = FastMath.clamp(Widgets.measureOptions(options), MenuHost.kDropdownMaxW, w * DropdownLayout.kInlineShare);

			bool render = Widgets.inlineReveal(label, value, out float reveal);

			float reserveRight = swatchReserve + (render ? buttonW + swatchGap : 0f);

			float preY = y;

			value = Widgets.drawToggle(ref y, w, value, label, reserveRight, tooltip);
			if (multiSelect && requireOne && value)
				Widgets.keepOne(selected);

			if (!render)
			{
				Widgets.endRow(ref y, ref row);
				return value;
			}

			Widgets.beginReveal(reveal, value, out Color prevColor, out bool prevEnabled);

			float dropX = offsetX + w - swatchReserve - swatchGap - buttonW;

			float anchorX = dropX - offsetX;

			Rect buttonRect = RectMath.create(dropX, offsetY + preY, buttonW, MenuHost.kFieldH);
			if (multiSelect)
			{
				string id = Widgets.widgetId("msi", label);
				if (!value && Widgets.m_multiSelectId == id)
					Widgets.closeMultiSelect();

				Widgets.multiSelectButton(buttonRect, id, anchorX, preY, buttonW, options, selected, requireOne, hints);
			}
			else
			{
				string id = Widgets.widgetId("box", label);
				if (!value && Widgets.m_dropdownId == id)
					Widgets.closeDropdown();

				styleIndex = Widgets.dropdownButton(buttonRect, id, anchorX, preY, buttonW, options, styleIndex);
			}

			if (colored)
				ColorPicker.drawInline(pickerId, w - MenuHost.kSwatchW, preY, w, ref color, MenuHost.kSwatchW);

			if (secondary)
				ColorPicker.drawInline(secondaryId, w - MenuHost.kSwatchW - secondaryReserve, preY, w, ref secondaryColor, MenuHost.kSwatchW);

			Widgets.endReveal(prevColor, prevEnabled);

			Widgets.endRow(ref y, ref row);
			return value;
		}
	}
}
