using System;
using UnityEngine;
using static Trellis.MenuHost;

namespace Trellis
{
	internal static partial class Widgets
	{
		private static class SelectableListLayout
		{
			internal const float kBgInset = 4f;
			internal const float kRowGap = 4f;
			internal const int kHoverSalt = 0x5E17;
			internal const float kHoverEpsilon = 0.001f;
			internal const float kStripeW = 3f;
		}

		// the mouse in clip local space is offscreen when the list is not interactive
		internal static Vector2 listMouse => Widgets.m_listMouse;

		private static Vector2 m_listMouse;

		internal static int drawSelectableList(float x, float y, float w, float listH, ref ScrollState scroll, int count, Func<int, float> rowHeight, float barW, float gutter, float scrollSpeed, bool interactive, Action<int, Rect, float> drawRow, bool reserveBar = false)
		{
			float totalH = 0f;
			for (int i = 0; i < count; i++)
				totalH += rowHeight(i) + SelectableListLayout.kRowGap;

			float scrollMax = LayoutMath.scrollMax(totalH, listH);

			bool scrollable = scrollMax > 0f;

			float scrollbarX = x + w - barW;

			bool barVisible = scrollable || Widgets.scrollbarVisible(ref scroll, scrollMax);

			bool reserved = barVisible || reserveBar;

			float contentW = reserved ? w - barW - gutter : w;

			Rect bgRect = RectMath.create(x - SelectableListLayout.kBgInset, y - SelectableListLayout.kBgInset, contentW + SelectableListLayout.kBgInset, listH + SelectableListLayout.kBgInset * 2f);
			Theme.drawLogBg(bgRect);

			Rect viewRect = RectMath.create(x, y, w, listH);
			if (interactive)
			{
				Widgets.readScroll(viewRect, ref scroll, scrollMax, scrollSpeed);
				Widgets.reserveScrollbar(scrollbarX, y, barW, listH, scrollMax);
			}
			else
				scroll.m_position.y = FastMath.clamp(scroll.m_position.y, 0f, scrollMax);

			Vector2 clipMouse = interactive ? VectorMath.v2(MenuHost.scrollMouse.x - x, MenuHost.scrollMouse.y - y) : MenuHost.kOffscreenMouse;
			Widgets.m_listMouse = clipMouse;

			int listKey = unchecked((x.GetHashCode() * Widgets.kKeyMix) ^ y.GetHashCode()) ^ SelectableListLayout.kHoverSalt;

			Gui.beginClip(viewRect);

			float entryY = -scroll.m_position.y;
			for (int i = 0; i < count; i++)
			{
				float rowH = rowHeight(i);
				if (entryY + rowH < 0f || entryY > listH)
				{
					entryY += rowH + SelectableListLayout.kRowGap;
					continue;
				}

				Rect rowRect = RectMath.create(0f, entryY, contentW, rowH);
				Widgets.drawRowBg(rowRect, unchecked(listKey * Widgets.kKeyMix) ^ i, RectMath.contains(rowRect, clipMouse), false);

				drawRow(i, rowRect, contentW);

				entryY += rowH + SelectableListLayout.kRowGap;
			}

			Gui.endClip();

			int clicked = -1;
			if (interactive && RectMath.contains(viewRect, MenuHost.scrollMouse) && MenuHost.scrollMouse.x < x + contentW && MenuHost.mouseDown0)
				clicked = Widgets.rowAt(count, rowHeight, MenuHost.scrollMouse.y - y + scroll.m_position.y);

			if (barVisible)
				Widgets.drawScrollbar(scrollbarX, y, barW, listH, scrollMax, ref scroll);
			return clicked;
		}

		// a list inside the block flow that shrinks to its rows and scrolls past maxH
		internal static int drawListPane(ref float y, float w, float maxH, ref ScrollState scroll, int count, Func<int, float> rowHeight, Action<int, Rect, float> drawRow, out int hovered)
		{
			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, RowPrefix.kList, null);

			float contentH = -SelectableListLayout.kRowGap;
			for (int i = 0; i < count; i++)
				contentH += rowHeight(i) + SelectableListLayout.kRowGap;

			// the background bleeds past the list so pull the list in to keep the box on the column
			float inset = SelectableListLayout.kBgInset;
			Rect pane = RectMath.create(MenuHost.m_blockOffset.x + inset, MenuHost.m_blockOffset.y + y + inset, w - inset, FastMath.clamp(contentH, 0f, maxH));

			bool inside = Gui.enabled && RectMath.contains(pane, MenuHost.scrollMouse);

			hovered = inside ? Widgets.rowAt(count, rowHeight, MenuHost.scrollMouse.y - RectMath.y(pane) + scroll.m_position.y) : -1;

			int clicked = -1;
			if (!MenuHost.rowCullable(RectMath.y(pane), RectMath.h(pane)))
				clicked = Widgets.drawSelectableList(RectMath.x(pane), RectMath.y(pane), RectMath.w(pane), RectMath.h(pane), ref scroll, count, rowHeight, ScrollLayout.kBarWidth, ScrollLayout.kGutter, ScrollLayout.kSpeed, Gui.enabled, drawRow);

			y += RectMath.h(pane) + inset * 2f + Widgets.kFieldGap;
			Widgets.endRow(ref y, ref row);
			return clicked;
		}

		internal static void drawRowBg(Rect rowRect, int hoverKey, bool hovered, bool selected)
		{
			float hoverT = MenuAnimation.hoverProgress(hoverKey, hovered);

			if (!Gui.repainting)
				return;

			if (selected)
			{
				Gui.drawTextureSnapped(rowRect, Theme.accentFaint);
				Gui.drawStroke(Gui.leftEdge(rowRect, SelectableListLayout.kStripeW), Theme.accentBar);
			}

			if (hoverT <= SelectableListLayout.kHoverEpsilon)
				return;

			Widgets.drawFaded(rowRect, Theme.accentFaint, hoverT);
		}

		internal static void drawEmptyList(float x, float y, float w, float listH, string message)
		{
			Theme.drawLogBg(RectMath.create(x - SelectableListLayout.kBgInset, y - SelectableListLayout.kBgInset, w + SelectableListLayout.kBgInset, listH + SelectableListLayout.kBgInset * 2f));

			UiText.alignedLabel(RectMath.create(x, y, w, listH), message, Theme.valueLabel, TextAnchor.MiddleCenter);
		}

		private static int rowAt(int count, Func<int, float> rowHeight, float localY)
		{
			if (localY < 0f)
				return -1;

			float offset = 0f;
			for (int i = 0; i < count; i++)
			{
				float rowH = rowHeight(i);
				if (localY < offset + rowH)
					return i;
				if (localY < offset + rowH + SelectableListLayout.kRowGap)
					return -1;

				offset += rowH + SelectableListLayout.kRowGap;
			}
			return -1;
		}
	}
}
