using System.Runtime.CompilerServices;
using UnityEngine;

namespace Trellis
{
	// every Rect property and the ctor are boxing interop stubs
	internal static class RectMath
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Rect create(float x, float y, float width, float height)
		{
			Rect rect = default;
			rect.m_XMin = x;
			rect.m_YMin = y;
			rect.m_Width = width;
			rect.m_Height = height;
			return rect;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Rect minMax(float xMin, float yMin, float xMax, float yMax) => RectMath.create(xMin, yMin, xMax - xMin, yMax - yMin);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float x(in Rect rect) => rect.m_XMin;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float y(in Rect rect) => rect.m_YMin;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float w(in Rect rect) => rect.m_Width;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float h(in Rect rect) => rect.m_Height;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float xMax(in Rect rect) => rect.m_XMin + rect.m_Width;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float yMax(in Rect rect) => rect.m_YMin + rect.m_Height;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float centerX(in Rect rect) => rect.m_XMin + rect.m_Width * 0.5f;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float centerY(in Rect rect) => rect.m_YMin + rect.m_Height * 0.5f;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool contains(in Rect rect, Vector2 point) => RectMath.contains(rect, point.x, point.y);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Rect offset(in Rect rect, float dx, float dy) => RectMath.create(rect.m_XMin + dx, rect.m_YMin + dy, rect.m_Width, rect.m_Height);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Rect inset(in Rect rect, float amount) => RectMath.create(rect.m_XMin + amount, rect.m_YMin + amount, rect.m_Width - amount * 2f, rect.m_Height - amount * 2f);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Rect inflate(in Rect rect, float amount) => RectMath.inset(rect, -amount);

		// matches Rect.Contains closed min open max
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool contains(in Rect rect, float px, float py) => px >= rect.m_XMin && px < rect.m_XMin + rect.m_Width && py >= rect.m_YMin && py < rect.m_YMin + rect.m_Height;
	}
}
