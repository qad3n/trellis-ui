namespace Trellis
{
	internal static partial class Widgets
	{
		internal static class ButtonLayout
		{
			internal const float kHeight = 28f;
			internal const float kGap = 8f;
		}

		private static void drawSpacer(ref float y, float buttonH = ButtonLayout.kHeight) => y += buttonH + ButtonLayout.kGap;
	}
}
