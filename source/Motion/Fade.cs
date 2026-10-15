namespace Trellis
{
	internal static class Fade
	{
		// user took it away so it is cut fast
		internal static float cut(ref MotionValue light, bool on, bool animate) => Fade.drive(ref light, on, MotionProfile.light(animate), MotionProfile.light(animate));

		// system let it go so it fades over the size of what it lit
		internal static float release(ref MotionValue light, bool on, float extentPx, bool animate) => Fade.drive(ref light, on, MotionProfile.light(animate), MotionProfile.release(extentPx, animate));

		internal static float frame(ref MotionValue light, bool on, float extentPx, bool animate)
		{
			MotionProfile profile = MotionProfile.frameLight(extentPx, animate);
			return Fade.drive(ref light, on, profile, profile);
		}

		// occlusion flickers so it only dims once the change outlasts a reading fixation
		internal static float settle(ref MotionValue light, bool on, float level, bool animate)
		{
			if (on)
				light.reel(1f, MotionProfile.light(animate));
			else
				light.ventAfter(level, MotionProfile.light(animate), MotionTokens.kFixation.m_seconds);
			return light.sampleLight();
		}

		internal static MotionValue spawn(float holdSeconds, float extentPx, bool animate)
		{
			MotionValue light = MotionValue.at(1f);
			light.ventAfter(0f, MotionProfile.release(extentPx, animate), holdSeconds);
			return light;
		}

		internal static float releaseSeconds(float extentPx) => AnimationConfig.Enabled ? MotionProfile.releaseSeconds(extentPx) : 0f;
		internal static bool dark(in MotionValue light) => light.atRest && light.goal <= 0f;

		private static float drive(ref MotionValue light, bool on, in MotionProfile rising, in MotionProfile falling)
		{
			if (on)
				light.reel(1f, rising);
			else
				light.vent(0f, falling);
			return light.sampleLight();
		}
	}
}
