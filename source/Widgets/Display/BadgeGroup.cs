namespace Trellis
{
	// in the grouped style badges of one group merge into a single bracket that opens with the prefix
	internal readonly struct BadgeGroup
	{
		internal const int kNone = -1;

		internal readonly string m_prefix;

		internal BadgeGroup(string prefix)
		{
			m_prefix = prefix;
		}
	}
}
