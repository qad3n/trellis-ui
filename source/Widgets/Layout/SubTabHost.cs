using System;

namespace Trellis
{
	internal sealed class SubTabHost
	{
		private readonly string m_animKey;
		private readonly string[] m_names;
		private readonly Func<int, float, float, float> m_drawPage;
		// the compiler only caches static method groups
		private readonly Func<int, float, float, float> m_guardedDraw;
		private int m_selected;

		internal SubTabHost(string animKey, string[] names, Func<int, float, float, float> drawPage)
		{
			m_animKey = animKey;
			m_names = names;
			m_drawPage = drawPage;
			m_guardedDraw = drawGuarded;
		}

		internal int selected => m_selected;
		internal static SubTabHost forBlocks<TBlock>(string animKey, (string label, TBlock[] blocks)[] pages) where TBlock : unmanaged, Enum => new SubTabHost(animKey, RowTable.column(pages, page => page.label), (int index, float w, float viewH) => MenuHost.drawBlocks(w, 0f, pages[index].blocks));
		internal void drawHeader(float x, float y, float w) => m_selected = Widgets.drawSubTabsFixed(x, y, w, m_names, m_selected, m_animKey);
		internal float draw(float w, float viewH) => MenuAnimation.drawSubTabs(m_animKey, m_selected, w, viewH, m_guardedDraw);
		private float drawGuarded(int index, float w, float viewH) => index >= 0 && index < m_names.Length ? m_drawPage(index, w, viewH) : 0f;
	}
}
