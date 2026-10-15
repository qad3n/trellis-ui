namespace Trellis
{
	internal readonly struct LineToken
	{
		internal readonly float m_stretch;
		internal readonly float m_damping;

		internal LineToken(float stretch)
		{
			m_stretch = stretch;
			m_damping = MotionMath.dampingFor(stretch);
		}
	}
}
