namespace Trellis
{
	internal sealed class StickToBottom
	{
		private const float kDefaultEpsilon = 4f;
		private const float kDefaultIdle = 2f;

		private readonly float m_epsilon;
		private readonly float m_idleSeconds;
		private bool m_stuck;
		private float m_scrollTime;

		internal StickToBottom(float idleSeconds = StickToBottom.kDefaultIdle, float epsilon = StickToBottom.kDefaultEpsilon)
		{
			m_epsilon = epsilon;
			m_idleSeconds = idleSeconds;
			m_stuck = true;
			m_scrollTime = float.NegativeInfinity;
		}

		internal bool stuck => m_stuck;

		internal void reset()
		{
			m_stuck = true;
			m_scrollTime = float.NegativeInfinity;
		}

		internal void noteScroll(float currentY, float maxY)
		{
			m_scrollTime = Gui.unscaledTime;

			if (maxY <= 0f)
			{
				m_stuck = true;
				return;
			}
			m_stuck = currentY >= maxY - m_epsilon;
		}

		internal float follow(float currentY, float maxY)
		{
			if (m_stuck)
				return maxY;

			return FastMath.clamp(currentY, 0f, maxY);
		}

		internal void onGrowth(bool grew, float maxY, ref float currentY)
		{
			if (!grew)
				return;
			if (!(m_stuck || Gui.unscaledTime - m_scrollTime >= m_idleSeconds))
				return;

			currentY = maxY;

			m_stuck = true;
		}
	}
}
