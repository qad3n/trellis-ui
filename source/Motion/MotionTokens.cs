namespace Trellis
{
	internal static class MotionTokens
	{
		// wire rope stretches about 2 percent under shock
		internal static readonly LineToken kCable = new LineToken(0.02f);
		internal static readonly LineToken kHand = new LineToken(0f);
		internal static readonly LineToken kLight = new LineToken(0f);

		internal static readonly LoadToken kAnchorLoad = new LoadToken(16f, 0.1f, 0.05f);

		// light fades out as slowly as it fades in or the 50 ms body vent reads as a pop
		internal static readonly LoadToken kLightLoad = new LoadToken(16f, 0.1f, 0.1f);

		internal static readonly SolverToken kSolver = new SolverToken(1f / 360f, 1f, 0.5f, 1f / 510f);

		internal static readonly DwellToken kFixation = new DwellToken(0.225f, 4f);

		internal static readonly float kAnchorOmega = MotionMath.settleOmega(MotionTokens.kAnchorLoad.m_settleSeconds, MotionTokens.kCable.m_damping);
		internal static readonly float kVentAcceleration = MotionMath.ventAcceleration(MotionTokens.kAnchorLoad.m_extentPx, MotionTokens.kAnchorLoad.m_ventSeconds);
		internal static readonly float kLightVentAcceleration = MotionMath.ventAcceleration(MotionTokens.kLightLoad.m_extentPx, MotionTokens.kLightLoad.m_ventSeconds);
		internal static readonly int kMaxSteps = (int)(MotionTokens.kSolver.m_maxCatchUp / MotionTokens.kSolver.m_stepSeconds);
	}
}
