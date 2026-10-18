using System.Collections.Generic;
using UnityEngine;
using static Trellis.MenuHost;

namespace Trellis
{
	internal static partial class Widgets
	{
		private const float kMultiSelectCheckSize = 14f;
		private const float kMultiSelectCheckPad = 6f;
		private const float kMultiSelectPadLeft = 10f;
		private const int kNeutralState = 0;

		private static readonly Dictionary<int, string> kSelectedCounts = new Dictionary<int, string>();

		internal static string m_multiSelectId;
		internal static string m_multiSelectAnim;
		private static string[] m_multiSelectChecks;
		private static Rect m_multiSelectAnchor;
		private static Vector2 m_multiSelectOffset;
		private static string[] m_multiSelectOptions;
		private static bool[] m_multiSelectValues;
		private static MultiSelectItemHint[] m_multiSelectHints;
		private static int m_multiSelectToggled = -1;
		private static int m_multiSelectToggleFrame = -1;
		private static bool m_multiSelectJustOpened;
		private static bool m_multiSelectUp;
		private static int[] m_cycleStates;
		private static string[] m_cycleLabels;
		private static bool m_multiSelectCycle;

		internal static MultiSelectItemHint[] tooltipHints(string[] tooltips) => RowTable.column(tooltips, Widgets.tooltipHint);
		private static MultiSelectItemHint tooltipHint(string tooltip) => new MultiSelectItemHint(false, null, tooltip);

		private static class CycleLayout
		{
			internal const float kLabelGap = 12f;
			internal const float kRowPadX = 8f;
			internal const float kStatusPad = 8f;
			internal const float kNeutralAlpha = 0.5f;
		}

		private static class HintLayout
		{
			internal const float kNotePad = 8f;
			internal const float kNoteGap = 10f;
			internal const float kInactiveAlpha = 0.5f;
		}

		internal static void closeMultiSelect()
		{
			if (Widgets.m_multiSelectAnim == null)
				return;

			MenuAnimation.close(Widgets.m_multiSelectAnim);

			Widgets.m_multiSelectId = null;
			Widgets.m_multiSelectToggled = -1;
			Widgets.m_multiSelectJustOpened = false;
		}

		internal static void snapCloseMultiSelect()
		{
			if (Widgets.m_multiSelectAnim != null)
				MenuAnimation.snapClose(Widgets.m_multiSelectAnim);

			Widgets.clearMultiSelect();
			Widgets.m_multiSelectToggleFrame = -1;
		}

		internal static void readMultiSelect(Rect viewRect, Vector2 scrollOffset)
		{
			if (Widgets.m_multiSelectId == null)
				return;

			bool interactive = !MenuAnimation.isClosingOrClosed(Widgets.m_multiSelectAnim);

			Rect popupRect = Widgets.multiSelectRect(viewRect, scrollOffset);
			if (interactive && MenuHost.mouseDown0)
			{
				if (RectMath.contains(popupRect, MenuHost.mousePosition))
				{
					int index = Widgets.popupRowAt(popupRect, MenuHost.mousePosition.y);
					if (index >= 0 && index < Widgets.m_multiSelectOptions.Length && Widgets.m_multiSelectToggleFrame != Gui.frameCount)
					{
						if (Widgets.m_multiSelectCycle)
							Widgets.applyCycle(index);
						else
						{
							Widgets.m_multiSelectToggled = index;
							Widgets.m_multiSelectToggleFrame = Gui.frameCount;
						}
					}
				}
				else if (!Widgets.m_multiSelectJustOpened)
				{
					Widgets.closeMultiSelect();
					Widgets.m_dropdownFrame = Gui.frameCount;
				}
			}

			if (!Gui.repainting)
				return;

			Widgets.m_multiSelectJustOpened = false;
		}

