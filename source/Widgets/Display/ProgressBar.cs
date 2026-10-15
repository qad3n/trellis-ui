using UnityEngine;

namespace Trellis
{
	internal static partial class Widgets
	{
		private static class BarLayout
		{
			internal const float kHeight = 8f;
			internal const float kMinFillW = 1f;
			internal const float kBusyFraction = 0.3f;
			internal const int kFillSalt = 0x9B0F;
		}

		internal static float barHeight => BarLayout.kHeight;

		internal static void drawProgressBar(Rect area, float ratio, bool indeterminate)
		{
			Theme.drawBlockBg(area);

			Rect fill = indeterminate ? Widgets.busyRect(area) : Widgets.fillRect(area, ratio);
			if (RectMath.w(fill) < BarLayout.kMinFillW)
				return;

			Theme.drawRoundedBox(fill, Theme.m_accent, ColorMath.kTransparent, Theme.cornerR, 0f);
		}

		// keyed by width since a position key churns every frame of a window drag
		private static Rect fillRect(Rect area, float ratio)
		{
			int key = unchecked(RectMath.w(area).GetHashCode() * Widgets.kKeyMix) ^ BarLayout.kFillSalt;

			float shown = MenuAnimation.displayedValue(key, FastMath.clamp01(ratio), false, RectMath.w(area));
			return RectMath.create(RectMath.x(area), RectMath.y(area), RectMath.w(area) * shown, RectMath.h(area));
		}

		private static Rect busyRect(Rect area)
		{
			float segmentW = RectMath.w(area) * BarLayout.kBusyFraction;
			return RectMath.create(RectMath.centerX(area) - segmentW * 0.5f, RectMath.y(area), segmentW, RectMath.h(area));
		}
	}
}
