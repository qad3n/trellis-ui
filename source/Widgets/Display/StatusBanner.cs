using UnityEngine;

namespace Trellis
{
	internal static partial class Widgets
	{
		// hashing a Color is an engine invoke
		private static readonly EvictingCache<(string title, string hex), string> kBannerTitles = new EvictingCache<(string title, string hex), string>(RowPrimitiveLayout.kMarkupMax);

		private static class BannerLayout
		{
			internal const float kPadX = 10f;
			internal const float kPadY = 7f;
			internal const float kTitleGap = 3f;
			internal const float kTextInset = RowAccentLayout.kStripeWidth + BannerLayout.kPadX;
			internal const int kFadeSalt = 0x0BA9;
		}

		internal static void statusBanner(float x, float y, float width, string title, string detail, Color accent, out float height)
		{
			Color prevColor = Gui.color;

			float fadeIn = MenuAnimation.fadeAlpha(unchecked(title.GetHashCode() * Widgets.kKeyMix) ^ BannerLayout.kFadeSalt);
			Gui.color = ColorMath.fade(prevColor, fadeIn);

			GUIStyle titleStyle = Widgets.wrapStyle(Theme.label);

			GUIStyle detailStyle = Widgets.wrapStyle(Theme.valueLabel);

			float textW = FastMath.max(0f, width - BannerLayout.kTextInset - BannerLayout.kPadX);

			string titleText = Widgets.bannerTitle(title, accent);

			float titleH = Widgets.measuredHeight(titleStyle, titleText, textW);

			float detailH = string.IsNullOrEmpty(detail) ? 0f : Widgets.measuredHeight(detailStyle, detail, textW);

			float gap = detailH > 0f ? BannerLayout.kTitleGap : 0f;
			height = BannerLayout.kPadY * 2f + titleH + gap + detailH;
			Widgets.drawRowAccent(RectMath.create(x, y, width, height), accent);

			float textX = x + BannerLayout.kTextInset;
			Gui.label(RectMath.create(textX, y + BannerLayout.kPadY, textW, titleH), titleText, titleStyle);

			if (detailH > 0f)
				Gui.label(RectMath.create(textX, y + BannerLayout.kPadY + titleH + gap, textW, detailH), detail, detailStyle);

			Gui.color = prevColor;
		}

		private static string bannerTitle(string title, Color accent)
		{
			(string, string) key = (title, ColorHex.of(accent));
			if (Widgets.kBannerTitles.tryGetValue(key, out string cached))
				return cached;

			string text = RichText.wrapColor(Widgets.bold(title), accent);
			Widgets.kBannerTitles.set(key, text);
			return text;
		}
	}
}