		internal static void drawMultiSelectPopup(Rect viewRect, Vector2 scrollOffset)
		{
			string animId = Widgets.m_multiSelectAnim;
			if (animId == null)
				return;

			if (MenuAnimation.consumeClosed(animId))
			{
				Widgets.clearMultiSelect();
				return;
			}

			bool interactive = Widgets.m_multiSelectId != null && !MenuAnimation.isClosingOrClosed(animId);
			if (!interactive && !MenuAnimation.shouldRender(animId))
			{
				Widgets.clearMultiSelect();
				return;
			}

			if (Widgets.m_multiSelectOptions == null)
				return;

			if (Widgets.m_multiSelectCycle)
			{
				if (Widgets.m_cycleStates == null || Widgets.m_cycleLabels == null)
					return;
			}
			else if (Widgets.m_multiSelectValues == null)
				return;

			string keyId = Widgets.m_multiSelectId ?? animId;

			Rect popupRect = Widgets.multiSelectRect(viewRect, scrollOffset);
			using (MenuAnimation.beginReveal(animId, popupRect, Widgets.m_multiSelectUp))
			{
				Theme.drawWindowBg(popupRect);

				if (Widgets.m_multiSelectCycle)
				{
					Widgets.drawCycles(popupRect, interactive);
					return;
				}

				Widgets.syncCheckIds(keyId, Widgets.m_multiSelectOptions.Length);

				int animHash = animId.GetHashCode();
				for (int i = 0; i < Widgets.m_multiSelectOptions.Length; i++)
				{
					Rect itemRect = Widgets.popupRow(popupRect, i);

					MultiSelectItemHint hint = Widgets.hintAt(i);

					bool hovered = interactive && RectMath.contains(itemRect, MenuHost.mousePosition);
					if (hovered)
						MenuHost.showTooltip(hint.m_tooltip);

					// hover bg must land before the checkbox or it paints on top of it
					Widgets.drawItemBg(itemRect, unchecked(animHash * Widgets.kKeyMix) ^ i, hovered, false);

					Rect checkRect = RectMath.create(RectMath.x(itemRect) + Widgets.kMultiSelectCheckPad, RectMath.y(itemRect) + LayoutMath.centerOffset(MenuHost.kDropdownItemH, Widgets.kMultiSelectCheckSize), Widgets.kMultiSelectCheckSize, Widgets.kMultiSelectCheckSize);
					MenuAnimation.drawCheckbox(checkRect, Widgets.m_multiSelectValues[i], hovered, Widgets.m_multiSelectChecks[i]);

					float labelX = RectMath.xMax(checkRect) + Widgets.kMultiSelectCheckPad - Widgets.kMultiSelectPadLeft;

					float labelRight = RectMath.xMax(itemRect);

					Color prevColor = Gui.color;
					if (hint.m_inactive)
						Gui.color = ColorMath.fade(prevColor, HintLayout.kInactiveAlpha);

					if (!string.IsNullOrEmpty(hint.m_note))
					{
						float noteW = UiText.measuredWidth(Theme.valueLabel, hint.m_note);

						float noteX = RectMath.xMax(itemRect) - HintLayout.kNotePad - noteW;

						labelRight = noteX - HintLayout.kNoteGap;

						UiText.fittedLabel(RectMath.create(noteX, RectMath.y(itemRect), noteW, MenuHost.kDropdownItemH), hint.m_note, Theme.valueLabel, TextAnchor.MiddleLeft);
					}
					UiText.fittedLabel(RectMath.create(labelX, RectMath.y(itemRect), FastMath.max(0f, labelRight - labelX), MenuHost.kDropdownItemH), Widgets.m_multiSelectOptions[i], Theme.dropdownItem, TextAnchor.MiddleLeft);

					Gui.color = prevColor;
				}
			}
		}

		internal static bool[] drawMultiSelect(ref float y, float w, string label, string[] options, bool[] selected, float reserveRight = 0f, bool requireOne = false, string tooltip = null, MultiSelectItemHint[] hints = null)
		{
			if (requireOne)
				Widgets.keepOne(selected);

			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, RowPrefix.kMultiSelect, label);

			Rect buttonRect = Widgets.drawDropdownLabel(y, w, label, options, reserveRight, tooltip, out float buttonW, out float anchorX);
			Widgets.multiSelectButton(buttonRect, Widgets.widgetId("ms", label), anchorX, y, buttonW, options, selected, requireOne, hints);

