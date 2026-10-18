using System;
using UnityEngine;
using static Trellis.MenuHost;

namespace Trellis
{
	internal static partial class Widgets
	{
		internal const float kRowEndReserve = MenuHost.kFieldH + Widgets.kRowEndGap;
		internal const int kThirdButton = 2;
		private const float kRowEndGap = 6f;
		private const int kNoClick = -1;

		internal static void drawButton(ref float y, float w, string label, GUIStyle style, Action onClick, string tooltip = null)
		{
			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, RowPrefix.kButton, null);

			Rect rect = RectMath.create(MenuHost.m_blockOffset.x, MenuHost.m_blockOffset.y + y, w, ButtonLayout.kHeight);
			if (Gui.enabled)
				MenuHost.hoverTooltip(rect, tooltip);

			if (Widgets.scrollButton(rect, label, style))
				onClick();

			Widgets.drawSpacer(ref y);
			Widgets.endRow(ref y, ref row);
		}

		internal static bool drawRowEndButton(float rowY, float w, string label, string tooltip)
		{
			Rect rect = RectMath.create(MenuHost.m_blockOffset.x + w - MenuHost.kFieldH, MenuHost.m_blockOffset.y + rowY, MenuHost.kFieldH, MenuHost.kFieldH);
			MenuHost.hoverTooltip(rect, tooltip);
			return Widgets.scrollButton(rect, label, Theme.button);
		}

		internal static bool drawButtonRow(ref float y, float w, in ActionButton only)
		{
			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, RowPrefix.kButton, null);

			bool clicked = Widgets.drawActionButtonAt(RectMath.create(MenuHost.m_blockOffset.x, MenuHost.m_blockOffset.y + y, w, ButtonLayout.kHeight), only);

			Widgets.drawSpacer(ref y);
			Widgets.endRow(ref y, ref row);
			return clicked;
		}

		internal static int drawButtonRow(ref float y, float w, in ActionButton left, in ActionButton right)
		{
			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, RowPrefix.kButton, null);

			int clicked = Widgets.drawButtonPairAt(MenuHost.m_blockOffset.x, MenuHost.m_blockOffset.y + y, w, ButtonLayout.kHeight, ButtonLayout.kGap, left, right);

			Widgets.drawSpacer(ref y);
			Widgets.endRow(ref y, ref row);
			return clicked;
		}

		internal static int drawButtonRow(ref float y, float w, in ActionButton left, in ActionButton middle, in ActionButton right)
		{
			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, RowPrefix.kButton, null);

			int clicked = Widgets.drawButtonTripletAt(MenuHost.m_blockOffset.x, MenuHost.m_blockOffset.y + y, w, ButtonLayout.kHeight, ButtonLayout.kGap, left, middle, right);

			Widgets.drawSpacer(ref y);
			Widgets.endRow(ref y, ref row);
			return clicked;
		}

		internal static bool drawActionButtonAt(Rect rect, in ActionButton button)
		{
			if (button.m_label == null)
				return false;

			Widgets.beginDisabled(button.m_disabled);

			if (Gui.enabled)
				MenuHost.hoverTooltip(rect, button.m_tooltip);

			bool clicked = Widgets.scrollButton(rect, button.m_label, button.m_style);

			Widgets.endDisabled();

			if (clicked)
				button.m_onClick?.Invoke();
			return clicked;
		}

		internal static int drawButtonPairAt(float x, float y, float w, float buttonH, float gap, in ActionButton left, in ActionButton right)
		{
			float halfW = (w - gap) * 0.5f;

			int clicked = Widgets.kNoClick;
			if (Widgets.drawActionButtonAt(RectMath.create(x, y, halfW, buttonH), left))
				clicked = 0;

			if (Widgets.drawActionButtonAt(RectMath.create(x + halfW + gap, y, halfW, buttonH), right))
				clicked = 1;
			return clicked;
		}

		internal static int drawButtonTripletAt(float x, float y, float w, float buttonH, float gap, in ActionButton left, in ActionButton middle, in ActionButton right)
		{
			float thirdW = (w - gap * 2f) / 3f;

			int clicked = Widgets.kNoClick;
			if (Widgets.drawActionButtonAt(RectMath.create(x, y, thirdW, buttonH), left))
				clicked = 0;

			if (Widgets.drawActionButtonAt(RectMath.create(x + thirdW + gap, y, thirdW, buttonH), middle))
				clicked = 1;

			if (Widgets.drawActionButtonAt(RectMath.create(x + (thirdW + gap) * 2f, y, thirdW, buttonH), right))
				clicked = Widgets.kThirdButton;
			return clicked;
		}
	}
}
