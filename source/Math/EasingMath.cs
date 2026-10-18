namespace Trellis
{
	internal static class EasingMath
	{
		private const float kRainbowScale = 0.25f;

		internal static float smoothStep(float t)
		{
			if (t <= 0f)
				return 0f;
			if (t >= 1f)
				return 1f;

			return t * t * (3f - 2f * t);
		}

		// 1 at the edge with a long tail and no slope where it ends
		internal static float cubicFalloff(float t)
		{
			if (t <= 0f)
				return 1f;
			if (t >= 1f)
				return 0f;

			float inverse = 1f - t;
			return inverse * inverse * inverse;
		}

		internal static float wrap01(float value)
		{
			float remainder = value % 1f;
			return remainder < 0f ? remainder + 1f : remainder;
		}

		internal static float expDecay(float dt, float rate) => 1f - FastMath.exp(-dt * rate);
		internal static float midpoint(float a, float b) => (a + b) * 0.5f;

		internal static float indexFraction(int index, int count)
		{
			if (count <= 1)
				return 0f;

			return index / (float)(count - 1);
		}

		internal static float normalize(float value, float min, float max) => FastMath.clamp01((value - min) / (max - min));
		internal static float snapStep(float value, float min, float max, float step) => FastMath.clamp(FastMath.round(value / step) * step, min, max);

		internal static float fadeOut(float elapsed, float holdTime, float fadeTime)
		{
			if (elapsed <= holdTime)
				return 1f;

			return FastMath.clamp01(1f - (elapsed - holdTime) / fadeTime);
		}

		internal static float pulseSine(float t) => 0.5f + 0.5f * FastMath.sin(t);
		internal static float pulseCosine(float t) => 0.5f + 0.5f * FastMath.cos(t);
		internal static float pulseSineHz(float t, float hz) => 0.5f + 0.5f * FastMath.sin(t * hz * (2f * FastMath.kPI));
		internal static float bouncePhase(float t) => FastMath.pingPong(t / FastMath.kPI, 1f);
		internal static float repeatPhase(float t) => FastMath.repeat(t / (FastMath.kPI * 2f), 1f);

		internal static float heartbeatWave(float t, float attack = 0.18f)
		{
			float phase = EasingMath.repeatPhase(t);
			return phase < attack ? phase / attack : 1f - (phase - attack) / (1f - attack);
		}

		internal static float rainbowHue(float time, float speed) => FastMath.repeat(time * speed * EasingMath.kRainbowScale, 1f);
	}
}
