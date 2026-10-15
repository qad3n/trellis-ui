using UnityEngine;

namespace Trellis
{
	internal static partial class Widgets
	{
		internal const float kBadgeHeight = 18f;
		internal const float kBadgeGap = BadgeChip.kSpacing;

		private static class BadgeChip
		{
			internal const float kTextPad = 14f;
			internal const float kSpacing = 6f;
			internal const float kFillShade = 0.62f;
			internal const float kBorderAlpha = 0.55f;
		}

		internal static float badgeWidth(string text) => Widgets.measuredWidth(Theme.badgeLabel, text) + BadgeChip.kTextPad;

		internal static float drawBadge(float x, float y, string text, Color color, bool rightAligned = false)
		{
			float badgeWidth = Widgets.badgeWidth(text);

			float drawX = rightAligned ? x - badgeWidth : x;

			Rect box = RectMath.create(drawX, y, badgeWidth, Widgets.kBadgeHeight);
			Widgets.drawChip(box, color);

			Gui.label(box, text, Theme.badgeLabel);
			return rightAligned ? drawX - BadgeChip.kSpacing : drawX + badgeWidth + BadgeChip.kSpacing;
		}

		internal static void drawBox(Rect rect, Color fill, Color border) => Theme.drawRoundedBox(rect, fill, border, Theme.cornerR, Theme.borderWidth);
		// a raw texture fill stayed unsnapped while the shader border snapped
		private static void drawChip(Rect rect, Color color) => Theme.drawRoundedBox(rect, ColorMath.shade(color, BadgeChip.kFillShade), ColorMath.rgba(0f, 0f, 0f, BadgeChip.kBorderAlpha), Theme.cornerR, Theme.borderWidth);
	}
}
