using System.Diagnostics;

namespace Trellis
{
	internal struct ReflowThrottle
	{
		internal const int kVisiblePasses = 2;
		private const float kIntervalSeconds = 0.05f;
		private const double kSliceMs = 1.5;
		private const double kMsPerSecond = 1000.0;

		private float m_committed;
		private float m_live;
		private float m_commitTime;
		private float m_changeTime;
		private bool m_primed;
		private bool m_moving;

		internal readonly bool moving => m_moving;
		internal static long sliceDeadline() => Stopwatch.GetTimestamp() + (long)(ReflowThrottle.kSliceMs * Stopwatch.Frequency / ReflowThrottle.kMsPerSecond);
		internal static bool sliceExpired(long deadline) => Stopwatch.GetTimestamp() >= deadline;

		internal float step(float value, float now)
		{
			if (!m_primed)
			{
				m_primed = true;
				m_committed = value;
				m_live = value;
				m_commitTime = now;
				m_changeTime = now - ReflowThrottle.kIntervalSeconds;
				m_moving = false;
				return value;
			}

			if (value != m_live)
			{
				m_live = value;
				m_changeTime = now;
			}

			if (m_committed != m_live && now - m_commitTime >= ReflowThrottle.kIntervalSeconds)
			{
				m_committed = m_live;
				m_commitTime = now;
			}
			m_moving = m_committed != m_live || now - m_changeTime < ReflowThrottle.kIntervalSeconds;
			return m_committed;
		}
	}
}
