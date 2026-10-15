using System.Collections.Generic;
using UnityEngine;
using static Trellis.MenuHost;

namespace Trellis
{
	internal static partial class Widgets
	{
		private static class ChipFlow
		{
			internal const string kRemoveMark = "x";
			internal const float kRemoveW = 14f;
			internal const float kLineGap = 4f;
			internal const float kHoverLift = 0.3f;
			internal const int kHoverSalt = 0x0C41;
		}

		// wraps removable chips onto as many lines as the width needs, returns the clicked index or -1
		internal static int drawChipFlow(ref float y, float w, IReadOnlyList<string> labels, Color color, string tooltip = null)
		{
			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, RowPrefix.kChips, null);

			float offsetX = MenuHost.m_blockOffset.x;

			float top = MenuHost.m_blockOffset.y + y;

			float lineH = Widgets.kBadgeHeight + ChipFlow.kLineGap;

			float x = 0f;

			float lineY = 0f;

			int clicked = -1;
			for (int i = 0; i < labels.Count; i++)
			{
				float chipW = FastMath.min(w, Widgets.badgeWidth(labels[i]) + ChipFlow.kRemoveW);
				if (x > 0f && x + chipW > w)
				{
					x = 0f;
					lineY += lineH;
				}

				Rect chip = RectMath.create(offsetX + x, top + lineY, chipW, Widgets.kBadgeHeight);
				if (Widgets.drawRemovableChip(chip, labels[i], color, tooltip))
					clicked = i;

				x += chipW + Widgets.kBadgeGap;
			}

			y += lineY + Widgets.kBadgeHeight + Widgets.kFieldGap;
			Widgets.endRow(ref y, ref row);
			return clicked;
		}

		private static bool drawRemovableChip(Rect chip, string label, Color color, string tooltip)
		{
			bool hovered = Gui.enabled && RectMath.contains(chip, MenuHost.scrollMouse);

			float hoverT = MenuAnimation.hoverProgress(unchecked(label.GetHashCode() * Widgets.kKeyMix) ^ ChipFlow.kHoverSalt, hovered);
			if (hovered)
				MenuHost.hoverTooltip(chip, tooltip);

			if (Gui.repainting && !MenuHost.rowCullable(RectMath.y(chip), RectMath.h(chip)))
			{
				Widgets.drawChip(chip, ColorMath.lerp(color, Theme.Text.m_primary, ChipFlow.kHoverLift * hoverT));

				Rect text = RectMath.create(RectMath.x(chip), RectMath.y(chip), RectMath.w(chip) - ChipFlow.kRemoveW, RectMath.h(chip));
				Widgets.fittedLabel(text, label, Theme.badgeLabel, TextAnchor.MiddleCenter);

				Rect remove = RectMath.create(RectMath.x(chip) + RectMath.w(chip) - ChipFlow.kRemoveW, RectMath.y(chip), ChipFlow.kRemoveW, RectMath.h(chip));
				Widgets.fittedLabel(remove, ChipFlow.kRemoveMark, Theme.badgeLabel, TextAnchor.MiddleLeft);
			}
			return hovered && MenuHost.mouseDown0;
		}
	}
}
