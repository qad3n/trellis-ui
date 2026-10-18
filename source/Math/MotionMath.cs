namespace Trellis
{
	internal static class MotionMath
	{
		// ln 50 so the envelope is down to 2 percent
		private const float kSettleEnvelope = 3.91202307f;
		// solves (1 + x) e^-x = 0.02 for a critically damped settle to 2 percent
		private const float kCriticalSettle = 5.8335f;
		// the usual 1 plus 0.7 zeta fit
		private const float kRiseDamping = 0.7f;

		internal static float dampingFor(float stretch)
		{
			if (stretch <= 0f)
				return 1f;

			float logStretch = -FastMath.log(stretch);
			return logStretch / FastMath.sqrt(FastMath.kPI * FastMath.kPI + logStretch * logStretch);
		}

		internal static float settleOmega(float settleSeconds, float damping) => MotionMath.kSettleEnvelope / (damping * settleSeconds);
		internal static float settleSeconds(float omega, float damping) => MotionMath.kSettleEnvelope / (damping * omega);
		internal static float criticalOmega(float settleSeconds) => MotionMath.kCriticalSettle / settleSeconds;

		internal static float massOmega(float anchorOmega, float anchorExtent, float extent) => anchorOmega * FastMath.sqrt(anchorExtent / FastMath.max(extent, 1f));

		internal static float halfRise(float omega, float damping) => (1f + MotionMath.kRiseDamping * damping) / omega;

		internal static float ventAcceleration(float span, float seconds) => 2f * span / (seconds * seconds);
		internal static float ventSeconds(float span, float acceleration) => FastMath.sqrt(2f * span / acceleration);
		// square root read as sluggish on a full window and flat read as a pop so frames grow with the cube root
		internal static float frameSeconds(float anchorSeconds, float anchorExtent, float extent) => anchorSeconds * FastMath.pow(FastMath.max(extent, anchorExtent) / anchorExtent, 1f / 3f);
		internal static float extent(float width, float height) => FastMath.sqrt(FastMath.max(width, 0f) * FastMath.max(height, 0f));

		internal static void springStep(ref float position, ref float velocity, float goal, float omega, float damping, float h)
		{
			velocity += (-omega * omega * (position - goal) - 2f * damping * omega * velocity) * h;
			position += velocity * h;
		}

		internal static bool ventStep(ref float position, ref float velocity, float goal, float acceleration, float h)
		{
			float direction = goal >= position ? 1f : -1f;
			velocity += direction * acceleration * h;

			float next = position + velocity * h;
			if ((goal - next) * direction <= 0f)
			{
				position = goal;
				velocity = 0f;
				return true;
			}

			position = next;
			return false;
		}
	}
}
