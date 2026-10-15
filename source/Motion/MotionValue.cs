namespace Trellis
{
	internal struct MotionValue
	{
		private float m_position;
		private float m_previous;
		private float m_velocity;
		private float m_goal;
		private float m_omega;
		private float m_damping;
		private float m_ventAcceleration;
		private float m_rest;
		private int m_step;
		private int m_holdUntil;
		private MotionPhase m_phase;
		private bool m_endStop;

		internal float goal => m_goal;
		internal float velocity => m_velocity;
		internal bool atRest => m_phase == MotionPhase.Rest;
		internal bool venting => m_phase == MotionPhase.Vent;

		internal static MotionValue at(float value)
		{
			MotionClock.sync();
			return new MotionValue
			{
				m_position = value,
				m_previous = value,
				m_goal = value,
				m_step = MotionClock.step,
				m_phase = MotionPhase.Rest
			};
		}

		internal float sample()
		{
			advance();
			if (m_phase == MotionPhase.Rest)
				return m_position;

			return FastMath.lerpUnclamped(m_previous, m_position, MotionClock.blend);
		}

		internal float sampleLight() => FastMath.clamp01(sample());

		internal void snap(float value)
		{
			MotionClock.sync();
			m_position = value;
			m_previous = value;
			m_goal = value;
			m_velocity = 0f;
			m_step = MotionClock.step;
			m_holdUntil = 0;
			m_phase = MotionPhase.Rest;
		}

		internal void reel(float goal, in MotionProfile profile) => reelAfter(goal, profile, 0f);

		internal void reelAfter(float goal, in MotionProfile profile, float delaySeconds)
		{
			if (profile.m_instant)
			{
				snap(goal);
				return;
			}

			advance();
			if (m_phase == MotionPhase.Reel && m_goal == goal)
			{
				retune(profile);
				return;
			}
			if (m_phase == MotionPhase.Rest && m_position == goal)
				return;

			retune(profile);
			m_goal = goal;
			m_phase = MotionPhase.Reel;
			m_holdUntil = delaySeconds > 0f ? MotionClock.step + (int)(delaySeconds / MotionTokens.kSolver.m_stepSeconds) : 0;
		}

		internal void vent(float goal, in MotionProfile profile) => ventAfter(goal, profile, 0f);

		// the line is let go after a hold, a vent already pending keeps its original release step
		internal void ventAfter(float goal, in MotionProfile profile, float delaySeconds)
		{
			if (profile.m_instant)
			{
				snap(goal);
				return;
			}

			advance();
			if (m_phase == MotionPhase.Vent && m_goal == goal)
				return;
			if (m_phase == MotionPhase.Rest && m_position == goal)
				return;

			retune(profile);
			m_goal = goal;
			m_holdUntil = delaySeconds > 0f ? MotionClock.step + (int)(delaySeconds / MotionTokens.kSolver.m_stepSeconds) : 0;
			m_phase = MotionPhase.Vent;
		}

		internal void retune(in MotionProfile profile)
		{
			m_omega = profile.m_omega;
			m_damping = profile.m_damping;
			m_ventAcceleration = profile.m_ventAcceleration;
			m_rest = profile.m_rest;
			m_endStop = profile.m_endStop;
		}

		internal void offsetBy(float delta)
		{
			advance();
			m_position += delta;
			m_previous += delta;
		}

		private void advance()
		{
			MotionClock.sync();

			int now = MotionClock.step;

			int pending = now - m_step;
			if (pending <= 0)
				return;

			int first = m_step;
			m_step = now;
			if (m_phase == MotionPhase.Rest)
			{
				m_previous = m_position;
				return;
			}
			if (pending > MotionTokens.kMaxSteps)
			{
				land();
				return;
			}

			float h = MotionTokens.kSolver.m_stepSeconds;
			for (int i = 1; i <= pending; i++)
			{
				m_previous = m_position;
				if (first + i <= m_holdUntil)
					continue;

				if (integrate(h))
					return;
			}
		}

		private bool integrate(float h)
		{
			if (m_phase == MotionPhase.Vent)
			{
				if (!MotionMath.ventStep(ref m_position, ref m_velocity, m_goal, m_ventAcceleration, h))
					return false;

				land();
				return true;
			}

			float before = m_position - m_goal;
			MotionMath.springStep(ref m_position, ref m_velocity, m_goal, m_omega, m_damping, h);

			float after = m_position - m_goal;
			if (m_endStop && before * after <= 0f && before != 0f)
			{
				land();
				return true;
			}

			if (FastMath.abs(after) >= m_rest || FastMath.abs(m_velocity) >= m_rest * m_omega)
				return false;

			land();
			return true;
		}

		private void land()
		{
			m_position = m_goal;
			m_previous = m_goal;
			m_velocity = 0f;
			m_holdUntil = 0;
			m_phase = MotionPhase.Rest;
		}
	}
}
