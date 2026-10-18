using System;
using UnityEngine;
using static Trellis.MenuHost;

namespace Trellis
{
	internal static partial class Widgets
	{
		internal const int kNoCap = -1;

		private static readonly EvictingCache<string, string> kPreviewMarkups = new EvictingCache<string, string>(Widgets.kMarkupCacheMax);

		private enum TextEntryKind
		{
			Text,
			Password,
			Area
		}

		private static class FocusLine
		{
			internal const float kH = 2f;
			internal const int kSalt = 0x0F0C;
			internal const float kEpsilon = 0.001f;
		}

		private const float kAreaH = MenuHost.kFieldH * 3f;

		private static class ApplyField
		{
			internal const float kApplyWidth = 60f;
			internal const float kRollWidth = 44f;
			internal const float kGap = 4f;
			internal const float kNoRoll = 0f;
			internal const string kApply = "Apply";
		}

		internal static string fixedTextField(Rect rect, string text, GUIStyle style, string controlName, int maxLength = Widgets.kNoCap, string hint = null) => Widgets.fixedEntry(rect, text, style, controlName, TextEntryKind.Text, default, maxLength, hint);
		internal static string fixedPasswordField(Rect rect, string text, GUIStyle style, string controlName, char maskChar) => Widgets.fixedEntry(rect, text, style, controlName, TextEntryKind.Password, maskChar, Widgets.kNoCap);
		internal static string fixedTextArea(Rect rect, string text, GUIStyle style, string controlName) => Widgets.fixedEntry(rect, text, style, controlName, TextEntryKind.Area, default, Widgets.kNoCap);

		internal static string drawTextField(ref float y, float w, string label, string value, bool disabled = false, string tooltip = null)
		{
			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, RowPrefix.kTextField, label);

			float offsetX = MenuHost.m_blockOffset.x;

			float offsetY = MenuHost.m_blockOffset.y;

			string controlName = Widgets.widgetId("tf", label);

			bool rowHovered = Widgets.rowHovered(offsetX, offsetY + y, w, disabled);

			if (!disabled && Gui.enabled)
				MenuHost.hoverTooltip(RectMath.create(offsetX, offsetY + y, w, MenuHost.kFieldH), tooltip);

			Widgets.beginDisabled(disabled);
			UiText.fittedLabel(RectMath.create(offsetX, offsetY + y, MenuHost.kLabelW, MenuHost.kFieldH), UiText.bold(label), Widgets.labelStyle(rowHovered), TextAnchor.MiddleLeft);

			string result = Widgets.fixedTextField(RectMath.create(offsetX + MenuHost.kLabelW, offsetY + y, w - MenuHost.kLabelW, MenuHost.kFieldH), value ?? string.Empty, Theme.textField, controlName);

			Widgets.endDisabled();

			y += MenuHost.kFieldH + Widgets.kFieldGap;
			Widgets.endRow(ref y, ref row);
			return result;
		}

		internal static string drawTextArea(ref float y, float w, string label, string value, string tooltip = null)
		{
			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, RowPrefix.kTextArea, label);

			float offsetX = MenuHost.m_blockOffset.x;

			float offsetY = MenuHost.m_blockOffset.y;

			string controlName = Widgets.widgetId("ta", label);

			Rect rowRect = RectMath.create(offsetX, offsetY + y, w, Widgets.kAreaH);

			bool rowHovered = Gui.enabled && RectMath.contains(rowRect, MenuHost.scrollMouse);
			if (Gui.enabled)
				MenuHost.hoverTooltip(rowRect, tooltip);

			UiText.fittedLabel(RectMath.create(offsetX, offsetY + y, MenuHost.kLabelW, MenuHost.kFieldH), UiText.bold(label), Widgets.labelStyle(rowHovered), TextAnchor.MiddleLeft);

			string result = Widgets.fixedTextArea(RectMath.create(offsetX + MenuHost.kLabelW, offsetY + y, w - MenuHost.kLabelW, Widgets.kAreaH), value ?? string.Empty, Theme.textArea, controlName);

