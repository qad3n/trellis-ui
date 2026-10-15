using System;
using UnityEngine;
using static Trellis.Widgets;

namespace Trellis
{
	internal static partial class MenuHost
	{
		private const string kPopupId = "menu_popup";
		private const float kScrimAlpha = 0.5f;

		private static bool m_popupActive;
		private static string m_popupMessage;
		private static bool m_popupConfirm;
		private static Action m_onConfirm;
		private static string m_popupMeasured;
		private static int m_popupVersion = -1;
		private static float m_popupTextH;

		private static class PopupLayout
		{
			internal const float kWidth = 360f;
			internal const float kButtonW = 90f;
			internal const float kButtonH = 32f;
			internal const float kPad = 20f;
			internal const float kButtonGap = 10f;
		}

		internal static void showConfirm(string message, Action onConfirm)
		{
			MenuHost.m_popupActive = true;
			MenuHost.m_popupMessage = message;
			MenuHost.m_popupConfirm = true;
			MenuHost.m_onConfirm = onConfirm;

			MenuAnimation.noteOpened(MenuHost.kPopupId);
		}

		internal static void showInfo(string message)
		{
			MenuHost.m_popupActive = true;
			MenuHost.m_popupMessage = message;
			MenuHost.m_popupConfirm = false;
			MenuHost.m_onConfirm = null;

			MenuAnimation.noteOpened(MenuHost.kPopupId);
		}

		private static void dismissPopup() => MenuAnimation.close(MenuHost.kPopupId);

		private static void snapPopup()
		{
			if (!MenuHost.m_popupActive && !MenuAnimation.shouldRender(MenuHost.kPopupId))
				return;

			MenuAnimation.snapClose(MenuHost.kPopupId);

			MenuHost.m_popupActive = false;
			MenuHost.m_onConfirm = null;
		}

		private static float measurePopup()
		{
			if (MenuHost.m_popupMeasured != MenuHost.m_popupMessage || MenuHost.m_popupVersion != Theme.layoutVersion)
			{
				GUIContent content = Theme.kScratchContent;
				content.text = MenuHost.m_popupMessage;

				float textW = PopupLayout.kWidth - PopupLayout.kPad * 2f;
				MenuHost.m_popupTextH = Theme.popupLabel.CalcHeight(content, textW);
				MenuHost.m_popupMeasured = MenuHost.m_popupMessage;
				MenuHost.m_popupVersion = Theme.layoutVersion;
			}
			return MenuHost.m_popupTextH;
		}

		private static void layoutPopup(out float popupX, out float popupY, out float popupH, out float textH)
		{
			textH = MenuHost.measurePopup();
			popupH = PopupLayout.kPad + textH + PopupLayout.kPad + PopupLayout.kButtonH + PopupLayout.kPad;

			popupX = MenuHost.m_winX + (MenuHost.winW - PopupLayout.kWidth) / 2f;

			popupY = MenuHost.m_winY + (MenuHost.winH - popupH) / 2f;
		}

		private static void popupButtons(out Rect okRect, out Rect cancelRect)
		{
			MenuHost.layoutPopup(out float popupX, out float popupY, out float popupH, out float textH);

			float buttonY = popupY + PopupLayout.kPad + textH + PopupLayout.kPad;
			if (MenuHost.m_popupConfirm)
			{
				float buttonsW = PopupLayout.kButtonW * 2f + PopupLayout.kButtonGap;

				float startX = popupX + (PopupLayout.kWidth - buttonsW) / 2f;
				okRect = RectMath.create(startX, buttonY, PopupLayout.kButtonW, PopupLayout.kButtonH);

				cancelRect = RectMath.create(startX + PopupLayout.kButtonW + PopupLayout.kButtonGap, buttonY, PopupLayout.kButtonW, PopupLayout.kButtonH);
			}
			else
			{
				float startX = popupX + (PopupLayout.kWidth - PopupLayout.kButtonW) / 2f;
				okRect = RectMath.create(startX, buttonY, PopupLayout.kButtonW, PopupLayout.kButtonH);

				cancelRect = RectMath.create(0f, 0f, 0f, 0f);
			}
		}

		private static void pollPopup()
		{
			if (!MenuHost.m_popupActive || MenuAnimation.isClosingOrClosed(MenuHost.kPopupId))
				return;
			if (string.IsNullOrEmpty(MenuHost.m_popupMessage))
				return;
			if (!MenuHost.mouseDown0)
				return;

			MenuHost.popupButtons(out Rect okRect, out Rect cancelRect);
			if (RectMath.contains(okRect, MenuHost.mousePosition))
			{
				Action onConfirm = MenuHost.m_onConfirm;

				MenuHost.m_onConfirm = null;
				MenuHost.dismissPopup();
				MenuHost.consumeMouseDown0();

				onConfirm?.Invoke();
				return;
			}

			if (!(MenuHost.m_popupConfirm && RectMath.contains(cancelRect, MenuHost.mousePosition)))
				return;

			MenuHost.m_onConfirm = null;
			MenuHost.dismissPopup();
			MenuHost.consumeMouseDown0();
		}

		private static void drawPopup()
		{
			if (MenuAnimation.consumeClosed(MenuHost.kPopupId))
			{
				MenuHost.m_popupActive = false;
				MenuHost.m_onConfirm = null;
			}
			if (!MenuHost.m_popupActive && !MenuAnimation.shouldRender(MenuHost.kPopupId))
				return;

			float popupAlpha = MenuAnimation.alphaOf(MenuHost.kPopupId);

			Color prev = Gui.color;

			Gui.color = ColorMath.kWhite;

			Theme.drawRoundedTint(RectMath.create(MenuHost.m_winX, MenuHost.m_winY, MenuHost.winW, MenuHost.winH), ColorMath.withAlpha(ColorMath.kBlack, FastMath.clamp01(MenuHost.kScrimAlpha * AnimationConfig.DimStrength) * MenuHost.displayFade * popupAlpha));

			Gui.color = ColorMath.withAlpha(ColorMath.kWhite, MenuHost.displayFade);

			MenuHost.layoutPopup(out float popupX, out float popupY, out float popupH, out float textH);

			bool interactive = MenuHost.m_popupActive && !MenuAnimation.isClosingOrClosed(MenuHost.kPopupId);
			using (MenuAnimation.beginSheet(MenuHost.kPopupId, MotionMath.extent(PopupLayout.kWidth, popupH)))
			{
				Theme.drawPopupBg(RectMath.create(popupX, popupY, PopupLayout.kWidth, popupH));
				Theme.kScratchContent.text = MenuHost.m_popupMessage;

				float textW = PopupLayout.kWidth - PopupLayout.kPad * 2f;
				Gui.label(RectMath.create(popupX + PopupLayout.kPad, popupY + PopupLayout.kPad, textW, textH), Theme.kScratchContent, Theme.popupLabel);

				MenuHost.popupButtons(out Rect okRect, out Rect cancelRect);

				Widgets.beginDisabled(!interactive);
				Widgets.scaledButton(okRect, "OK", Theme.popupButton, MenuHost.mousePosition);

				if (MenuHost.m_popupConfirm)
					Widgets.scaledButton(cancelRect, "Cancel", Theme.button, MenuHost.mousePosition);

				Widgets.endDisabled();
			}

			Gui.color = prev;
		}
	}
}
