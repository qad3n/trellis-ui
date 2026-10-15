namespace Trellis
{
	internal readonly struct DwellToken
	{
		internal readonly float m_seconds;
		internal readonly float m_radiusPx;

		internal DwellToken(float seconds, float radiusPx)
		{
			m_seconds = seconds;
			m_radiusPx = radiusPx;
		}
	}
}