			y += MenuHost.kFieldH + Widgets.kFieldGap;
			Widgets.endRow(ref y, ref row);
			return selected;
		}

		internal static bool drawToggleCycle(ref float y, float w, bool value, string label, string[] options, int[] states, string[] stateLabels, string tooltip = null)
		{
			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, RowPrefix.kToggle, label);

			float offsetX = MenuHost.m_blockOffset.x;

			float offsetY = MenuHost.m_blockOffset.y;

			float buttonW = FastMath.clamp(Widgets.measureOptions(options), MenuHost.kDropdownMaxW, w * DropdownLayout.kInlineShare);

			bool render = Widgets.inlineReveal(label, value, out float reveal);

			float reserveRight = render ? buttonW : 0f;

			float preY = y;

			value = Widgets.drawToggle(ref y, w, value, label, reserveRight, tooltip);
			if (!render)
			{
				Widgets.endRow(ref y, ref row);
				return value;
			}

			Widgets.beginReveal(reveal, value, out Color prevColor, out bool prevEnabled);

			float dropX = offsetX + w - buttonW;

			float anchorX = dropX - offsetX;

			Rect buttonRect = RectMath.create(dropX, offsetY + preY, buttonW, MenuHost.kFieldH);

			string id = Widgets.widgetId("cyms", label);
			if (!value && Widgets.m_multiSelectId == id)
				Widgets.closeMultiSelect();

			bool open = Widgets.m_multiSelectId == id;
			if (open)
				Widgets.syncCycle(anchorX, preY, buttonW, options, states, stateLabels);

			GUIStyle style = open ? Theme.dropdownOpen : Theme.dropdownButton;
			if (Widgets.scaledButton(buttonRect, Widgets.dropdownCaption(Widgets.formatCycle(states)), style, MenuHost.scrollMouse))
			{
				if (open)
					Widgets.closeMultiSelect();
				else
					Widgets.openCycle(id, anchorX, preY, buttonW, options, states, stateLabels);
			}

			Widgets.endReveal(prevColor, prevEnabled);

