using System;

namespace Trellis
{
	internal sealed class TabRow
	{
		internal readonly string m_label;
		internal readonly Func<float, float, float> m_draw;
		internal readonly Action<float, float, float> m_drawHeader;
		// a page that runs its own scroll view gets the full width and no outer scroll
		internal readonly Func<bool> m_ownsScroll;

		internal TabRow(string label, Func<float, float, float> draw, Action<float, float, float> drawHeader = null, Func<bool> ownsScroll = null)
		{
			m_label = label;
			m_draw = draw;
			m_drawHeader = drawHeader;
			m_ownsScroll = ownsScroll;
		}
	}
}
