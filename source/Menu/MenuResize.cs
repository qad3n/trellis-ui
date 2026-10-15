using UnityEngine;

namespace Trellis
{
	internal static partial class MenuHost
	{
		private const float kMinDpi = 0.0001f;

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

			MenuHost.winW = FastMath.clamp(MenuHost.winW, MenuHost.kMinW, designW);
			MenuHost.winH = FastMath.clamp(MenuHost.winH, MenuHost.kMinH, designH);

			float minX = MenuHost.kMinTitle + MenuHost.kEdgeSize - MenuHost.winW;

			float maxX = designW - MenuHost.kMinTitle - MenuHost.kEdgeSize;
			MenuHost.m_winX = FastMath.clamp(MenuHost.m_winX, minX, maxX);
			MenuHost.m_winY = FastMath.clamp(MenuHost.m_winY, 0f, designH - MenuHost.kTitleBarH);
		}

		internal static void clampOverlay(ref float winX, ref float winY, ref float winW, ref float winH, float minW, float minH) => LayoutMath.clampWindow(ref winX, ref winY, ref winW, ref winH, minW, minH, MenuHost.kBaseWidth, MenuHost.baseHeight);
		internal static void clampScaled(ref float winX, ref float winY, ref float winW, ref float winH, float minW, float minH) => LayoutMath.clampWindow(ref winX, ref winY, ref winW, ref winH, minW, minH, MenuHost.designW, MenuHost.designH);

		private static int detectEdges(Vector2 mouse) => WindowChrome.detectEdges(RectMath.create(MenuHost.m_winX, MenuHost.m_winY, MenuHost.winW, MenuHost.winH), mouse);
		private static void drawEdges(int edges) => WindowChrome.drawEdgesAnimated(RectMath.create(MenuHost.m_winX, MenuHost.m_winY, MenuHost.winW, MenuHost.winH), edges, WindowChrome.WindowId.kMenu);
		private static void snapWindow() => LayoutMath.snapWindow(ref MenuHost.m_winX, ref MenuHost.m_winY, ref MenuHost.winW, ref MenuHost.winH, MenuHost.kMinW, MenuHost.kMinH);
	}
}
