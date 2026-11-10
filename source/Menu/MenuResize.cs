using UnityEngine;

namespace Trellis
{
	internal static partial class MenuHost
	{
		private const float kMinDpi = 0.0001f;

		// the minimum yields to the screen so a high scale never pushes the window past it
		private static float minW => FastMath.min(MenuHost.kMinW, MenuHost.designW);
		private static float minH => FastMath.min(MenuHost.kMinH, MenuHost.designH);

		internal static Rect clampPopup(Rect rect)
		{
			float designHeight = MenuHost.designH;
			if (RectMath.yMax(rect) > designHeight)
				rect.m_YMin = designHeight - RectMath.h(rect);

			if (RectMath.y(rect) < 0f)
				rect.m_YMin = 0f;
			return rect;
		}

		internal static void clampWindow()
		{
			float designW = MenuHost.designW;

			float designH = MenuHost.designH;

			MenuHost.winW = FastMath.clamp(MenuHost.winW, MenuHost.minW, designW);
			MenuHost.winH = FastMath.clamp(MenuHost.winH, MenuHost.minH, designH);

			float minX = MenuHost.kMinTitle + MenuHost.kEdgeSize - MenuHost.winW;

			float maxX = designW - MenuHost.kMinTitle - MenuHost.kEdgeSize;
			MenuHost.m_winX = FastMath.clamp(MenuHost.m_winX, minX, maxX);
			MenuHost.m_winY = FastMath.clamp(MenuHost.m_winY, 0f, designH - MenuHost.kTitleBarH);
		}

		private static void setDpi(float scale)
		{
			float previous = Gui.DpiScale;
			if (FastMath.approximately(previous, scale))
				return;

			Gui.DpiScale = scale;

			// design space is unknown until the first gui pass
			if (Gui.screenHeight <= 0f)
				return;

			MenuHost.recenter(previous / scale);
		}

		// design units shrink as the scale grows so the window keeps its screen centre and fits whole
		private static void recenter(float ratio)
		{
			float centerX = (MenuHost.m_winX + MenuHost.winW * 0.5f) * ratio;
			float centerY = (MenuHost.m_winY + MenuHost.winH * 0.5f) * ratio;

			float designW = MenuHost.designW;
			float designH = MenuHost.designH;
			MenuHost.winW = FastMath.clamp(MenuHost.winW, MenuHost.minW, designW);
			MenuHost.winH = FastMath.clamp(MenuHost.winH, MenuHost.minH, designH);

			MenuHost.m_winX = FastMath.clamp(centerX - MenuHost.winW * 0.5f, 0f, FastMath.max(0f, designW - MenuHost.winW));
			MenuHost.m_winY = FastMath.clamp(centerY - MenuHost.winH * 0.5f, 0f, FastMath.max(0f, designH - MenuHost.winH));
		}

		internal static void clampOverlay(ref float winX, ref float winY, ref float winW, ref float winH, float minW, float minH) => LayoutMath.clampWindow(ref winX, ref winY, ref winW, ref winH, minW, minH, MenuHost.kBaseWidth, MenuHost.baseHeight);
		internal static void clampScaled(ref float winX, ref float winY, ref float winW, ref float winH, float minW, float minH) => LayoutMath.clampWindow(ref winX, ref winY, ref winW, ref winH, minW, minH, MenuHost.designW, MenuHost.designH);

		private static int detectEdges(Vector2 mouse) => WindowChrome.detectEdges(RectMath.create(MenuHost.m_winX, MenuHost.m_winY, MenuHost.winW, MenuHost.winH), mouse);
		private static void drawEdges(int edges) => WindowChrome.drawEdgesAnimated(RectMath.create(MenuHost.m_winX, MenuHost.m_winY, MenuHost.winW, MenuHost.winH), edges, WindowChrome.WindowId.kMenu);
		private static void snapWindow() => LayoutMath.snapWindow(ref MenuHost.m_winX, ref MenuHost.m_winY, ref MenuHost.winW, ref MenuHost.winH, MenuHost.minW, MenuHost.minH);
	}
}
