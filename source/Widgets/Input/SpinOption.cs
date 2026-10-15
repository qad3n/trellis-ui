
namespace Trellis
{
	internal static partial class Widgets
	{
		private static class SpinOptionText
		{
			internal const string kLabel = "Rotate";
			internal const string kTooltip = "Spin the marker around its axis.";
			internal const string kSpeedLabel = "Rotation Speed";
			internal const string kSpeedUnit = " turns/s";
		}

		internal static void drawSpinOption(ref float y, float w, ref bool enabled, ref float speed, float minSpeed, float maxSpeed, float step, string animId)
		{
			enabled = Widgets.drawToggle(ref y, w, enabled, SpinOptionText.kLabel, tooltip: SpinOptionText.kTooltip);

			MenuAnimation.SectionScope scope = MenuAnimation.beginSection(ref y, animId, enabled);
			if (scope.m_render)
				speed = Widgets.drawSliderPerSecond(ref y, w, SpinOptionText.kSpeedLabel, SpinOptionText.kSpeedUnit, speed, minSpeed, maxSpeed, step);
			MenuAnimation.endSection(ref y, scope);
		}
	}
}
