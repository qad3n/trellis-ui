namespace Trellis
{
	internal readonly struct MultiSelectItemHint
	{
		internal readonly bool m_inactive;
		internal readonly string m_note;
		internal readonly string m_tooltip;

		internal MultiSelectItemHint(bool inactive, string note, string tooltip = null)
		{
			m_inactive = inactive;
			m_note = note;
			m_tooltip = tooltip;
		}
	}
}
