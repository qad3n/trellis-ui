using System;
using UnityEngine;
using static Trellis.MenuHost;

namespace Trellis
{
	internal static partial class Widgets
	{
		private static readonly Func<int, float> kSelectRowHeight = _ => SelectRowLayout.kH;
		private static readonly Func<int, float> kCompactRowHeight = _ => SelectRowLayout.kCompactH;
		private static readonly Action<int, Rect, float> kSelectRowDraw = Widgets.drawSelectRowAt;

		// the list draws inside a clip so the row callback reads what it needs from here
		private static Func<int, SelectRowItem> m_selectItems;
		private static string[] m_selectChips;
		private static int m_selectSelected;
		private static bool m_selectCompact;

		private static class SelectRowLayout
		{
			internal const float kH = 42f;
			internal const float kCompactH = 28f;
			internal const float kPadX = 10f;
			internal const float kTopPad = 4f;
			internal const float kTitleH = 18f;
			internal const float kDetailH = 16f;
			internal const float kChipGap = 8f;
			// the title keeps at least this much room before chips stop being added
			internal const float kTitleMinW = 80f;
			internal const int kMaxVisible = 6;
			internal const int kMaxVisibleCompact = 8;
		}

		// a compact list drops the detail line
		internal static int drawSelectList(ref float y, float w, ref ScrollState scroll, int count, int selected, Func<int, SelectRowItem> items, string[] chips, bool compact, out int toggled)
		{
			toggled = -1;
			if (count == 0)
				return -1;

			Widgets.m_selectItems = items;
			Widgets.m_selectChips = chips;
			Widgets.m_selectSelected = selected;
			Widgets.m_selectCompact = compact;

			int clicked = Widgets.drawListPane(ref y, w, Widgets.selectPaneMax(compact), ref scroll, count, compact ? Widgets.kCompactRowHeight : Widgets.kSelectRowHeight, Widgets.kSelectRowDraw, out int hovered);

			Widgets.m_selectItems = null;
			if (clicked < 0 || clicked >= count)
				return -1;

			// the switch column takes the click instead of the row
			if (MenuHost.scrollMouse.x < MenuHost.m_blockOffset.x + SelectRowLayout.kPadX + Widgets.kToggleSize + Widgets.kToggleGap * 0.5f)
			{
				toggled = clicked;
				return -1;
			}
			return clicked;
		}

		// keeps a row the code just picked inside the visible window
		internal static void revealSelectRow(ref ScrollState scroll, int count, int index, bool compact)
		{
			if (index < 0 || index >= count)
				return;

			float rowH = compact ? SelectRowLayout.kCompactH : SelectRowLayout.kH;

			float paneH = FastMath.min(count * (rowH + SelectableListLayout.kRowGap) - SelectableListLayout.kRowGap, Widgets.selectPaneMax(compact));

			float top = index * (rowH + SelectableListLayout.kRowGap);
			if (top < scroll.m_position.y)
				scroll.m_position.y = top;
			else if (top + rowH > scroll.m_position.y + paneH)
				scroll.m_position.y = top + rowH - paneH;
		}

		private static float selectPaneMax(bool compact)
		{
			if (compact)
				return SelectRowLayout.kMaxVisibleCompact * (SelectRowLayout.kCompactH + SelectableListLayout.kRowGap) - SelectableListLayout.kRowGap;

			return SelectRowLayout.kMaxVisible * (SelectRowLayout.kH + SelectableListLayout.kRowGap) - SelectableListLayout.kRowGap;
		}

		// rects here are clip local
		private static void drawSelectRowAt(int index, Rect rowRect, float contentW)
		{
			SelectRowItem item = Widgets.m_selectItems(index);

			float x = RectMath.x(rowRect);

			float top = RectMath.y(rowRect);

			float rowH = RectMath.h(rowRect);
			if (index == Widgets.m_selectSelected)
				Widgets.drawHighlight(rowRect, SelectableListLayout.kStripeW);

			Rect switchRect = RectMath.create(x + SelectRowLayout.kPadX, top + (rowH - Widgets.kToggleSize) * 0.5f, Widgets.kToggleSize, Widgets.kToggleSize);
			MenuAnimation.drawCheckbox(switchRect, item.m_enabled, RectMath.contains(switchRect, Widgets.listMouse), item.m_animId);

			float textX = RectMath.xMax(switchRect) + Widgets.kToggleGap;

			float rightX = x + contentW - SelectRowLayout.kPadX;

			// dimmed rather than disabled because a disabled scope would shift every widget id after the list
			Color prevColor = Gui.color;
			if (!item.m_enabled)
				Gui.color = ColorMath.fade(prevColor, HintLayout.kInactiveAlpha);

			float titleTop = Widgets.m_selectCompact ? top + (rowH - SelectRowLayout.kTitleH) * 0.5f : top + SelectRowLayout.kTopPad;

			float chipsLeft = Widgets.m_selectChips == null ? rightX + SelectRowLayout.kChipGap : Widgets.drawRowChips(rightX, titleTop, Widgets.m_selectChips, item.m_chipMask, textX + SelectRowLayout.kTitleMinW);
			UiText.fittedLabel(RectMath.create(textX, titleTop, chipsLeft - SelectRowLayout.kChipGap - textX, SelectRowLayout.kTitleH), item.m_title, Theme.label, TextAnchor.MiddleLeft);
			if (!Widgets.m_selectCompact)
				UiText.fittedLabel(RectMath.create(textX, top + SelectRowLayout.kTopPad + SelectRowLayout.kTitleH, rightX - textX, SelectRowLayout.kDetailH), item.m_detail, Theme.valueLabel, TextAnchor.MiddleLeft);

			Gui.color = prevColor;
		}

		// returns the left edge the right aligned chips reached
		private static float drawRowChips(float rightX, float chipY, string[] chips, int mask, float minLeft)
		{
			float x = rightX;
			for (int i = chips.Length - 1; i >= 0; i--)
			{
				if ((mask & (1 << i)) == 0)
					continue;
				if (x - Badges.width(chips[i]) < minLeft)
					break;

				x = Badges.draw(x, chipY, chips[i], Theme.m_accent, true);
			}
			return x;
		}
	}
}
