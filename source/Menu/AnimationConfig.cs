namespace Trellis
{
	internal static class AnimationConfig
	{
		[Config]
		public static bool Enabled = true;

		[Config]
		public static float Speed = 1f;

		[Config]
		public static bool ReducedMotion = false;

		[Config]
		public static float MotionAmount = 1f;

		[Config]
		public static float DimStrength = 1f;

		[Config]
		public static bool HoverFeedback = true;

		[Config]
		public static bool PressFeedback = true;

		[Config]
		public static bool TabTransitions = true;

		[Config]
		public static bool StaggerBlocks = true;

		[Config]
		public static bool ContainerShuffle = true;

		[Config]
		public static bool PopupMotion = true;

		[Config]
		public static bool SectionReveals = true;

		[Config]
		public static bool SmoothScrolling = true;

		[Config]
		public static bool WindowMotion = true;

		[Config]
		public static bool TooltipMotion = true;

		internal static class Limits
		{
			internal const float kMinSpeed = 0.5f;
			internal const float kMaxSpeed = 2.5f;
			internal const float kSpeedStep = 0.05f;
			internal const float kMinMotion = 0f;
			internal const float kMaxMotion = 2f;
			internal const float kMinDim = 0f;
			internal const float kMaxDim = 1.5f;
			internal const float kAmountStep = 0.05f;
		}

		internal static bool hoverEnabled => AnimationConfig.Enabled && AnimationConfig.HoverFeedback;
		internal static bool pressEnabled => AnimationConfig.Enabled && AnimationConfig.PressFeedback;
		internal static bool tabsEnabled => AnimationConfig.Enabled && AnimationConfig.TabTransitions;
		internal static bool staggerEnabled => AnimationConfig.tabsEnabled && AnimationConfig.StaggerBlocks;
		internal static bool shuffleEnabled => AnimationConfig.Enabled && AnimationConfig.ContainerShuffle;
		internal static bool popupsEnabled => AnimationConfig.Enabled && AnimationConfig.PopupMotion;
		internal static bool sectionsEnabled => AnimationConfig.Enabled && AnimationConfig.SectionReveals;
		internal static bool rowsEnabled => AnimationConfig.sectionsEnabled;
		internal static bool scrollEnabled => AnimationConfig.Enabled && AnimationConfig.SmoothScrolling;
		internal static bool windowEnabled => AnimationConfig.Enabled && AnimationConfig.WindowMotion;
		internal static bool tooltipEnabled => AnimationConfig.Enabled && AnimationConfig.TooltipMotion;
	}
}
