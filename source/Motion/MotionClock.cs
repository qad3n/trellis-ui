namespace Trellis
{
	internal static class MotionClock
	{
		private static int m_frame = -1;
		private static float m_lastTime = -1f;
		private static float m_accumulator;

		internal static int step { get; private set; }
		internal static float blend { get; private set; }

		internal static void sync()
		{
			int frame = Gui.frameCount;
			if (frame == MotionClock.m_frame)
				return;

			MotionClock.m_frame = frame;

			float now = Gui.unscaledTime;

			float elapsed = MotionClock.m_lastTime < 0f ? 0f : now - MotionClock.m_lastTime;

			MotionClock.m_lastTime = now;

			float stepSeconds = MotionTokens.kSolver.m_stepSeconds;
			MotionClock.m_accumulator += FastMath.clamp(elapsed, 0f, MotionTokens.kSolver.m_maxCatchUp);

			int steps = (int)(MotionClock.m_accumulator / stepSeconds);
			MotionClock.m_accumulator -= steps * stepSeconds;
			MotionClock.step += steps;
			MotionClock.blend = MotionClock.m_accumulator / stepSeconds;
		}
	}
}
