using System.Collections.Generic;
using UnityEngine;
using static Trellis.MenuHost;

namespace Trellis
{
	internal static partial class Widgets
	{
		private const float kKeybindPadX = 12f;
		private const float kKeybindMinW = 80f;
		private const string kKeybindUnbound = "None";
		private const string kKeybindCapturing = "<press a key>";
		private const string kKeybindTooltip = "Bind any key or mouse button. Right click to clear.";
		private const float kKeybindInlineGap = 10f;

		// a KeyCode.ToString call boxes and allocates every draw
		private static readonly Dictionary<KeyCode, string> kKeyNames = new Dictionary<KeyCode, string>();

		private static string m_keybindId;

		private static class KeybindCapture
		{
			internal const float kAlpha = 0.16f;
			internal const int kSalt = 0x4B0D;
			internal const float kEpsilon = 0.001f;
		}

		// a key pressed to bind must not fire its current bind
		internal static bool capturingKeybind => Widgets.m_keybindId != null;
		internal static void closeKeybind() => Widgets.m_keybindId = null;

		internal static KeyCode drawKeybind(ref float y, float w, string label, KeyCode value, string tooltip = null, bool disabled = false)
		{
			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, RowPrefix.kKeybind, label);

			float offsetX = MenuHost.m_blockOffset.x;

			float offsetY = MenuHost.m_blockOffset.y;

			string id = Widgets.widgetId("kb", label);

			float buttonW = Widgets.keybindWidth(id, value, w);

			Widgets.beginDisabled(disabled);

			Rect rowRect = RectMath.create(offsetX, offsetY + y, w, MenuHost.kFieldH);

			bool rowHovered = Gui.enabled && RectMath.contains(rowRect, MenuHost.scrollMouse);

			Rect labelRect = Widgets.dropdownLabelRect(y, w, buttonW);
			Widgets.fittedLabel(labelRect, Widgets.bold(label), Widgets.labelStyle(rowHovered), TextAnchor.MiddleLeft);

			MenuHost.hoverTooltip(rowRect, tooltip);

			value = Widgets.keybindButton(Widgets.dropdownButtonRect(y, w, buttonW), id, label, value, disabled);

			Widgets.endDisabled();

			y += MenuHost.kFieldH + Widgets.kFieldGap;
			Widgets.endRow(ref y, ref row);
			return value;
		}

		// the key stays editable while the feature is off so it can be bound ahead
		internal static bool drawToggleKeybind(ref float y, float w, bool value, string label, ref KeyCode key, string tooltip = null, string keyTooltip = Widgets.kKeybindTooltip)
		{
			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, RowPrefix.kToggle, label);

			float offsetX = MenuHost.m_blockOffset.x;

			float offsetY = MenuHost.m_blockOffset.y;

			string id = Widgets.widgetId("kb", label);

			float buttonW = Widgets.keybindWidth(id, key, w);

			float preY = y;

			value = Widgets.drawToggle(ref y, w, value, label, buttonW + Widgets.kKeybindInlineGap, tooltip);

			Rect buttonRect = RectMath.create(offsetX + w - buttonW, offsetY + preY, buttonW, MenuHost.kFieldH);
			MenuHost.hoverTooltip(buttonRect, keyTooltip);
			key = Widgets.keybindButton(buttonRect, id, label, key, false);

			Widgets.endRow(ref y, ref row);
			return value;
		}

		internal static bool drawButtonKeybind(ref float y, float w, in ActionButton button, ref KeyCode key, string keyTooltip = Widgets.kKeybindTooltip)
		{
			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, RowPrefix.kButton, null);

			float offsetX = MenuHost.m_blockOffset.x;

			float offsetY = MenuHost.m_blockOffset.y;

			string id = Widgets.widgetId("kb", button.m_label);

			float buttonW = Widgets.keybindWidth(id, key, w);

			bool clicked = Widgets.drawActionButtonAt(RectMath.create(offsetX, offsetY + y, w - buttonW - Widgets.kKeybindInlineGap, ButtonLayout.kHeight), button);

			Rect keyRect = RectMath.create(offsetX + w - buttonW, offsetY + y, buttonW, ButtonLayout.kHeight);
			MenuHost.hoverTooltip(keyRect, keyTooltip);
			key = Widgets.keybindButton(keyRect, id, button.m_label, key, false);

			Widgets.drawSpacer(ref y);
			Widgets.endRow(ref y, ref row);
			return clicked;
		}

		private static float keybindWidth(string id, KeyCode value, float w)
		{
			string display = Widgets.m_keybindId == id ? Widgets.kKeybindCapturing : Widgets.formatKeybind(value);

			float contentW = Widgets.measuredWidth(Theme.dropdownButton, display) + Widgets.kKeybindPadX;
			return Widgets.buttonWidth(w, FastMath.max(Widgets.kKeybindMinW, contentW));
		}

		private static KeyCode keybindButton(Rect buttonRect, string id, string label, KeyCode value, bool disabled)
		{
			bool capturing = Widgets.m_keybindId == id;

			string display = capturing ? Widgets.kKeybindCapturing : Widgets.formatKeybind(value);

			GUIStyle style = capturing ? Theme.dropdownOpen : Theme.dropdownButton;

			bool wasCapturing = capturing;

			if (Widgets.scaledButton(buttonRect, display, style, MenuHost.scrollMouse))
			{
				Widgets.m_keybindId = capturing ? null : id;

				capturing = Widgets.m_keybindId == id;
			}
			Widgets.drawWash(buttonRect, id, capturing);

			if (!disabled && MenuHost.mouseDown1 && RectMath.contains(buttonRect, MenuHost.scrollMouse))
			{
				value = KeyCode.None;

				Widgets.m_keybindId = null;

				capturing = false;
			}

			if (capturing && Event.current.type == EventType.KeyDown)
			{
				KeyCode pressed = Event.current.keyCode;
				if (pressed == KeyCode.Escape)
					Widgets.m_keybindId = null;
				else if (pressed != KeyCode.None)
				{
					value = pressed;

					Widgets.m_keybindId = null;

					UiLog.debug($"[Widgets.keybindButton] {label} = {Widgets.formatKeybind(value)}");
				}

				Event.current.Use();
			}

			if (wasCapturing && capturing && MenuHost.extraButton >= MenuHost.kFirstExtra)
			{
				value = KeyCode.Mouse0 + MenuHost.extraButton;

				Widgets.m_keybindId = null;

				UiLog.debug($"[Widgets.keybindButton] {label} = {Widgets.formatKeybind(value)}");
			}

			// an armed capture off screen eats the next key meant for the game
			if (wasCapturing && capturing && MenuHost.rawMouseDown0 && !RectMath.contains(buttonRect, MenuHost.scrollMouse))
				Widgets.m_keybindId = null;
			return value;
		}

		private static void drawWash(Rect buttonRect, string id, bool capturing)
		{
			float captureT = MenuAnimation.hoverProgress(unchecked(id.GetHashCode() * Widgets.kKeyMix) ^ KeybindCapture.kSalt, capturing);
			if (!Gui.repainting || captureT <= KeybindCapture.kEpsilon)
				return;

			Theme.drawRoundedTint(buttonRect, ColorMath.withAlpha(Theme.m_accent, KeybindCapture.kAlpha * captureT));
		}

		private static string formatKeybind(KeyCode key)
		{
			if (key == KeyCode.None)
				return Widgets.kKeybindUnbound;
			if (Widgets.kKeyNames.TryGetValue(key, out string cached))
				return cached;

			string name = key.ToString();
			Widgets.kKeyNames[key] = name;
			return name;
		}
	}
}
