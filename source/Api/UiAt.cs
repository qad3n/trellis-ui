using System;
using UnityEngine;

namespace Trellis
{
	// widgets placed at a rect the caller owns, for overlays, popups and panels
	internal static class UiAt
	{
		internal static float barHeight => Widgets.barHeight;
		internal static float fieldGap => Widgets.kFieldGap;
		internal static float subTabBarHeight => Widgets.kSubTabBarH;
		internal static float rowEndReserve => Widgets.kRowEndReserve;
		internal const int kThirdButton = Widgets.kThirdButton;
		// the mouse in list row space for controls drawn inside a list row
		internal static Vector2 listMouse => Widgets.listMouse;

		internal static float toggleWidth(string label) => Widgets.toggleWidth(label);
		internal static float dropdownWidth(string label, string[] options) => Widgets.dropdownRowWidth(label, options);
		internal static float infoRowHeight(float w, string value) => Widgets.infoRowHeight(w, value);

		internal static bool toggle(float x, float y, float w, bool value, string label, string animId, string tooltip = null, bool disabled = false) => Widgets.drawToggleAt(x, y, w, value, label, animId, tooltip, disabled);
		internal static int dropdown(float x, float y, float w, string id, string label, string[] options, int selectedIndex, string tooltip = null) => Widgets.drawDropdownAt(x, y, w, id, label, options, selectedIndex, tooltip);
		internal static void readDropdown(Rect viewRect, Vector2 scrollOffset) => Widgets.readDropdown(viewRect, scrollOffset);
		internal static void dropdownPopup(Rect viewRect, Vector2 scrollOffset) => Widgets.drawDropdownPopup(viewRect, scrollOffset);
		internal static void infoRow(ref float y, float x, float w, string label, string value) => Widgets.drawInfoRowAt(ref y, x, w, label, value);
		internal static void subHeader(ref float y, float x, float w, string text, string tooltip = null) => Widgets.drawSubHeaderAt(ref y, x, w, text, tooltip);
		internal static void sectionHeader(Rect rect, string text, string tooltip = null, string trailing = null) => Widgets.drawSectionHeaderAt(rect, text, tooltip, trailing);
		internal static bool collapsibleHeader(Rect rect, string animId, string text, bool expanded) => Widgets.drawCollapsibleHeader(rect, animId, text, expanded);
		internal static void countLine(ref float y, float x, float w, int count, string noun, string innerSuffix = "", string trailing = "", TextAnchor anchor = TextAnchor.MiddleLeft) => Widgets.drawCountLine(ref y, x, w, count, noun, innerSuffix, trailing, anchor);
		internal static int subTabs(float x, float y, float w, string[] names, int selected, string animKey = null) => Widgets.drawSubTabsFixed(x, y, w, names, selected, animKey);

		internal static void emptyList(float x, float y, float w, float listH, string message) => Widgets.drawEmptyList(x, y, w, listH, message);
		internal static void emptyState(Rect area, string title, string message = null) => Widgets.drawEmptyState(area, title, message);
		internal static bool emptyStateAction(Rect area, string title, string message, string actionLabel) => Widgets.drawEmptyStateAction(area, title, message, actionLabel);
		internal static void progress(Rect area, float ratio, bool indeterminate) => Widgets.drawProgressBar(area, ratio, indeterminate);
		internal static void marker(in MarkerFrame frame, Texture texture) => Widgets.drawMarker(frame, texture);
		internal static void marker(in MarkerFrame frame, Vector2 offset, Texture texture) => Widgets.drawMarker(frame, offset, texture);
		internal static void banner(float x, float y, float width, string title, string detail, Color accent, out float height) => Widgets.statusBanner(x, y, width, title, detail, accent, out height);
		internal static void reveal(Rect area, bool interactive, Action<float> drawBody) => Widgets.drawReveal(area, interactive, drawBody);

		internal static bool button(Rect rect, in ActionButton button) => Widgets.drawActionButtonAt(rect, button);
		internal static bool button(Rect rect, string text, GUIStyle style) => Widgets.scrollButton(rect, text, style);
		internal static bool scaledButton(Rect rect, string text, GUIStyle style, Vector2 mouse) => Widgets.scaledButton(rect, text, style, mouse);
		internal static int buttonPair(float x, float y, float w, float buttonH, float gap, in ActionButton left, in ActionButton right) => Widgets.drawButtonPairAt(x, y, w, buttonH, gap, left, right);
		internal static int buttonTriplet(float x, float y, float w, float buttonH, float gap, in ActionButton left, in ActionButton middle, in ActionButton right) => Widgets.drawButtonTripletAt(x, y, w, buttonH, gap, left, middle, right);
		internal static bool rowEndButton(float rowY, float w, string label, string tooltip) => Widgets.drawRowEndButton(rowY, w, label, tooltip);

		internal static string textField(Rect rect, string text, GUIStyle style, string controlName, int maxLength = Widgets.kNoCap, string hint = null) => Widgets.fixedTextField(rect, text, style, controlName, maxLength, hint);
		internal static string textArea(Rect rect, string text, GUIStyle style, string controlName) => Widgets.fixedTextArea(rect, text, style, controlName);
		internal static string password(Rect rect, string text, GUIStyle style, string controlName, char maskChar) => Widgets.fixedPasswordField(rect, text, style, controlName, maskChar);

		internal static void box(Rect rect, Color fill, Color border) => Widgets.drawBox(rect, fill, border);
		internal static void highlight(Rect rowRect, float accentWidth = 0f) => Widgets.drawHighlight(rowRect, accentWidth);
		internal static void rowAccent(Rect rowRect, Color accentColor) => Widgets.drawRowAccent(rowRect, accentColor);
		internal static void rowAccent(Rect rowRect, Color accentColor, float tintAlpha) => Widgets.drawRowAccent(rowRect, accentColor, tintAlpha);
		internal static void itemBg(Rect itemRect, int hoverKey, bool hovered, bool selected) => Widgets.drawItemBg(itemRect, hoverKey, hovered, selected);
		internal static void rowBg(Rect rowRect, int hoverKey, bool hovered, bool selected) => Widgets.drawRowBg(rowRect, hoverKey, hovered, selected);
		internal static void cardBg(Rect cardRect, Color accentColor) => Widgets.drawCardBg(cardRect, accentColor);
		internal static void scrim(float screenW, float screenH, float openAlpha) => Widgets.drawScrim(screenW, screenH, openAlpha);
		internal static void faded(Rect rect, Texture texture, float alpha) => Widgets.drawFaded(rect, texture, alpha);
		internal static Rect popupBand(Rect popupRect, float headerH, float accentH)
		{
			Theme.drawPopupBand(popupRect, headerH, accentH);
			return RectMath.create(RectMath.x(popupRect), RectMath.y(popupRect), RectMath.w(popupRect), headerH);
		}
		internal static void popupTitle(Rect headerRect, float pad, float badgesLeft, string title, GUIStyle titleStyle) => Widgets.drawPopupTitle(headerRect, pad, badgesLeft, title, titleStyle);
	}
}
