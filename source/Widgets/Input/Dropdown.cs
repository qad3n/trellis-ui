using System.Collections.Generic;
using UnityEngine;
using static Trellis.MenuHost;

namespace Trellis
{
	internal static partial class Widgets
	{
		private static readonly Dictionary<string, string> kDropdownCaptions = new Dictionary<string, string>();
		private static readonly Dictionary<string[], float> kButtonWidths = new Dictionary<string[], float>();
		private static readonly Dictionary<string[], float> kItemWidths = new Dictionary<string[], float>();
		private static readonly GUIContent kMeasureContent = new GUIContent();

		internal static string m_dropdownId;
		internal static string m_dropdownAnim;
		internal static int m_dropdownFrame = -1;
		// set while an overlay window draws so its dropdown never surfaces in the menu pass
		internal static bool m_overlayPass;
		private static Rect m_dropdownAnchor;
		private static Vector2 m_dropdownOffset;
		private static string[] m_dropdownOptions;
		private static string[] m_dropdownLabels;
		private static int m_dropdownResult = -1;
		private static int m_dropdownSelected = -1;
		private static bool m_dropdownOpened;
		private static bool m_dropdownUp;
		private static bool m_dropdownOverlay;
		private static int m_widthsVersion = -1;

		private static class DropdownLayout
		{
			internal const float kLabelGap = 8f;
			internal const float kMinLabelW = 40f;
			internal const float kPopupOffsetY = 2f;
			internal const float kPopupRightPad = 10f;
			internal const float kStripeW = 2f;
			internal const float kHoverEpsilon = 0.001f;
			internal const string kCaret = "  v";
			internal const string kItemIndent = "  ";
			internal const string kNoSelection = "---";
			internal const float kDropUpTolerance = 0.5f;
			internal const float kInlineShare = 0.5f;
		}

		// the menu pass only answers for dropdowns it opened, an overlay one must survive the hidden menu snapping its popups
		internal static bool menuDropdownOpen => Widgets.m_dropdownId != null && !Widgets.m_dropdownOverlay;
		internal static bool menuDropdownLayered => !Widgets.m_dropdownOverlay && (Widgets.m_dropdownId != null || Live.frameCount == Widgets.m_dropdownFrame);
		internal static bool overlayDropdownLayered => Widgets.m_dropdownOverlay && (Widgets.m_dropdownId != null || Live.frameCount == Widgets.m_dropdownFrame);
		private static float popupInsetX => Theme.borderWidth;
		private static float popupInsetY => FastMath.max(Theme.borderWidth, Theme.cornerR);

		internal static void closeDropdown()
		{
			if (Widgets.m_dropdownAnim == null)
				return;

			MenuAnimation.close(Widgets.m_dropdownAnim);

			Widgets.m_dropdownId = null;
			Widgets.m_dropdownResult = -1;
			Widgets.m_dropdownOpened = false;
		}

		internal static void snapCloseDropdown()
		{
			if (Widgets.m_dropdownAnim != null)
				MenuAnimation.snapClose(Widgets.m_dropdownAnim);

			Widgets.clearDropdown();
			Widgets.m_dropdownFrame = -1;
		}

		internal static void readDropdown(Rect viewRect, Vector2 scrollOffset)
		{
			if (Widgets.m_dropdownId == null || Widgets.m_dropdownOverlay != Widgets.m_overlayPass)
				return;

			bool interactive = !MenuAnimation.isClosingOrClosed(Widgets.m_dropdownAnim);

			Rect popupRect = Widgets.dropdownRect(viewRect, scrollOffset);
			if (interactive && MenuHost.mouseDown0)
			{
				if (RectMath.contains(popupRect, MenuHost.mousePosition))
				{
					int index = Widgets.popupRowAt(popupRect, MenuHost.mousePosition.y);
					if (index >= 0 && index < Widgets.m_dropdownOptions.Length)
					{
						Widgets.m_dropdownResult = index;
						Widgets.m_dropdownFrame = Gui.frameCount;
					}
				}
				else if (!Widgets.m_dropdownOpened)
				{
					Widgets.closeDropdown();
					Widgets.m_dropdownFrame = Gui.frameCount;
				}
			}

			if (!Gui.repainting)
				return;

			Widgets.m_dropdownOpened = false;
		}

