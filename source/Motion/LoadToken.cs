namespace Trellis
{
	internal readonly struct LoadToken
	{
		internal readonly float m_extentPx;
		internal readonly float m_settleSeconds;
		internal readonly float m_ventSeconds;

		internal LoadToken(float extentPx, float settleSeconds, float ventSeconds)
		{
			m_extentPx = extentPx;
			m_settleSeconds = settleSeconds;
			m_ventSeconds = ventSeconds;
		}
	}
}
