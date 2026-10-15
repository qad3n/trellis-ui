using static Trellis.MenuHost;

namespace Trellis
{
	internal static partial class Widgets
	{
		private const float kDividerPadY = 10f;
		private const float kDividerInsetX = 2f;
		private const float kDividerThickness = 1f;

		internal static void drawDivider(ref float y, float w)
		{
			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, RowPrefix.kDivider, null);

			float offsetX = MenuHost.m_blockOffset.x;

			float offsetY = MenuHost.m_blockOffset.y;

			y += Widgets.kDividerPadY;
			Gui.drawStroke(RectMath.create(offsetX + Widgets.kDividerInsetX, offsetY + y, w - Widgets.kDividerInsetX * 2f, Widgets.kDividerThickness), Theme.divider);

			y += Widgets.kDividerPadY;
			Widgets.endRow(ref y, ref row);
		}
	}
}
