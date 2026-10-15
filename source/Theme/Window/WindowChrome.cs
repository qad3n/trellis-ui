using UnityEngine;

namespace Trellis
{
	internal static class WindowChrome
	{
		internal const int kEdgeRight = 2;
		internal const int kEdgeTop = 4;
		internal const float kEdgeSize = 10f;
		private const int kEdgeLeft = 1;
		private const int kEdgeBottom = 8;
		internal const int kEdgeAll = WindowChrome.kEdgeLeft | WindowChrome.kEdgeRight | WindowChrome.kEdgeTop | WindowChrome.kEdgeBottom;
		private const float kThickness = 2f;

		private static readonly int[] kLastEdges = new int[WindowId.kCount];

		internal static class WindowId
		{
			internal const int kMenu = 0;
			internal const int kSpectators = 1;
			internal const int kChat = 2;
			internal const int kChatMinimal = 3;
			internal const int kKeystrokes = 4;
			internal const int kCount = 5;
		}

		internal static int detectEdges(Rect rect, Vector2 mouse)
		{
			Rect outer = RectMath.create(RectMath.x(rect) - WindowChrome.kEdgeSize, RectMath.y(rect) - WindowChrome.kEdgeSize, RectMath.w(rect) + WindowChrome.kEdgeSize * 2, RectMath.h(rect) + WindowChrome.kEdgeSize * 2);
			if (!RectMath.contains(outer, mouse))
				return 0;

			Rect inner = RectMath.create(RectMath.x(rect) + WindowChrome.kEdgeSize, RectMath.y(rect) + WindowChrome.kEdgeSize, RectMath.w(rect) - WindowChrome.kEdgeSize * 2, RectMath.h(rect) - WindowChrome.kEdgeSize * 2);
			if (RectMath.contains(inner, mouse))
				return 0;

			int edges = 0;
			if (mouse.x < RectMath.x(rect) + WindowChrome.kEdgeSize)
				edges |= WindowChrome.kEdgeLeft;

			if (mouse.x > RectMath.xMax(rect) - WindowChrome.kEdgeSize)
				edges |= WindowChrome.kEdgeRight;

			if (mouse.y < RectMath.y(rect) + WindowChrome.kEdgeSize)
				edges |= WindowChrome.kEdgeTop;

			if (mouse.y > RectMath.yMax(rect) - WindowChrome.kEdgeSize)
				edges |= WindowChrome.kEdgeBottom;
			return edges;
		}

		internal static Rect resize(Rect start, int edges, Vector2 delta, float minW, float minH)
		{
			float x = RectMath.x(start);

			float y = RectMath.y(start);

			float w = RectMath.w(start);

			float h = RectMath.h(start);

			if ((edges & WindowChrome.kEdgeRight) != 0)
				w = FastMath.max(minW, RectMath.w(start) + delta.x);

			if ((edges & WindowChrome.kEdgeBottom) != 0)
				h = FastMath.max(minH, RectMath.h(start) + delta.y);

			if ((edges & WindowChrome.kEdgeLeft) != 0)
			{
				w = FastMath.max(minW, RectMath.w(start) - delta.x);

				x = RectMath.x(start) + RectMath.w(start) - w;
			}

			if ((edges & WindowChrome.kEdgeTop) != 0)
			{
				h = FastMath.max(minH, RectMath.h(start) - delta.y);

				y = RectMath.y(start) + RectMath.h(start) - h;
			}
			return RectMath.create(x, y, w, h);
		}

		internal static void drawEdgesAnimated(Rect rect, int edges, int windowId)
		{
			if (edges != 0)
				WindowChrome.kLastEdges[windowId] = edges;

			float alpha = MenuAnimation.edgeAlpha(windowId, edges != 0);
			if (alpha <= 0f || WindowChrome.kLastEdges[windowId] == 0)
				return;
			if (!Gui.repainting)
				return;

			WindowChrome.drawEdges(rect, WindowChrome.kLastEdges[windowId], alpha);
		}

		internal static void resetEdges(int windowId)
		{
			WindowChrome.kLastEdges[windowId] = 0;

			MenuAnimation.edgeAlpha(windowId, false);
		}

		private static void drawEdges(Rect rect, int edges, float alpha)
		{
			if (alpha <= 0f)
				return;

			bool left = (edges & WindowChrome.kEdgeLeft) != 0;

			bool right = (edges & WindowChrome.kEdgeRight) != 0;

			bool top = (edges & WindowChrome.kEdgeTop) != 0;

			bool bottom = (edges & WindowChrome.kEdgeBottom) != 0;

			Color prevColor = Gui.color;
			Gui.color = ColorMath.fade(prevColor, FastMath.clamp01(alpha));

			if (left)
				Theme.drawEdgeLine(RectMath.create(RectMath.x(rect), RectMath.y(rect), WindowChrome.kThickness, RectMath.h(rect)), true);

			if (right)
				Theme.drawEdgeLine(RectMath.create(RectMath.xMax(rect) - WindowChrome.kThickness, RectMath.y(rect), WindowChrome.kThickness, RectMath.h(rect)), true);

			float edgeX = RectMath.x(rect) + (left ? WindowChrome.kThickness : 0f);

			float edgeWidth = RectMath.w(rect) - (left ? WindowChrome.kThickness : 0f) - (right ? WindowChrome.kThickness : 0f);
			if (top && edgeWidth > 0f)
				Theme.drawEdgeLine(RectMath.create(edgeX, RectMath.y(rect), edgeWidth, WindowChrome.kThickness), false);

			if (bottom && edgeWidth > 0f)
				Theme.drawEdgeLine(RectMath.create(edgeX, RectMath.yMax(rect) - WindowChrome.kThickness, edgeWidth, WindowChrome.kThickness), false);

			Gui.color = prevColor;
		}
	}
}
