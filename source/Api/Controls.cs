using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Trellis
{
	// every widget takes the value by ref and returns true on the frame it changes
	internal static partial class Ui
	{
		internal static bool toggle(ref bool value, string label, string tooltip = null, bool disabled = false, float reserveRight = 0f) => Ui.set(ref value, Widgets.drawToggle(ref Ui.m_y, Ui.m_w, value, label, reserveRight, tooltip, disabled));
		internal static bool toggleKey(ref bool value, string label, ref KeyChord key, string tooltip = null, string keyTooltip = Widgets.kKeybindTooltip) => Ui.set(ref value, Widgets.drawToggleKeybind(ref Ui.m_y, Ui.m_w, value, label, ref key, tooltip, keyTooltip));
		internal static bool toggleColor(ref bool value, string label, string pickerId, ref Color color, string tooltip = null) => Ui.set(ref value, Widgets.drawToggleColor(ref Ui.m_y, Ui.m_w, value, label, pickerId, ref color, tooltip));
		internal static bool toggleDropdown(ref bool value, string label, string[] options, ref int index, string tooltip = null) => Ui.set(ref value, Widgets.drawToggleDropdown(ref Ui.m_y, Ui.m_w, value, label, options, ref index, tooltip));
		internal static bool toggleDropdownColor(ref bool value, string label, string pickerId, ref Color color, string[] options, ref int index, string tooltip = null) => Ui.set(ref value, Widgets.drawToggleDropdownColor(ref Ui.m_y, Ui.m_w, value, label, pickerId, ref color, options, ref index, tooltip));
		internal static bool toggleDropdownColors(ref bool value, string label, string pickerId, ref Color color, string secondaryId, ref Color secondaryColor, string[] options, ref int index, string tooltip = null) => Ui.set(ref value, Widgets.drawToggleDropdownColorPair(ref Ui.m_y, Ui.m_w, value, label, pickerId, ref color, secondaryId, ref secondaryColor, options, ref index, tooltip));
		internal static bool toggleMultiSelect(ref bool value, string label, string[] options, ref bool[] selected, string tooltip = null, bool requireOne = false, MultiSelectItemHint[] hints = null) => Ui.set(ref value, Widgets.drawToggleMultiSelect(ref Ui.m_y, Ui.m_w, value, label, options, ref selected, tooltip, requireOne, hints));
		internal static bool toggleMultiSelectColor(ref bool value, string label, string pickerId, ref Color color, string[] options, ref bool[] selected, string tooltip = null, bool requireOne = false, MultiSelectItemHint[] hints = null) => Ui.set(ref value, Widgets.drawToggleMultiSelectColor(ref Ui.m_y, Ui.m_w, value, label, pickerId, ref color, options, ref selected, tooltip, requireOne, hints));
		internal static bool toggleCycle(ref bool value, string label, string[] options, int[] states, string[] stateLabels, string tooltip = null) => Ui.set(ref value, Widgets.drawToggleCycle(ref Ui.m_y, Ui.m_w, value, label, options, states, stateLabels, tooltip));
		internal static bool cycle(string label, string display, string tooltip = null) => Widgets.drawCycleRow(ref Ui.m_y, Ui.m_w, label, display, tooltip);

		internal static bool slider(ref float value, string label, float min, float max, float step = -1f)
		{
			float prev = value;

			value = Widgets.drawSlider(ref Ui.m_y, Ui.m_w, label, value, min, max, step);
			return !FastMath.approximately(prev, value);
		}

		internal static bool slider(ref int value, string label, float min, float max, string tooltip = null, float step = Widgets.kIntStep, string unit = null) => Ui.set(ref value, Widgets.drawSliderInt(ref Ui.m_y, Ui.m_w, label, value, min, max, tooltip, step, unit));
		internal static bool sliderPx(ref float value, string label, float min, float max, float step = -1f) => Ui.changed(ref value, Widgets.drawSliderPx(ref Ui.m_y, Ui.m_w, label, value, min, max, step));
		internal static bool sliderMeters(ref float value, string label, float min, float max, float step = -1f) => Ui.changed(ref value, Widgets.drawSliderMeters(ref Ui.m_y, Ui.m_w, label, value, min, max, step));
		internal static bool sliderPercent(ref float value, string label, float min, float max, float step, string tooltip = null) => Ui.changed(ref value, Widgets.drawSliderPercent(ref Ui.m_y, Ui.m_w, label, value, min, max, step, tooltip));
		internal static bool sliderPerSecond(ref float value, string label, string unit, float min, float max, float step = -1f) => Ui.changed(ref value, Widgets.drawSliderPerSecond(ref Ui.m_y, Ui.m_w, label, unit, value, min, max, step));
		internal static bool sliderUnits(ref float value, string label, string unit, float min, float max, float step = -1f) => Ui.changed(ref value, Widgets.drawSliderUnits(ref Ui.m_y, Ui.m_w, label, unit, value, min, max, step));
		internal static bool sliderDistance(ref float value, string label, float min, float max, float step = 50f) => Ui.changed(ref value, Widgets.drawDistanceSlider(ref Ui.m_y, Ui.m_w, label, value, min, max, step));
		internal static bool sliderFull(ref float value, string label, string display, float min, float max, float step, string tooltip = null) => Ui.changed(ref value, Widgets.drawSliderFull(ref Ui.m_y, Ui.m_w, label, display, value, min, max, step, tooltip));

		internal static bool dropdown(ref int index, string label, string[] options, string tooltip = null, float reserveRight = 0f) => Ui.set(ref index, Widgets.drawDropdown(ref Ui.m_y, Ui.m_w, label, options, index, reserveRight, tooltip));

		// the enum must count up from zero in the same order as the option labels
		internal static bool dropdown<TEnum>(ref TEnum value, string label, string[] options, string tooltip = null) where TEnum : unmanaged, Enum
		{
			int index = MenuHost.key(value);
			if (!Ui.dropdown(ref index, label, options, tooltip))
				return false;

			value = Unsafe.SizeOf<TEnum>() == sizeof(int) ? Unsafe.As<int, TEnum>(ref index) : (TEnum)Enum.ToObject(typeof(TEnum), index);
			return true;
		}

		// the array is read and written in place so keep one per widget
		internal static bool[] multiSelect(string label, string[] options, bool[] selected, bool requireOne = false, string tooltip = null, MultiSelectItemHint[] hints = null, float reserveRight = 0f) => Widgets.drawMultiSelect(ref Ui.m_y, Ui.m_w, label, options, selected, reserveRight, requireOne, tooltip, hints);

		internal static bool keybind(ref KeyChord key, string label, string tooltip = null) => Ui.set(ref key, Widgets.drawKeybind(ref Ui.m_y, Ui.m_w, label, key, tooltip));
		internal static bool textField(ref string value, string label, string tooltip = null, bool disabled = false) => Ui.set(ref value, Widgets.drawTextField(ref Ui.m_y, Ui.m_w, label, value, disabled, tooltip));
		internal static bool textArea(ref string value, string label, string tooltip = null) => Ui.set(ref value, Widgets.drawTextArea(ref Ui.m_y, Ui.m_w, label, value, tooltip));
		internal static bool password(ref string value, string label, ref bool reveal, bool disabled = false) => Ui.set(ref value, Widgets.drawPasswordField(ref Ui.m_y, Ui.m_w, label, value, ref reveal, disabled));
		internal static bool actionField(ref string value, string label, string action, out bool pressed, string tooltip = null) => Ui.set(ref value, Widgets.drawActionField(ref Ui.m_y, Ui.m_w, label, value, action, out pressed, tooltip));
		internal static bool applyField(ref string value, string label, out bool applied) => Ui.set(ref value, Widgets.drawApplyField(ref Ui.m_y, Ui.m_w, label, value, out applied));
		internal static bool rollField(ref string value, string label, out bool applied, out bool rolled) => Ui.set(ref value, Widgets.drawRollField(ref Ui.m_y, Ui.m_w, label, value, out applied, out rolled));

		internal static void spin(ref bool enabled, ref float speed, float minSpeed, float maxSpeed, float step, string animId) => Widgets.drawSpinOption(ref Ui.m_y, Ui.m_w, ref enabled, ref speed, minSpeed, maxSpeed, step, animId);

		internal static void button(string label, Action onClick, string tooltip = null) => Widgets.drawButton(ref Ui.m_y, Ui.m_w, label, Theme.button, onClick, tooltip);
		internal static void button(string label, GUIStyle style, Action onClick, string tooltip = null) => Widgets.drawButton(ref Ui.m_y, Ui.m_w, label, style, onClick, tooltip);
		internal static bool buttonKey(in ActionButton button, ref KeyChord key, string keyTooltip = Widgets.kKeybindTooltip) => Widgets.drawButtonKeybind(ref Ui.m_y, Ui.m_w, button, ref key, keyTooltip);
		internal static bool buttonKey(string label, Action onClick, ref KeyChord key, bool disabled = false, string tooltip = null, string keyTooltip = Widgets.kKeybindTooltip) => Widgets.drawButtonKeybind(ref Ui.m_y, Ui.m_w, new ActionButton(label, Theme.button, onClick, disabled, tooltip), ref key, keyTooltip);
		internal static bool buttonRow(in ActionButton only) => Widgets.drawButtonRow(ref Ui.m_y, Ui.m_w, only);
		internal static int buttonRow(in ActionButton left, in ActionButton right) => Widgets.drawButtonRow(ref Ui.m_y, Ui.m_w, left, right);
		internal static int buttonRow(in ActionButton left, in ActionButton middle, in ActionButton right) => Widgets.drawButtonRow(ref Ui.m_y, Ui.m_w, left, middle, right);

		internal static int listPane(float maxH, ref ScrollState scroll, int count, Func<int, float> rowHeight, Action<int, Rect, float> drawRow, out int hovered) => Widgets.drawListPane(ref Ui.m_y, Ui.m_w, maxH, ref scroll, count, rowHeight, drawRow, out hovered);
		internal static int selectList(ref ScrollState scroll, int count, int selected, Func<int, SelectRowItem> items, string[] chips, bool compact, out int toggled) => Widgets.drawSelectList(ref Ui.m_y, Ui.m_w, ref scroll, count, selected, items, chips, compact, out toggled);

		private static bool changed(ref float value, float next)
		{
			if (FastMath.approximately(value, next))
				return false;

			value = next;
			return true;
		}
	}
}