		internal static void drawDropdownPopup(Rect viewRect, Vector2 scrollOffset)
		{
			string animId = Widgets.m_dropdownAnim;
			if (animId == null || Widgets.m_dropdownOverlay != Widgets.m_overlayPass)
				return;

			if (MenuAnimation.consumeClosed(animId))
			{
				Widgets.clearDropdown();
				return;
			}

			bool interactive = Widgets.m_dropdownId != null && !MenuAnimation.isClosingOrClosed(animId);
			if (!interactive && !MenuAnimation.shouldRender(animId))
			{
				Widgets.clearDropdown();
				return;
			}

			if (Widgets.m_dropdownOptions == null || Widgets.m_dropdownLabels == null)
				return;

			Rect popupRect = Widgets.dropdownRect(viewRect, scrollOffset);
			using (MenuAnimation.beginReveal(animId, popupRect, Widgets.m_dropdownUp))
			{
				Theme.drawWindowBg(popupRect);

				int animHash = animId.GetHashCode();
				for (int i = 0; i < Widgets.m_dropdownOptions.Length; i++)
				{
					Rect itemRect = Widgets.popupRow(popupRect, i);

					bool hovered = interactive && RectMath.contains(itemRect, MenuHost.mousePosition);

					bool selected = i == Widgets.m_dropdownSelected;
					Widgets.drawItemBg(itemRect, unchecked(animHash * Widgets.kKeyMix) ^ i, hovered, selected);
					Widgets.fittedLabel(itemRect, Widgets.m_dropdownLabels[i], Theme.dropdownItem, TextAnchor.MiddleLeft);
				}
			}
		}

		internal static void drawItemBg(Rect itemRect, int hoverKey, bool hovered, bool selected)
		{
			float hoverT = MenuAnimation.hoverProgress(hoverKey, hovered);

			if (!Gui.repainting)
				return;

			if (selected)
				Gui.drawTextureSnapped(itemRect, Theme.accentFaint);

			if (hoverT > DropdownLayout.kHoverEpsilon)
				Theme.drawChrome(itemRect, Theme.kItemChrome, hoverT, false);

			if (!selected)
				return;

			Gui.drawStroke(Gui.leftEdge(itemRect, DropdownLayout.kStripeW), Theme.accentBar);
		}

		internal static int drawDropdown(ref float y, float w, string label, string[] options, int selectedIndex, float reserveRight = 0f, string tooltip = null)
		{
			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, RowPrefix.kDropdown, label);

			Rect buttonRect = Widgets.drawDropdownLabel(y, w, label, options, reserveRight, tooltip, out float buttonW, out float anchorX);

			int result = Widgets.dropdownButton(buttonRect, Widgets.widgetId("dd", label), anchorX, y, buttonW, options, selectedIndex);

			y += MenuHost.kFieldH + Widgets.kFieldGap;
			Widgets.endRow(ref y, ref row);
			return result;
		}

		// overlay windows have no block, the id must be stable across passes
		internal static int drawDropdownAt(float x, float y, float w, string id, string label, string[] options, int selectedIndex, string tooltip = null)
		{
			Vector2 prevOffset = MenuHost.m_blockOffset;

			MenuHost.m_blockOffset = VectorMath.v2(x, 0f);
			try
			{
				Rect buttonRect = Widgets.drawDropdownLabel(y, w, label, options, 0f, tooltip, out float buttonW, out float anchorX);
				return Widgets.dropdownButton(buttonRect, id, anchorX, y, buttonW, options, selectedIndex);
			}
			finally
			{
				MenuHost.m_blockOffset = prevOffset;
			}
		}

		internal static void closeOverlayDropdown()
		{
			if (Widgets.m_dropdownOverlay && Widgets.m_dropdownAnim != null)
				Widgets.snapCloseDropdown();
		}

