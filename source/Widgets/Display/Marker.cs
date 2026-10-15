using UnityEngine;

namespace Trellis
{
	internal static partial class Widgets
	{
		internal static void drawMarker(in MarkerFrame frame, Texture texture) => Widgets.drawMarker(frame, VectorMath.kZero2, texture);

		internal static void drawMarker(in MarkerFrame frame, Vector2 offset, Texture texture)
		{
			if (!Gui.repainting)
				return;

			if (frame.m_leavingAlpha > 0f)
			{
				Color prev = Gui.color;
				Gui.color = ColorMath.fade(prev, frame.m_leavingAlpha);
				Gui.drawStroke(RectMath.offset(frame.m_leaving, offset.x, offset.y), texture);
				Gui.color = prev;
			}

			if (RectMath.w(frame.m_live) > 0f && RectMath.h(frame.m_live) > 0f)
				Gui.drawStroke(RectMath.offset(frame.m_live, offset.x, offset.y), texture);
		}
	}
}
