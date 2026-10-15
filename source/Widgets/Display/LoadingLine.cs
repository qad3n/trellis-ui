using UnityEngine;
using static Trellis.MenuHost;

namespace Trellis
{
	internal static partial class Widgets
	{
		private const float kLoadingH = 20f;
		private const float kLoadingGap = Widgets.kFieldGap;
		private const string kLoadingText = "working";

		private static class LoadingDots
		{
			internal const int kCount = 3;
			internal const float kSize = 5f;
			internal const float kGap = 4f;
			internal const float kAlpha = 0.6f;
			internal const float kSpanW = LoadingDots.kCount * LoadingDots.kSize + (LoadingDots.kCount - 1) * LoadingDots.kGap + LoadingDots.kTextGap;
			private const float kTextGap = 8f;
		}

		internal static void drawLoadingLine(ref float y, float w, string text)
		{
			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, RowPrefix.kLabel, null);

			float rowX = MenuHost.m_blockOffset.x;

			float rowY = MenuHost.m_blockOffset.y + y;
			Widgets.drawDots(rowX, rowY);

			string label = string.IsNullOrEmpty(text) ? Widgets.kLoadingText : text;
			Widgets.wrappedLabel(rowX + LoadingDots.kSpanW, rowY, w - LoadingDots.kSpanW, label, Theme.statusLabel, out float height);
			y += FastMath.max(Widgets.kLoadingH, height) + Widgets.kLoadingGap;
			Widgets.endRow(ref y, ref row);
		}

		private static void drawDots(float x, float rowY)
		{
			float dotY = rowY + (Widgets.kLoadingH - LoadingDots.kSize) * 0.5f;

			Color dotColor = ColorMath.withAlpha(Theme.m_accent, LoadingDots.kAlpha);
			for (int i = 0; i < LoadingDots.kCount; i++)
				Gui.fillRect(RectMath.create(x + i * (LoadingDots.kSize + LoadingDots.kGap), dotY, LoadingDots.kSize, LoadingDots.kSize), dotColor);
		}
	}
}
