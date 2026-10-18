namespace Trellis
{
	internal readonly struct SelectRowItem
	{
		internal readonly string m_animId;
		internal readonly string m_title;
		internal readonly string m_detail;
		internal readonly bool m_enabled;
		internal readonly int m_chipMask;

		internal SelectRowItem(string animId, string title, string detail, bool enabled, int chipMask = 0)
		{
			m_animId = animId;
			m_title = title;
			m_detail = detail;
			m_enabled = enabled;
			m_chipMask = chipMask;
		}
	}
}
