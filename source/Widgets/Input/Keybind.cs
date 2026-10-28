using System;
using System.Collections.Generic;
using UnityEngine;
using static Trellis.MenuHost;

namespace Trellis
{
	internal static partial class Widgets
	{
		private const float kKeybindPadX = 12f;
		private const float kKeybindMinW = 80f;
		private const string kKeybindCapturing = "<press a key>";
		internal const string kKeybindTooltip = "Bind a key, mouse button or combo like Ctrl + I. Right click to clear.";
		private const float kKeybindInlineGap = 10f;
		private const float kKeybindConflictAlpha = 0.22f;

		// the host names every bind sharing a chord so both key caps warn wherever they are drawn
		internal static Func<KeyChord, string> m_keyConflict;

		// a chord label allocates every draw
		private static readonly Dictionary<KeyChord, string> kChordNames = new Dictionary<KeyChord, string>();

		// keys held so far in a capture which commits when the first of them is released
		private static readonly KeyCode[] kCaptureKeys = new KeyCode[KeyChord.kMaxKeys];

		private static string m_keybindId;
		private static int m_captureCount;
		private static string m_captureLabel = Widgets.kKeybindCapturing;

		private static class KeybindCapture
		{
			internal const float kAlpha = 0.16f;
			internal const int kSalt = 0x4B0D;
			internal const float kEpsilon = 0.001f;
		}

		// a key pressed to bind must not fire its current bind
		internal static bool capturingKeybind => Widgets.m_keybindId != null;
		internal static void closeKeybind() => Widgets.armKeybind(null);

		internal static KeyChord drawKeybind(ref float y, float w, string label, KeyChord value, string tooltip = null, bool disabled = false)
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
			UiText.fittedLabel(labelRect, UiText.bold(label), Widgets.labelStyle(rowHovered), TextAnchor.MiddleLeft);

			MenuHost.hoverTooltip(rowRect, tooltip);

			value = Widgets.keybindButton(Widgets.dropdownButtonRect(y, w, buttonW), id, label, value, disabled);

			Widgets.endDisabled();

			y += MenuHost.kFieldH + Widgets.kFieldGap;
			Widgets.endRow(ref y, ref row);
			return value;
		}

		// the key stays editable while the feature is off so it can be bound ahead
		internal static bool drawToggleKeybind(ref float y, float w, bool value, string label, ref KeyChord key, string tooltip = null, string keyTooltip = Widgets.kKeybindTooltip)
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

		internal static bool drawButtonKeybind(ref float y, float w, in ActionButton button, ref KeyChord key, string keyTooltip = Widgets.kKeybindTooltip)
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

		private static float keybindWidth(string id, KeyChord value, float w)
		{
			float contentW = UiText.measuredWidth(Theme.dropdownButton, Widgets.keybindDisplay(id, value)) + Widgets.kKeybindPadX;
			return Widgets.buttonWidth(w, FastMath.max(Widgets.kKeybindMinW, contentW));
		}

		private static string keybindDisplay(string id, KeyChord value) => Widgets.m_keybindId == id ? Widgets.m_captureLabel : Widgets.formatKeybind(value);

		private static KeyChord keybindButton(Rect buttonRect, string id, string label, KeyChord value, bool disabled)
		{
			bool capturing = Widgets.m_keybindId == id;

			GUIStyle style = capturing ? Theme.keybindCapture : Theme.dropdownButton;

			bool wasCapturing = capturing;

			if (Widgets.scaledButton(buttonRect, Widgets.keybindDisplay(id, value), style, MenuHost.scrollMouse))
			{
				Widgets.armKeybind(capturing ? null : id);

				capturing = Widgets.m_keybindId == id;
			}
			Widgets.drawWash(buttonRect, id, capturing);

			if (!capturing)
				Widgets.drawConflict(buttonRect, value);

			if (!disabled && MenuHost.mouseDown1 && RectMath.contains(buttonRect, MenuHost.scrollMouse))
			{
				value = default;

				Widgets.armKeybind(null);

				capturing = false;
			}

			if (capturing && Widgets.captureKeyEvent())
				value = Widgets.commitKeybind(label);

			capturing = Widgets.m_keybindId == id;

			if (wasCapturing && capturing && MenuHost.extraButton >= MenuHost.kFirstExtra)
			{
				Widgets.holdCaptureKey(KeyCode.Mouse0 + MenuHost.extraButton);

				value = Widgets.commitKeybind(label);
			}

			// an armed capture off screen eats the next key meant for the game
			if (wasCapturing && capturing && MenuHost.rawMouseDown0 && !RectMath.contains(buttonRect, MenuHost.scrollMouse))
				Widgets.armKeybind(null);
			return value;
		}

		// true once the first held key is released and the capture is ready to commit
		private static bool captureKeyEvent()
		{
			Event current = Event.current;

			EventType type = current.type;
			if (type != EventType.KeyDown && type != EventType.KeyUp)
				return false;

			KeyCode key = current.keyCode;

			current.Use();

			if (type == EventType.KeyUp)
				return Array.IndexOf(Widgets.kCaptureKeys, key, 0, Widgets.m_captureCount) >= 0;

			if (key == KeyCode.Escape)
				Widgets.armKeybind(null);
			else if (key != KeyCode.None)
				Widgets.holdCaptureKey(key);
			return false;
		}

		private static void holdCaptureKey(KeyCode key)
		{
			if (Widgets.m_captureCount == KeyChord.kMaxKeys || Array.IndexOf(Widgets.kCaptureKeys, key, 0, Widgets.m_captureCount) >= 0)
				return;

			Widgets.kCaptureKeys[Widgets.m_captureCount++] = key;
			Widgets.m_captureLabel = Widgets.capturedChord().ToString();
		}

		private static KeyChord commitKeybind(string label)
		{
			KeyChord chord = Widgets.capturedChord();

			Widgets.armKeybind(null);

			UiLog.debug($"[Widgets.commitKeybind] {label} = {Widgets.formatKeybind(chord)}");
			return chord;
		}

		private static KeyChord capturedChord() => new KeyChord(Widgets.kCaptureKeys, Widgets.m_captureCount);

		private static void armKeybind(string id)
		{
			Widgets.m_keybindId = id;
			Widgets.m_captureCount = 0;
			Widgets.m_captureLabel = Widgets.kKeybindCapturing;
		}

		private static void drawWash(Rect buttonRect, string id, bool capturing)
		{
			float captureT = MenuAnimation.hoverProgress(unchecked(id.GetHashCode() * Widgets.kKeyMix) ^ KeybindCapture.kSalt, capturing);
			if (!Gui.repainting || captureT <= KeybindCapture.kEpsilon)
				return;

			Theme.drawRoundedTint(buttonRect, ColorMath.withAlpha(Theme.m_accent, KeybindCapture.kAlpha * captureT));
		}

		// drawn after the row tooltip so the conflict owns the hover
		private static void drawConflict(Rect buttonRect, KeyChord value)
		{
			string conflict = Widgets.m_keyConflict?.Invoke(value);
			if (conflict == null)
				return;

			if (Gui.repainting)
				Theme.drawRoundedTint(buttonRect, ColorMath.withAlpha(Theme.Status.m_gold, Widgets.kKeybindConflictAlpha));
			MenuHost.hoverTooltip(buttonRect, conflict);
		}

		private static string formatKeybind(KeyChord chord)
		{
			if (Widgets.kChordNames.TryGetValue(chord, out string cached))
				return cached;

			string name = chord.ToString();
			Widgets.kChordNames[chord] = name;
			return name;
		}
	}
}
