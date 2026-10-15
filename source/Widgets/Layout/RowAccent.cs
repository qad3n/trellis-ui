using UnityEngine;

namespace Trellis
{
	internal static partial class Widgets
	{
		private static class RowAccentLayout
		{
			internal const float kStripeWidth = 3f;
			internal const float kStripeInset = 2f;
			internal const float kTintAlpha = ThemeConstants.kAlphaSubtle;
		}

		internal static void drawRowAccent(Rect rowRect, Color accentColor) => Widgets.drawRowAccent(rowRect, accentColor, RowAccentLayout.kTintAlpha);

		internal static void drawRowAccent(Rect rowRect, Color accentColor, float tintAlpha)
		{
			Gui.fillRect(rowRect, ColorMath.withAlpha(accentColor, tintAlpha));

			Rect stripeSpan = RectMath.create(RectMath.x(rowRect), RectMath.y(rowRect) + RowAccentLayout.kStripeInset, RectMath.w(rowRect), RectMath.h(rowRect) - RowAccentLayout.kStripeInset * 2f);
			Gui.drawStroke(Gui.leftEdge(stripeSpan, RowAccentLayout.kStripeWidth), accentColor);
		}

		internal static void drawHighlight(Rect rowRect, float accentWidth = 0f)
		{
			Gui.fillRect(rowRect, Theme.kSelectionTint);

			if (!(accentWidth > 0f))
				return;

			Gui.drawStroke(Gui.leftEdge(rowRect, accentWidth), Theme.accentBar);
		}
	}
}
