namespace Trellis
{
	internal sealed class Page
	{
		private readonly int[] m_blocks;
		private readonly int[][] m_columns;

		internal Page(int[] blocks) => m_blocks = blocks;
		internal Page(int[][] columns) => m_columns = columns;

		internal float draw(float w, float viewH) => this.drawAt(w, 0f);
		internal float drawAt(float w, float baseY) => m_columns != null ? MenuHost.drawPageColumns(w, baseY, m_columns) : MenuHost.drawPage(w, baseY, m_blocks);
	}
}
