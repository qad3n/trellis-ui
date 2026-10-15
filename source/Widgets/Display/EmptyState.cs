using UnityEngine;

namespace Trellis
{
	internal static partial class Widgets
	{
		private static class EmptyCard
		{
			internal const int kFadeSalt = 0x3AD3;
			internal const float kMinW = 240f;
			internal const float kMaxW = 380f;
			internal const float kSideInset = 80f;
			internal const float kPadX = 18f;
			internal const float kPadTop = 16f;
			internal const float kPadBot = 18f;
			internal const float kTitleH = 24f;
			internal const float kGap = 6f;
			internal const float kStripeH = 3f;
			internal const float kActionGap = 12f;
			internal const float kActionH = 28f;
			internal const float kActionW = 110f;
		}

		internal static void drawEmptyState(Rect area, string title, string message = null)
		{
			Color prev = Widgets.beginEmptyFade(title);

			Widgets.drawEmptyCard(area, title, message, 0f, out Rect cardRect);

			Gui.color = prev;
		}

		internal static bool drawEmptyStateAction(Rect area, string title, string message, string actionLabel)
		{
			Color prev = Widgets.beginEmptyFade(title);

			float reservedH = EmptyCard.kActionGap + EmptyCard.kActionH;
			Widgets.drawEmptyCard(area, title, message, reservedH, out Rect cardRect);

			float buttonY = RectMath.y(cardRect) + RectMath.h(cardRect) - EmptyCard.kPadBot - EmptyCard.kActionH;

			float buttonX = RectMath.x(cardRect) + (RectMath.w(cardRect) - EmptyCard.kActionW) * 0.5f;

			bool clicked = Widgets.scrollButton(RectMath.create(buttonX, buttonY, EmptyCard.kActionW, EmptyCard.kActionH), actionLabel, Theme.button);

			Gui.color = prev;
			return clicked;
		}

		private static Color beginEmptyFade(string title)
		{
			Color prev = Gui.color;

			float alpha = MenuAnimation.fadeAlpha(unchecked(title.GetHashCode() * Widgets.kKeyMix) ^ EmptyCard.kFadeSalt);
			Gui.color = ColorMath.fade(prev, alpha);
			return prev;
		}

		private static void drawEmptyCard(Rect area, string title, string message, float extraH, out Rect cardRect)
		{
			float cardW = FastMath.clamp(RectMath.w(area) - EmptyCard.kSideInset, EmptyCard.kMinW, FastMath.min(EmptyCard.kMaxW, RectMath.w(area)));

			float innerW = cardW - EmptyCard.kPadX * 2f;

			// a wrapping copy so the shared value style is never flipped mid pass
			GUIStyle messageStyle = Widgets.wrapStyle(Theme.valueLabel);

			bool anyMessage = !string.IsNullOrEmpty(message);

			float messageH = anyMessage ? Widgets.measuredHeight(messageStyle, message, innerW) : 0f;

			float cardH = EmptyCard.kPadTop + EmptyCard.kTitleH + (anyMessage ? EmptyCard.kGap + messageH : 0f) + extraH + EmptyCard.kPadBot;

			float cardX = RectMath.x(area) + (RectMath.w(area) - cardW) * 0.5f;

			float cardY = RectMath.y(area) + FastMath.max(0f, (RectMath.h(area) - cardH) * 0.5f);
			cardRect = RectMath.create(cardX, cardY, cardW, cardH);
			Theme.drawBlockBg(cardRect);
			Theme.drawTopAccent(cardRect, Theme.accentBar, 0f, EmptyCard.kStripeH);

			float textY = cardY + EmptyCard.kPadTop;
			Widgets.alignedLabel(RectMath.create(cardX + EmptyCard.kPadX, textY, innerW, EmptyCard.kTitleH), title, Theme.title, TextAnchor.MiddleCenter);

			if (!anyMessage)
				return;

			textY += EmptyCard.kTitleH + EmptyCard.kGap;
			Widgets.alignedLabel(RectMath.create(cardX + EmptyCard.kPadX, textY, innerW, messageH), message, messageStyle, TextAnchor.UpperCenter);
		}
	}
}
