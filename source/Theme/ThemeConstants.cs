namespace Trellis
{
	internal static class ThemeConstants
	{
		internal const float kAlphaFaint = 0.04f;
		internal const float kTabHover = 0.06f;
		internal const float kAlphaDim = 0.08f;
		internal const float kAlphaSubtle = 0.12f;
		internal const float kAlphaAcute = 0.15f;
		internal const float kAlphaHover = 0.3f;
		internal const float kAlphaSelection = 0.4f;
		internal const float kAlphaGlow = 0.5f;
		internal const float kAlphaEdge = 0.6f;
		internal const float kRebuildSeconds = 0.1f;
		internal const float kBorderTint = 0.35f;
		internal const float kFocusTint = 0.3f;
		internal const float kBorderMix = 0.5f;
		internal const int kFpsGood = 60;
		internal const int kFpsCaution = 30;

		internal static class CaptureTint
		{
			internal const float kRest = 0.35f;
			internal const float kHover = 0.45f;
			internal const float kPress = 0.6f;
		}

		internal static readonly HsvDelta kHover = new HsvDelta(-0.15f, +0.15f);
		internal static readonly HsvDelta kDim = new HsvDelta(+0.1f, -0.2f);
		internal static readonly HsvDelta kTitle = new HsvDelta(-0.35f, +0.25f);
		internal static readonly HsvDelta kBright = new HsvDelta(-0.2f, +0.3f);
		internal static readonly HsvDelta kStatusHover = new HsvDelta(-0.1f, +0.03f);
	}
}
