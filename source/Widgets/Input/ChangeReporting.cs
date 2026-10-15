namespace Trellis
{
	internal static partial class Widgets
	{
		internal static bool toggleChanged(ref float y, float w, ref bool value, string label, string tooltip = null, bool disabled = false)
		{
			bool prev = value;

			value = Widgets.drawToggle(ref y, w, value, label, tooltip: tooltip, disabled: disabled);
			return value != prev;
		}

		internal static bool sliderChanged(ref float y, float w, string label, ref float value, float min, float max, float step = -1f)
		{
			float prev = value;

			value = Widgets.drawSlider(ref y, w, label, value, min, max, step);
			return !FastMath.approximately(prev, value);
		}

		internal static bool sliderChanged(ref float y, float w, string label, ref int value, float min, float max, float step = 1f)
		{
			int prev = value;

			value = Widgets.drawSliderInt(ref y, w, label, value, min, max, step: step);
			return value != prev;
		}

		internal static bool dropdownChanged(ref float y, float w, string label, string[] options, ref int index, string tooltip = null)
		{
			int prev = index;

			index = Widgets.drawDropdown(ref y, w, label, options, index, tooltip: tooltip);
			return index != prev;
		}
	}
}