		private static Rect drawDropdownLabel(float y, float w, string label, string[] options, float reserveRight, string tooltip, out float buttonW, out float anchorX)
		{
			buttonW = Widgets.buttonWidth(w, Widgets.measureOptions(options), reserveRight);

			Rect labelRect = Widgets.dropdownLabelRect(y, w, buttonW, reserveRight);

			Rect buttonRect = Widgets.dropdownButtonRect(y, w, buttonW, reserveRight);

			anchorX = RectMath.x(buttonRect) - MenuHost.m_blockOffset.x;

			Rect rowRect = Widgets.hoverRect(labelRect, buttonRect);

			bool rowHovered = Widgets.hoverRow(rowRect, tooltip);

			Widgets.fittedLabel(labelRect, Widgets.bold(label), Widgets.labelStyle(rowHovered), TextAnchor.MiddleLeft);
			return buttonRect;
		}

		private static string dropdownAnim(string id) => $"dd:{id}";
		private static float popupHeight(int rows) => rows * MenuHost.kDropdownItemH + Widgets.popupInsetY * 2f;
		private static float popupWidth(float anchorW, float contentW) => FastMath.max(anchorW, contentW + Widgets.popupInsetX * 2f);
		private static Rect popupRow(Rect popupRect, int index) => RectMath.create(RectMath.x(popupRect) + Widgets.popupInsetX, RectMath.y(popupRect) + Widgets.popupInsetY + index * MenuHost.kDropdownItemH, RectMath.w(popupRect) - Widgets.popupInsetX * 2f, MenuHost.kDropdownItemH);
		private static int popupRowAt(Rect popupRect, float mouseY) => LayoutMath.hitIndex(mouseY, RectMath.y(popupRect) + Widgets.popupInsetY, MenuHost.kDropdownItemH);

		private static void clearDropdown()
		{
			Widgets.m_dropdownId = null;
			Widgets.m_dropdownAnim = null;
			Widgets.m_dropdownOptions = null;
			Widgets.m_dropdownLabels = null;
			Widgets.m_dropdownResult = -1;
			Widgets.m_dropdownSelected = -1;
			Widgets.m_dropdownOpened = false;
		}

		private static void syncDropdown(float anchorX, float y, float buttonW, string[] options)
		{
			Widgets.m_dropdownAnchor = RectMath.create(anchorX, y, buttonW, MenuHost.kFieldH);
			Widgets.m_dropdownOffset = MenuHost.m_blockOffset;
			Widgets.m_dropdownOptions = options;
		}

		private static void openDropdown(string id, float anchorX, float y, float buttonW, string[] options, int selectedIndex)
		{
			Widgets.m_dropdownId = id;
			Widgets.m_dropdownAnim = Widgets.dropdownAnim(id);
			Widgets.syncDropdown(anchorX, y, buttonW, options);
			Widgets.m_dropdownLabels = Widgets.buildLabels(options);
			Widgets.m_dropdownResult = -1;
			Widgets.m_dropdownSelected = selectedIndex;
			Widgets.m_dropdownOpened = true;
			Widgets.m_dropdownOverlay = Widgets.m_overlayPass;

			MenuAnimation.noteOpened(Widgets.m_dropdownAnim);
		}

		private static string[] buildLabels(string[] options)
		{
			string[] labels = new string[options.Length];
			for (int i = 0; i < options.Length; i++)
				labels[i] = $"{DropdownLayout.kItemIndent}{options[i]}";
			return labels;
		}

		private static string dropdownCaption(string display) => Widgets.markup(Widgets.kDropdownCaptions, display, string.Empty, DropdownLayout.kCaret, false);
		private static string optionLabel(string[] options, int index) => index >= 0 && index < options.Length ? options[index] : DropdownLayout.kNoSelection;

		private static int dropdownButton(Rect buttonRect, string id, float anchorX, float y, float buttonW, string[] options, int selectedIndex)
		{
			bool open = Widgets.m_dropdownId == id;
			if (open)
				Widgets.syncDropdown(anchorX, y, buttonW, options);

			GUIStyle style = open ? Theme.dropdownOpen : Theme.dropdownButton;
			if (Widgets.scaledButton(buttonRect, Widgets.dropdownCaption(Widgets.optionLabel(options, selectedIndex)), style, MenuHost.scrollMouse))
			{
				if (open)
					Widgets.closeDropdown();
				else
					Widgets.openDropdown(id, anchorX, y, buttonW, options, selectedIndex);
			}

			if (Widgets.m_dropdownId != id || Widgets.m_dropdownResult < 0)
				return selectedIndex;

			int result = Widgets.m_dropdownResult;

			Widgets.closeDropdown();
			return result;
		}

