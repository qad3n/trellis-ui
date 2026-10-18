using UnityEngine;

namespace Trellis
{
	// screen space labels with a shadow, clamped so nothing walks off the edge
	internal static class OverlayText
	{
		internal static readonly Color kShadow = ColorMath.rgba(0f, 0f, 0f, 0.85f);

		private static readonly GUIContent kScratch = new GUIContent();
		private static readonly GUIContent kShadowScratch = new GUIContent();

		// clamping parks every off screen label on the same edge where they stack
		internal static void drawTracked(Rect rect, string text, GUIStyle style, Color color, Color shadow)
		{
			if (!Gui.repainting || string.IsNullOrEmpty(text) || !OverlayText.touchesScreen(rect))
				return;

			OverlayText.draw(rect, RectMath.w(rect), text, style, color, shadow);
		}

		internal static bool touchesScreen(Rect rect) => RectMath.xMax(rect) >= 0f && RectMath.x(rect) <= Gui.spaceWidth && RectMath.yMax(rect) >= 0f && RectMath.y(rect) <= Gui.spaceHeight;

		internal static void drawShadowed(Rect rect, string text, GUIStyle style, Color color, Color shadow)
		{
			if (!Gui.repainting || string.IsNullOrEmpty(text))
				return;

			OverlayText.draw(Gui.clampScreen(rect), RectMath.w(rect), text, style, color, shadow);
		}

		internal static bool fits(float y, float lineHeight) => y + lineHeight <= Gui.spaceHeight;

		private static void draw(Rect bounded, float requestedWidth, string text, GUIStyle style, Color color, Color shadow)
		{
			string fitted = RectMath.w(bounded) < requestedWidth ? UiText.ellipsize(style, text, RectMath.w(bounded)) : text;
			OverlayText.kScratch.text = fitted;

			Gui.setTextColor(style, ColorMath.fade(shadow, color.a));
			Gui.shadowLabel(bounded, OverlayText.shadowContent(fitted), style);
			Gui.setTextColor(style, color);
			Gui.label(bounded, OverlayText.kScratch, style);
		}

		private static GUIContent shadowContent(string fitted)
		{
			string plain = RichText.stripColors(fitted);
			if (object.ReferenceEquals(plain, fitted))
				return OverlayText.kScratch;

			OverlayText.kShadowScratch.text = plain;
			return OverlayText.kShadowScratch;
		}
	}
}
