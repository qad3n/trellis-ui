namespace Trellis
{
	// one row of an overlay's content list, with the slot its flag used to live in
	internal sealed class OverlayOption
	{
		internal readonly string m_label;
		internal readonly bool m_on;
		internal readonly int m_legacySlot;

		internal OverlayOption(string label, bool on, int legacySlot)
		{
			m_label = label;
			m_on = on;
			m_legacySlot = legacySlot;
		}
	}
}
