namespace Trellis
{
	internal readonly struct MotionProfile
	{
		internal readonly float m_omega;
		internal readonly float m_damping;
		internal readonly float m_ventAcceleration;
		internal readonly float m_rest;
		internal readonly bool m_endStop;
		internal readonly bool m_instant;

		private MotionProfile(float omega, float damping, float ventAcceleration, float rest, bool endStop, bool instant)
		{
			m_omega = omega;
			m_damping = damping;
			m_ventAcceleration = ventAcceleration;
			m_rest = rest;
			m_endStop = endStop;
			m_instant = instant;
		}

		private static float speed => AnimationConfig.Speed;
		private static bool bodiesMove => AnimationConfig.Enabled && !AnimationConfig.ReducedMotion;

		internal static MotionProfile body(float extentPx, float unitPx, bool animate) => MotionProfile.cable(MotionMath.massOmega(MotionTokens.kAnchorOmega, MotionTokens.kAnchorLoad.m_extentPx, extentPx), unitPx, false, animate && MotionProfile.bodiesMove);

		internal static MotionProfile payout(float extentPx, float spanPx, bool animate) => MotionProfile.cable(MotionProfile.landOmega(extentPx), spanPx, true, animate && MotionProfile.bodiesMove);

		internal static MotionProfile hand(float unitPx, bool animate)
		{
			float unit = FastMath.max(unitPx, 1f);
			return new MotionProfile(MotionTokens.kAnchorOmega * MotionProfile.speed, MotionTokens.kHand.m_damping, MotionProfile.ventAcceleration(unit), MotionTokens.kSolver.m_restPx / unit, false, !(animate && MotionProfile.bodiesMove));
		}

		// a critical tail crawls below a pixel per frame and reads as stepping so the hop lands on the end stop
		internal static MotionProfile glide(float extentPx, bool animate) => MotionProfile.cable(MotionProfile.landOmega(extentPx), 1f, true, animate && MotionProfile.bodiesMove);

		internal static MotionProfile light(bool animate) => MotionProfile.lightOver(MotionTokens.kLightLoad.m_extentPx, animate);

		internal static MotionProfile release(float extentPx, bool animate) => MotionProfile.lightOver(FastMath.max(extentPx, MotionTokens.kLightLoad.m_extentPx), animate);

		internal static MotionProfile frameLight(float extentPx, bool animate)
		{
			float seconds = MotionMath.frameSeconds(MotionTokens.kAnchorLoad.m_ventSeconds, MotionTokens.kAnchorLoad.m_extentPx, extentPx) / MotionProfile.speed;

			float omega = FastMath.min(MotionTokens.kAnchorOmega * MotionProfile.speed, MotionMath.criticalOmega(seconds));
			return new MotionProfile(omega, MotionTokens.kLight.m_damping, MotionMath.ventAcceleration(1f, seconds), MotionTokens.kSolver.m_restLight, false, !(animate && AnimationConfig.Enabled));
		}

		internal static float releaseSeconds(float extentPx) => MotionMath.ventSeconds(FastMath.max(extentPx, MotionTokens.kLightLoad.m_extentPx), MotionTokens.kLightVentAcceleration * MotionProfile.speed * MotionProfile.speed);

		private static MotionProfile cable(float omega, float unitPx, bool endStop, bool animate)
		{
			float unit = FastMath.max(unitPx, 1f);
			return new MotionProfile(omega * MotionProfile.speed, MotionTokens.kCable.m_damping, MotionProfile.ventAcceleration(unit), MotionTokens.kSolver.m_restPx / unit, endStop, !animate);
		}

		private static MotionProfile lightOver(float spanPx, bool animate)
		{
			bool instant = !(animate && AnimationConfig.Enabled);
			return new MotionProfile(MotionTokens.kAnchorOmega * MotionProfile.speed, MotionTokens.kLight.m_damping, MotionTokens.kLightVentAcceleration * MotionProfile.speed * MotionProfile.speed / spanPx, MotionTokens.kSolver.m_restLight, false, instant);
		}

		// reveals span the whole content width so the square root mass law left them hanging
		private static float landOmega(float extentPx) => MotionMath.settleOmega(MotionMath.frameSeconds(MotionTokens.kAnchorLoad.m_settleSeconds, MotionTokens.kAnchorLoad.m_extentPx, extentPx), MotionTokens.kCable.m_damping);

		private static float ventAcceleration(float unitPx) => MotionTokens.kVentAcceleration * MotionProfile.speed * MotionProfile.speed / unitPx;
	}
}
