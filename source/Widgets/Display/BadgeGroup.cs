namespace Trellis
{
	// badges of one group merge into a single bracket that opens with the prefix
	internal readonly struct BadgeGroup
	{
		internal const int kNone = -1;

		internal readonly string m_prefix;
		internal readonly string m_code;

		internal BadgeGroup(string prefix, string code)
		{
			m_prefix = prefix;
			m_code = code;
		}
	}
}