			Widgets.endRow(ref y, ref row);
			return value;
		}

		private static MultiSelectItemHint hintAt(int index)
		{
			if (Widgets.m_multiSelectHints == null || index < 0 || index >= Widgets.m_multiSelectHints.Length)
				return default;

			return Widgets.m_multiSelectHints[index];
		}

		private static string multiSelectAnim(string id) => $"ms:{id}";

		private static void clearMultiSelect()
		{
			Widgets.m_multiSelectId = null;
			Widgets.m_multiSelectAnim = null;
			Widgets.m_multiSelectOptions = null;
			Widgets.m_multiSelectValues = null;
			Widgets.m_multiSelectHints = null;
			Widgets.m_cycleStates = null;
			Widgets.m_cycleLabels = null;
			Widgets.m_multiSelectChecks = null;
			Widgets.m_multiSelectCycle = false;
			Widgets.m_multiSelectToggled = -1;
			Widgets.m_multiSelectJustOpened = false;
		}

		private static void syncCheckIds(string keyId, int count)
		{
			if (Widgets.m_multiSelectChecks != null && Widgets.m_multiSelectChecks.Length == count)
				return;

			Widgets.m_multiSelectChecks = new string[count];
			for (int i = 0; i < count; i++)
				Widgets.m_multiSelectChecks[i] = $"ms:{keyId}:{i}";
		}

		private static void syncAnchor(float anchorX, float y, float buttonW, string[] options)
		{
			Widgets.m_multiSelectAnchor = RectMath.create(anchorX, y, buttonW, MenuHost.kFieldH);
			Widgets.m_multiSelectOffset = MenuHost.m_blockOffset;
			Widgets.m_multiSelectOptions = options;
		}

		private static void markOpened(string id)
		{
			Widgets.m_multiSelectId = id;
			Widgets.m_multiSelectAnim = Widgets.multiSelectAnim(id);
			Widgets.m_multiSelectToggled = -1;
			Widgets.m_multiSelectJustOpened = true;

			MenuAnimation.noteOpened(Widgets.m_multiSelectAnim);
		}

		private static void syncMultiSelect(float anchorX, float y, float buttonW, string[] options, bool[] values, MultiSelectItemHint[] hints = null)
		{
			Widgets.syncAnchor(anchorX, y, buttonW, options);
			Widgets.m_multiSelectValues = values;
			Widgets.m_multiSelectHints = hints;
			Widgets.m_cycleStates = null;
			Widgets.m_cycleLabels = null;
			Widgets.m_multiSelectCycle = false;
		}

		private static void openMultiSelect(string id, float anchorX, float y, float buttonW, string[] options, bool[] values, MultiSelectItemHint[] hints = null)
		{
			Widgets.syncMultiSelect(anchorX, y, buttonW, options, values, hints);
			Widgets.markOpened(id);
		}

		private static void syncCycle(float anchorX, float y, float buttonW, string[] options, int[] states, string[] stateLabels)
		{
			Widgets.syncAnchor(anchorX, y, buttonW, options);
			Widgets.m_multiSelectValues = null;
			Widgets.m_multiSelectHints = null;
			Widgets.m_cycleStates = states;
			Widgets.m_cycleLabels = stateLabels;
			Widgets.m_multiSelectCycle = true;
		}

		private static void openCycle(string id, float anchorX, float y, float buttonW, string[] options, int[] states, string[] stateLabels)
		{
			Widgets.syncCycle(anchorX, y, buttonW, options, states, stateLabels);
			Widgets.markOpened(id);
		}

		private static string selectionSummary(int count, int total)
		{
			if (count == 0)
				return "None";

			return count == total ? "All" : Widgets.numberDisplay(Widgets.kSelectedCounts, count, " selected");
		}

		private static string formatMultiSelect(bool[] selected, string[] options) => Widgets.selectionSummary(Widgets.countSelected(selected), options.Length);

		private static string formatCycle(int[] states)
		{
			int count = 0;
			foreach (int state in states)
			{
				if (state == Widgets.kNeutralState)
					continue;

				count++;
			}
			return Widgets.selectionSummary(count, states.Length);
		}

		private static void keepOne(bool[] selected)
		{
			if (!(selected != null && selected.Length > 0 && Widgets.countSelected(selected) == 0))
				return;

			selected[0] = true;
		}

		private static void multiSelectButton(Rect buttonRect, string id, float anchorX, float y, float buttonW, string[] options, bool[] selected, bool requireOne, MultiSelectItemHint[] hints)
		{
			bool open = Widgets.m_multiSelectId == id;
			if (open)
				Widgets.syncMultiSelect(anchorX, y, buttonW, options, selected, hints);

			GUIStyle style = open ? Theme.dropdownOpen : Theme.dropdownButton;
			if (Widgets.scaledButton(buttonRect, Widgets.dropdownCaption(Widgets.formatMultiSelect(selected, options)), style, MenuHost.scrollMouse))
			{
				if (open)
					Widgets.closeMultiSelect();
				else
					Widgets.openMultiSelect(id, anchorX, y, buttonW, options, selected, hints);
			}

			if (Widgets.m_multiSelectId != id || Widgets.m_multiSelectToggled < 0)
				return;

			int index = Widgets.m_multiSelectToggled;

			Widgets.m_multiSelectToggled = -1;

			bool wouldEmpty = requireOne && selected[index] && Widgets.countSelected(selected) <= 1;
			if (wouldEmpty)
				return;

			selected[index] = !selected[index];
		}

		private static void applyCycle(int index)
		{
			int count = Widgets.m_cycleLabels.Length;
			Widgets.m_cycleStates[index] = (Widgets.m_cycleStates[index] + 1) % count;
			Widgets.m_multiSelectToggleFrame = Gui.frameCount;
		}

		private static void drawCycles(Rect popupRect, bool interactive)
		{
			Color prevColor = Gui.color;

			float statusW = Widgets.statusWidth();

			float statusX = RectMath.xMax(popupRect) - CycleLayout.kStatusPad - statusW;

			float labelX = RectMath.x(popupRect) + CycleLayout.kRowPadX;

			float labelW = FastMath.max(0f, statusX - CycleLayout.kLabelGap - labelX);

			int animHash = Widgets.m_multiSelectAnim.GetHashCode();
			for (int i = 0; i < Widgets.m_multiSelectOptions.Length; i++)
			{
				Rect itemRect = Widgets.popupRow(popupRect, i);

				bool hovered = interactive && RectMath.contains(itemRect, MenuHost.mousePosition);
				Widgets.drawItemBg(itemRect, unchecked(animHash * Widgets.kKeyMix) ^ i, hovered, false);

				int state = Widgets.m_cycleStates[i];

				bool neutral = state == Widgets.kNeutralState;
				if (neutral)
					Gui.color = ColorMath.fade(prevColor, CycleLayout.kNeutralAlpha);

				UiText.fittedLabel(RectMath.create(labelX, RectMath.y(itemRect), labelW, MenuHost.kDropdownItemH), Widgets.m_multiSelectOptions[i], Theme.label, TextAnchor.MiddleLeft);
				UiText.fittedLabel(RectMath.create(statusX, RectMath.y(itemRect), statusW, MenuHost.kDropdownItemH), Widgets.m_cycleLabels[state], Theme.valueLabel, TextAnchor.MiddleLeft);

				Gui.color = prevColor;
			}
		}

		private static Rect multiSelectRect(Rect viewRect, Vector2 scrollOffset)
		{
			float popupH = Widgets.popupHeight(Widgets.m_multiSelectOptions.Length);

			float itemW = Widgets.m_multiSelectCycle ? Widgets.measureCycles(Widgets.m_multiSelectOptions) : Widgets.measureChecks(Widgets.m_multiSelectOptions);

			float popupW = Widgets.popupWidth(RectMath.w(Widgets.m_multiSelectAnchor), itemW);

			float popupX = RectMath.x(Widgets.m_multiSelectAnchor) + Widgets.m_multiSelectOffset.x - scrollOffset.x + RectMath.x(viewRect);

			float rawY = RectMath.yMax(Widgets.m_multiSelectAnchor) + Widgets.m_multiSelectOffset.y - scrollOffset.y + RectMath.y(viewRect) + DropdownLayout.kPopupOffsetY;

			Rect clamped = MenuHost.clampPopup(RectMath.create(popupX, rawY, popupW, popupH));
			Widgets.m_multiSelectUp = RectMath.y(clamped) < rawY - DropdownLayout.kDropUpTolerance;
			return clamped;
		}

		private static float measureChecks(string[] options)
		{
			float prefix = Widgets.kMultiSelectCheckSize + Widgets.kMultiSelectCheckPad * 2f;

			float widest = 0f;
			for (int i = 0; i < options.Length; i++)
			{
				float optionWidth = UiText.measuredWidth(Theme.dropdownItem, options[i]) + prefix;

				string note = Widgets.hintAt(i).m_note;
				if (!string.IsNullOrEmpty(note))
					optionWidth += HintLayout.kNoteGap + UiText.measuredWidth(Theme.valueLabel, note) + HintLayout.kNotePad;
				if (!(optionWidth > widest))
					continue;

				widest = optionWidth;
			}
			return widest + DropdownLayout.kPopupRightPad;
		}

		private static float statusWidth()
		{
			float widest = 0f;
			for (int i = 0; i < Widgets.m_cycleLabels.Length; i++)
			{
				float statusW = UiText.measuredWidth(Theme.valueLabel, Widgets.m_cycleLabels[i]);
				if (!(statusW > widest))
					continue;

				widest = statusW;
			}
			return widest;
		}

		private static float measureCycles(string[] options)
		{
			float widestLabel = 0f;
			foreach (string option in options)
			{
				float labelW = UiText.measuredWidth(Theme.label, option);
				if (!(labelW > widestLabel))
					continue;

				widestLabel = labelW;
			}
			return CycleLayout.kRowPadX + widestLabel + CycleLayout.kLabelGap + Widgets.statusWidth() + CycleLayout.kStatusPad;
		}
	}
}
