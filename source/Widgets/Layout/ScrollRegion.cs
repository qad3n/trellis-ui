namespace Trellis
{
	internal readonly struct ScrollRegion
	{
		internal readonly float m_contentW;
		internal readonly float m_viewH;
		internal readonly float m_contentY;

		internal ScrollRegion(float contentW, float viewH, float contentY)
		{
			m_contentW = contentW;
			m_viewH = viewH;
			m_contentY = contentY;
		}
	}
}
