namespace Trellis
{
	internal readonly struct SolverToken
	{
		internal readonly float m_stepSeconds;
		internal readonly float m_maxCatchUp;
		internal readonly float m_restPx;
		internal readonly float m_restLight;

		internal SolverToken(float stepSeconds, float maxCatchUp, float restPx, float restLight)
		{
			m_stepSeconds = stepSeconds;
			m_maxCatchUp = maxCatchUp;
			m_restPx = restPx;
			m_restLight = restLight;
		}
	}
}
