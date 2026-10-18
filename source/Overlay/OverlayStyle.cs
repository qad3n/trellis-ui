namespace Trellis
{
	internal static class OverlayStyle
	{
		internal static class Limits
		{
			internal const float kMinScale = 0.5f;
			internal const float kMaxScale = 2.5f;
			internal const float kScaleStep = 0.05f;
		}

		internal static float scale(float own) => FastMath.clamp(own, Limits.kMinScale, Limits.kMaxScale);
	}
}
