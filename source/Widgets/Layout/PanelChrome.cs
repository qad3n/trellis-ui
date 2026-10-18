using UnityEngine;

namespace Trellis
{
	internal static partial class Widgets
	{
		private const float kScrimAlpha = 0.55f;

		internal static void drawScrim(float screenW, float screenH, float openAlpha)
		{
			if (!Gui.repainting)
				return;

			Color prev = Gui.color;

			Gui.color = ColorMath.withAlpha(ColorMath.kBlack, FastMath.clamp01(Widgets.kScrimAlpha * AnimationConfig.DimStrength) * openAlpha);
			Gui.drawTexture(RectMath.create(0f, 0f, screenW, screenH), Gui.whiteTexture);
			Gui.color = prev;
		}

		internal static void drawBox(Rect rect, Color fill, Color border) => Theme.drawRoundedBox(rect, fill, border, Theme.cornerR, Theme.borderWidth);

		internal static void drawCardBg(Rect cardRect, Color accentColor)
		{
			Theme.drawBlockBg(cardRect);
			Theme.drawTopAccent(cardRect, accentColor);
		}

		internal static void drawPopupTitle(Rect headerRect, float pad, float badgesLeft, string title, GUIStyle titleStyle)
		{
			float titleX = RectMath.x(headerRect) + pad;

			float titleW = FastMath.max(0f, badgesLeft - Badges.kGap - titleX);
			UiText.alignedLabel(RectMath.create(titleX, RectMath.y(headerRect), titleW, RectMath.h(headerRect)), title, titleStyle, TextAnchor.MiddleLeft);
		}
	}
}
