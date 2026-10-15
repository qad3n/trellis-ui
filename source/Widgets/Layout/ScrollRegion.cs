using System;
using UnityEngine;

namespace Trellis
{
	internal static partial class Widgets
	{
		internal static class ScrollLayout
		{
			internal const float kBarWidth = 8f;
			internal const float kWideBar = 10f;
			internal const float kGutter = 6f;
			internal const float kSpeed = 40f;
			internal const float kSlowSpeed = 30f;
			internal const float kPanelInset = 4f;
		}

		internal readonly struct ScrollRegion
		{
			internal readonly float m_contentW;
			internal readonly float m_viewH;
			internal readonly float m_contentY;

			internal ScrollRegion(float contentW, float viewH, float contentY)
			{
				m_contentW = contentW;
				m_viewH = viewH;
				m_contentY = contentY;
			}
		}

		internal static ScrollRegion drawScrollRegion(Rect area, ref ScrollState state, float contentH, float barW, float gutter, float scrollSpeed, bool interactive, Action<ScrollRegion> drawContent, bool reserveBar = false)
		{
			float scrollMax = LayoutMath.scrollMax(contentH, RectMath.h(area));

			bool scrollable = scrollMax > 0f;

			float scrollbarX = RectMath.x(area) + RectMath.w(area) - barW;

			bool barVisible = scrollable || Widgets.scrollbarVisible(ref state, scrollMax);

			bool reserved = barVisible || reserveBar;

			float contentW = reserved ? RectMath.w(area) - barW - gutter : RectMath.w(area);

			if (interactive)
			{
				Widgets.easeScroll(ref state, scrollMax);
				Widgets.reserveScrollbar(scrollbarX, RectMath.y(area), barW, RectMath.h(area), scrollMax);
			}
			else
				state.m_position.y = FastMath.clamp(state.m_position.y, 0f, scrollMax);

			ScrollRegion region = new ScrollRegion(contentW, RectMath.h(area), -state.m_position.y);

			Rect clipRect = RectMath.create(RectMath.x(area), RectMath.y(area), contentW, RectMath.h(area));

			Vector2 savedMouse = MenuHost.scrollMouse;
			MenuHost.scrollMouse = interactive ? VectorMath.v2(savedMouse.x - RectMath.x(clipRect), savedMouse.y - RectMath.y(clipRect)) : MenuHost.kOffscreenMouse;

			Gui.beginClip(clipRect);

			try
			{
				drawContent(region);
			}
			catch (Exception ex)
			{
				UiLog.logException("Widgets.drawScrollRegion", ex);
			}
			finally
			{
				Gui.endClip();

				MenuHost.scrollMouse = savedMouse;
			}

			// wheel applies after the content so nested views get it first
			if (interactive)
				Widgets.applyWheel(area, ref state, scrollMax, scrollSpeed, MenuHost.scrollMouse);

			if (barVisible)
				Widgets.drawScrollbar(scrollbarX, RectMath.y(area), barW, RectMath.h(area), scrollMax, ref state);
			return region;
		}

		internal static void drawReveal(Rect area, bool interactive, Action<float> drawBody)
		{
			if (RectMath.h(area) <= 0f || drawBody == null)
				return;

			Vector2 savedMouse = MenuHost.scrollMouse;
			MenuHost.scrollMouse = interactive ? VectorMath.v2(savedMouse.x - RectMath.x(area), savedMouse.y - RectMath.y(area)) : MenuHost.kOffscreenMouse;

			Gui.beginClip(area);

			try
			{
				drawBody(RectMath.w(area));
			}
			catch (Exception ex)
			{
				UiLog.logException("Widgets.drawReveal", ex);
			}
			finally
			{
				Gui.endClip();

				MenuHost.scrollMouse = savedMouse;
			}
		}
	}
}
