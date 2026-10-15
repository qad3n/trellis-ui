using UnityEngine;
using static Trellis.MenuHost;

namespace Trellis
{
	internal static partial class Widgets
	{
		internal static bool drawCycleRow(ref float y, float w, string label, string display, string tooltip = null)
		{
			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, RowPrefix.kDropdown, label);

			float offsetX = MenuHost.m_blockOffset.x;

			float offsetY = MenuHost.m_blockOffset.y;

			Rect rowRect = RectMath.create(offsetX, offsetY + y, w, MenuHost.kFieldH);

			bool rowHovered = Widgets.hoverRow(rowRect, tooltip);

			Widgets.fittedLabel(RectMath.create(offsetX, offsetY + y, MenuHost.kLabelW, MenuHost.kFieldH), Widgets.bold(label), Widgets.labelStyle(rowHovered), TextAnchor.MiddleLeft);

			bool clicked = Widgets.scrollButton(RectMath.create(offsetX + MenuHost.kLabelW, offsetY + y, w - MenuHost.kLabelW, MenuHost.kFieldH), display, Theme.dropdownButton);

			y += MenuHost.kFieldH + Widgets.kFieldGap;
			Widgets.endRow(ref y, ref row);
			return clicked;
		}
	}
}
