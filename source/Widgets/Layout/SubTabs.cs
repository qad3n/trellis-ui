using System.Collections.Generic;
using UnityEngine;
using static Trellis.MenuHost;

namespace Trellis
{
	internal static partial class Widgets
	{
		internal const float kSubTabBarH = Widgets.kSubTabH + MenuHost.kContentMargin;
		private const float kSubTabAccentStartRatio = 0.2f;
		private const float kSubTabAccentWidthRatio = 0.6f;
		private const float kSubTabAccentThickness = 2f;
		private const float kSubTabEpsilon = 0.5f;
		private const float kSubTabH = 28f;

		private static readonly Dictionary<string, string> kUpperTabs = new Dictionary<string, string>();
		private static readonly Dictionary<int, float> kSubTabWidths = new Dictionary<int, float>();

		internal static int drawSubTabsFixed(float x, float y, float w, string[] names, int selected, string animKey = null)
		{
			const float kGap = MenuHost.kContentMargin;

			float totalGaps = kGap * (names.Length - 1);

			float tabW = (w - totalGaps) / names.Length;
			for (int i = 0; i < names.Length; i++)
			{
				float tabX = x + i * (tabW + kGap);

				GUIStyle style = i == selected ? Theme.activeSubTab : Theme.subTab;

				Rect tabRect = RectMath.create(tabX, y, tabW, Widgets.kSubTabH);

				string label = Widgets.markup(Widgets.kUpperTabs, names[i], string.Empty, string.Empty, true);
				if (!(Widgets.scaledButton(tabRect, label, style, MenuHost.mousePosition) && i != selected))
					continue;

				if (animKey != null)
					MenuAnimation.noteSwitch(animKey, selected, i);

				selected = i;

				MenuHost.closePopups();
			}

			float selectedOffset = selected * (tabW + kGap) + tabW * Widgets.kSubTabAccentStartRatio;

			Rect target = RectMath.create(selectedOffset, Widgets.kSubTabH - Widgets.kSubTabAccentThickness, tabW * Widgets.kSubTabAccentWidthRatio, Widgets.kSubTabAccentThickness);

			int key = animKey != null ? animKey.GetHashCode() : names.GetHashCode();
			Widgets.kSubTabWidths.TryGetValue(key, out float lastW);

			bool barResized = FastMath.abs(lastW - w) > Widgets.kSubTabEpsilon;

			Widgets.kSubTabWidths[key] = w;

			MarkerFrame line = MenuAnimation.displayedMarker(key, target, MenuHost.mousePosition.x - x, true, barResized);
			Widgets.drawMarker(line, VectorMath.v2(x, y), Theme.accentBar);
			return selected;
		}
	}
}