			y += Widgets.kAreaH + Widgets.kFieldGap;
			Widgets.endRow(ref y, ref row);
			return result;
		}

		internal static string drawPasswordField(ref float y, float w, string label, string value, ref bool reveal, bool disabled = false)
		{
			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, RowPrefix.kPassword, label);

			float offsetX = MenuHost.m_blockOffset.x;

			float offsetY = MenuHost.m_blockOffset.y;

			string controlName = Widgets.widgetId("pw", label);

			const float kRevealGap = 4f;

			const float kRevealWidth = 46f;

			float fieldW = w - MenuHost.kLabelW - kRevealWidth - kRevealGap;

			bool rowHovered = Widgets.rowHovered(offsetX, offsetY + y, w, disabled);

			Widgets.beginDisabled(disabled);
			UiText.fittedLabel(RectMath.create(offsetX, offsetY + y, MenuHost.kLabelW, MenuHost.kFieldH), UiText.bold(label), Widgets.labelStyle(rowHovered), TextAnchor.MiddleLeft);

			Rect fieldRect = RectMath.create(offsetX + MenuHost.kLabelW, offsetY + y, fieldW, MenuHost.kFieldH);

			const char kMaskChar = '•';

			string result = reveal ? Widgets.fixedTextField(fieldRect, value ?? string.Empty, Theme.textField, controlName) : Widgets.fixedPasswordField(fieldRect, value ?? string.Empty, Theme.textField, controlName, kMaskChar);

			Widgets.endDisabled();

			if (Widgets.scaledButton(RectMath.create(offsetX + w - kRevealWidth, offsetY + y, kRevealWidth, MenuHost.kFieldH), reveal ? "Hide" : "Show", Theme.button, MenuHost.scrollMouse))
				reveal = !reveal;

			y += MenuHost.kFieldH + Widgets.kFieldGap;
			Widgets.endRow(ref y, ref row);
			return result;
		}

		internal static string drawApplyField(ref float y, float w, string label, string value, out bool applied) => Widgets.drawApply(ref y, w, label, value, ApplyField.kApply, false, out applied, out bool _);

		internal static string drawActionField(ref float y, float w, string label, string value, string action, out bool pressed, string tooltip = null) => Widgets.drawApply(ref y, w, label, value, action, false, out pressed, out bool _, tooltip);

		internal static string drawRollField(ref float y, float w, string label, string value, out bool applied, out bool rolled) => Widgets.drawApply(ref y, w, label, value, ApplyField.kApply, true, out applied, out rolled);

		private static void drawUnderline(Rect rect, string controlName)
		{
			bool focused = TextInputFocus.activeName == controlName;

			float focusT = MenuAnimation.hoverProgress(unchecked(controlName.GetHashCode() * Widgets.kKeyMix) ^ FocusLine.kSalt, focused);
			if (!Gui.repainting || focusT <= FocusLine.kEpsilon)
				return;

			BoxMetrics box = Theme.windowBox(rect);

			Rect inner = box.inner;

			float arc = box.innerRadius;

			float maxW = FastMath.max(0f, RectMath.w(inner) - arc * 2f);

			float lineW = maxW * focusT;

			Rect span = RectMath.create(RectMath.x(inner) + arc + (maxW - lineW) * 0.5f, RectMath.y(inner), lineW, RectMath.h(inner));
			Gui.drawStroke(Gui.bottomEdge(span, FocusLine.kH), Theme.accentBar);
		}

		private static bool rowHovered(float x, float screenY, float w, bool disabled) => !disabled && Gui.enabled && RectMath.contains(RectMath.create(x, screenY, w, MenuHost.kFieldH), MenuHost.scrollMouse);

		private static string fixedEntry(Rect rect, string text, GUIStyle style, string controlName, TextEntryKind kind, char maskChar, int maxLength, string hint = null)
		{
			string input = text ?? string.Empty;
			try
			{
				TextInputFocus.focusOwned(rect, controlName);

				string result = MenuTextEditor.draw(rect, input, style, controlName, kind == TextEntryKind.Password, maskChar, maxLength, kind == TextEntryKind.Area, hint);

				Widgets.drawUnderline(rect, controlName);
				if (kind == TextEntryKind.Text)
					Widgets.hoverPreview(rect, result, controlName);
				return result;
			}
			catch (Exception ex)
			{
				UiLog.logException($"Widgets.fixedEntry({controlName})", ex);
				return input;
			}
		}

		// the scroll mouse is offscreen outside the menu view and under popups
		private static void hoverPreview(Rect rect, string text, string controlName)
		{
			if (text.Length == 0 || !RectMath.contains(rect, MenuHost.scrollMouse))
				return;

			MenuHost.showPreview(Widgets.previewMarkup(text), controlName);
		}

		private static string previewMarkup(string text)
		{
			if (Widgets.kPreviewMarkups.tryGetValue(text, out string cached))
				return cached;

			// null when uncoloured so plain text never echoes itself in a tooltip
			string markup = RichText.fromGameHex(text);
			if (!RichText.hasColor(markup))
				markup = null;
			Widgets.kPreviewMarkups.set(text, markup);
			return markup;
		}

		private static string drawApply(ref float y, float w, string label, string value, string action, bool withRoll, out bool applied, out bool rolled, string tooltip = null)
		{
			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, RowPrefix.kTextField, label);

			float offsetX = MenuHost.m_blockOffset.x;

			float offsetY = MenuHost.m_blockOffset.y;

			string controlName = Widgets.widgetId("tfa", label);
			if (Gui.enabled)
				MenuHost.hoverTooltip(RectMath.create(offsetX, offsetY + y, w, MenuHost.kFieldH), tooltip);

			float rollSpan = withRoll ? ApplyField.kRollWidth + ApplyField.kGap : ApplyField.kNoRoll;

			float fieldW = w - MenuHost.kLabelW - ApplyField.kApplyWidth - ApplyField.kGap - rollSpan;

			UiText.fittedLabel(RectMath.create(offsetX, offsetY + y, MenuHost.kLabelW, MenuHost.kFieldH), UiText.bold(label), Widgets.labelStyle(Widgets.rowHovered(offsetX, offsetY + y, w, false)), TextAnchor.MiddleLeft);

			string result = Widgets.fixedTextField(RectMath.create(offsetX + MenuHost.kLabelW, offsetY + y, fieldW, MenuHost.kFieldH), value ?? string.Empty, Theme.textField, controlName);

			float applyX = offsetX + w - ApplyField.kApplyWidth;
			rolled = withRoll && Widgets.scaledButton(RectMath.create(applyX - ApplyField.kGap - ApplyField.kRollWidth, offsetY + y, ApplyField.kRollWidth, MenuHost.kFieldH), "Roll", Theme.button, MenuHost.scrollMouse);

			applied = Widgets.scaledButton(RectMath.create(applyX, offsetY + y, ApplyField.kApplyWidth, MenuHost.kFieldH), action, Theme.button, MenuHost.scrollMouse);

			y += MenuHost.kFieldH + Widgets.kFieldGap;
			Widgets.endRow(ref y, ref row);
			return result;
		}
	}
}
