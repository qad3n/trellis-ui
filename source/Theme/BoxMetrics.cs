using UnityEngine;

namespace Trellis
{
	internal readonly struct BoxMetrics
	{
		internal readonly Rect m_outer;
		internal readonly float m_border;
		internal readonly float m_radius;

		private BoxMetrics(Rect outer, float border, float radius)
		{
			m_outer = outer;
			m_border = border;
			m_radius = radius;
		}

		internal bool empty => RectMath.w(m_outer) <= 0f || RectMath.h(m_outer) <= 0f;
		internal Rect inner => RectMath.inset(m_outer, m_border);
		internal float innerRadius => FastMath.max(0f, m_radius - m_border);

		// a border that isnt whole device pixels reads thicker at the corners
		internal static BoxMetrics measure(Rect rect, float radius, float borderWidth)
		{
			Rect snapped = Gui.snapRect(rect);

			float half = FastMath.max(0f, FastMath.min(RectMath.w(snapped), RectMath.h(snapped)) * 0.5f);

			float snappedBorder = borderWidth <= 0f ? 0f : FastMath.min(Gui.snapThickness(borderWidth), half);

			float heldRadius = radius <= 0f ? 0f : FastMath.min(BoxMetrics.arcRadius(radius, snappedBorder), half);
			return new BoxMetrics(snapped, snappedBorder, heldRadius);
		}

		// the shader only ramps one pixel across an arc so a tiny radius renders as a chipped corner
		private static float arcRadius(float radius, float border)
		{
			float pixel = Gui.devicePixel;

			float floor = FastMath.max(Arc.kMinPx * pixel, border > 0f ? border + Arc.kMinInnerPx * pixel : 0f);
			return FastMath.max(Gui.snapThickness(radius), floor);
		}

		private static class Arc
		{
			internal const float kMinPx = 3f;
			// a ring with no inner curve fills its corner pixel solid
			internal const float kMinInnerPx = 2f;
		}
	}
}
