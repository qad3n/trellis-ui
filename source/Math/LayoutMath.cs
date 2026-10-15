using System.Collections.Generic;
using UnityEngine;

namespace Trellis
{
	internal static class LayoutMath
	{
		internal static int hitIndex(float mouseY, float originY, float itemHeight) => FastMath.floorToInt((mouseY - originY) / itemHeight);

		internal static int firstBelow(List<float> bounds, float top)
		{
			int lastRow = bounds.Count - 2;

			int hi = lastRow;

			int result = lastRow + 1;

			int lo = 0;
			while (lo <= hi)
			{
				int mid = (lo + hi) >> 1;
				if (bounds[mid + 1] > top)
				{
					result = mid;

					hi = mid - 1;
				}
				else
					lo = mid + 1;
			}
			return result;
		}

		internal static int lastAbove(List<float> bounds, float bottom)
		{
			int hi = bounds.Count - 2;

			int result = -1;

			int lo = 0;
			while (lo <= hi)
			{
				int mid = (lo + hi) >> 1;
				if (bounds[mid] < bottom)
				{
					result = mid;

					lo = mid + 1;
				}
				else
					hi = mid - 1;
			}
			return result;
		}

		internal static int rowAt(List<float> bounds, float y)
		{
			int hi = bounds.Count - 2;

			int lo = 0;
			while (lo <= hi)
			{
				int mid = (lo + hi) >> 1;
				if (y < bounds[mid])
					hi = mid - 1;
				else if (y >= bounds[mid + 1])
					lo = mid + 1;
				else
					return mid;
			}
			return -1;
		}

		internal static float centerOffset(float containerSize, float elementSize) => (containerSize - elementSize) * 0.5f;

		internal static void clampWindow(ref float winX, ref float winY, ref float winW, ref float winH, float minW, float minH, float maxW, float maxH)
		{
			winW = FastMath.clamp(winW, minW, maxW);

			winH = FastMath.clamp(winH, minH, maxH);

			winX = FastMath.clamp(winX, 0f, maxW - winW);

			winY = FastMath.clamp(winY, 0f, maxH - winH);
		}

		internal static void snapWindow(ref float winX, ref float winY, ref float winW, ref float winH, float minW, float minH)
		{
			winX = Gui.snapValue(winX);

			winY = Gui.snapValue(winY);

			winW = FastMath.max(minW, Gui.snapValue(winW));

			winH = FastMath.max(minH, Gui.snapValue(winH));
		}

		internal static float scrollMax(float contentHeight, float viewHeight) => FastMath.max(0f, contentHeight - viewHeight);
		internal static int scaleFont(int baseSize, float scale, int minPixels = 1) => FastMath.max(minPixels, FastMath.roundToInt(baseSize * scale));

		// unity TRS and the multiply operator are interop invokes so expanded by hand
		internal static Matrix4x4 scaleAt(in Matrix4x4 prev, float scale, float pivotX, float pivotY)
		{
			float offsetX = pivotX * (1f - scale);

			float offsetY = pivotY * (1f - scale);

			Matrix4x4 result = prev;
			result.m00 = prev.m00 * scale;
			result.m01 = prev.m01 * scale;
			result.m03 = prev.m00 * offsetX + prev.m01 * offsetY + prev.m03;
			result.m10 = prev.m10 * scale;
			result.m11 = prev.m11 * scale;
			result.m13 = prev.m10 * offsetX + prev.m11 * offsetY + prev.m13;
			return result;
		}

		internal static Vector2 unscaleAt(Vector2 point, float scale, float pivotX, float pivotY) => VectorMath.v2(pivotX + (point.x - pivotX) / scale, pivotY + (point.y - pivotY) / scale);

		internal static int columnCount(float availableWidth, float blockWidth, float gap, int maxColumns) => FastMath.min(maxColumns, FastMath.max(1, FastMath.floorToInt((availableWidth + gap) / (blockWidth + gap))));
		internal static float edgeFade(float distance, float edgeStart, float aaWidth) => FastMath.clamp01(1f - (distance - edgeStart) / aaWidth);

		internal static float snapDown(float value, float step)
		{
			if (value <= 0f || step <= 0f)
				return value;

			return FastMath.floor(value / step) * step;
		}

		internal static float nestedRadius(float outerRadius, float fromSide, float fromEnd, float cap) => FastMath.min(FastMath.max(0f, outerRadius - FastMath.max(fromSide, fromEnd)), FastMath.max(0f, cap));
	}
}