		private static Rect dropdownRect(Rect viewRect, Vector2 scrollOffset)
		{
			float popupH = Widgets.popupHeight(Widgets.m_dropdownOptions.Length);

			float itemW = Widgets.measureItems(Widgets.m_dropdownOptions);

			float popupW = Widgets.popupWidth(RectMath.w(Widgets.m_dropdownAnchor), itemW);

			float popupX = RectMath.x(Widgets.m_dropdownAnchor) + Widgets.m_dropdownOffset.x - scrollOffset.x + RectMath.x(viewRect);

			float rawY = RectMath.yMax(Widgets.m_dropdownAnchor) + Widgets.m_dropdownOffset.y - scrollOffset.y + RectMath.y(viewRect) + DropdownLayout.kPopupOffsetY;

			Rect clamped = MenuHost.clampPopup(RectMath.create(popupX, rawY, popupW, popupH));
			Widgets.m_dropdownUp = RectMath.y(clamped) < rawY - DropdownLayout.kDropUpTolerance;
			return clamped;
		}

		private static void refreshWidths()
		{
			int currentVersion = Theme.layoutVersion;
			if (Widgets.m_widthsVersion == currentVersion)
				return;

			Widgets.kButtonWidths.Clear();

			Widgets.kItemWidths.Clear();

			Widgets.m_widthsVersion = currentVersion;
		}

		private static float measureOptions(string[] options)
		{
			Widgets.refreshWidths();

			if (Widgets.kButtonWidths.TryGetValue(options, out float cached))
				return cached;

			float widest = 0f;
			foreach (string option in options)
			{
				Widgets.kMeasureContent.text = $"{option}{DropdownLayout.kCaret}";

				float optionWidth = Theme.dropdownButton.CalcSize(Widgets.kMeasureContent).x;
				if (!(optionWidth > widest))
					continue;

				widest = optionWidth;
			}
			Widgets.kButtonWidths[options] = widest;
			return widest;
		}

		private static float measureItems(string[] options)
		{
			Widgets.refreshWidths();

			if (Widgets.kItemWidths.TryGetValue(options, out float cached))
				return cached;

			float widest = 0f;
			foreach (string option in options)
			{
				Widgets.kMeasureContent.text = $"{DropdownLayout.kItemIndent}{option}";

				float optionWidth = Theme.dropdownItem.CalcSize(Widgets.kMeasureContent).x;
				if (!(optionWidth > widest))
					continue;

				widest = optionWidth;
			}

			float result = widest + DropdownLayout.kPopupRightPad;
			Widgets.kItemWidths[options] = result;
			return result;
		}

		private static float buttonWidth(float w, float contentW, float reserveRight = 0f)
		{
			float available = w - reserveRight;

			float maxW = FastMath.max(MenuHost.kDropdownMaxW, available - DropdownLayout.kMinLabelW - DropdownLayout.kLabelGap);
			return FastMath.clamp(contentW, MenuHost.kDropdownMaxW, maxW);
		}

		private static Rect dropdownLabelRect(float y, float w, float buttonW, float reserveRight = 0f)
		{
			float offsetX = MenuHost.m_blockOffset.x;

			float offsetY = MenuHost.m_blockOffset.y;

			float available = w - reserveRight;

			float labelW = FastMath.max(DropdownLayout.kMinLabelW, available - buttonW - DropdownLayout.kLabelGap);
			return RectMath.create(offsetX, offsetY + y, labelW, MenuHost.kFieldH);
		}

		private static Rect dropdownButtonRect(float y, float w, float buttonW, float reserveRight = 0f)
		{
			float offsetX = MenuHost.m_blockOffset.x;

			float offsetY = MenuHost.m_blockOffset.y;
			return RectMath.create(offsetX + w - reserveRight - buttonW, offsetY + y, buttonW, MenuHost.kFieldH);
		}

		private static Rect hoverRect(Rect labelRect, Rect buttonRect) => RectMath.create(RectMath.x(labelRect), RectMath.y(labelRect), RectMath.xMax(buttonRect) - RectMath.x(labelRect), FastMath.max(RectMath.h(labelRect), RectMath.h(buttonRect)));
	}
}
