namespace Trellis
{
	internal static class AnimationConfig
	{
		[Config(0xFA80D220AE577AA8)]
		public static bool Enabled = true;

		[Config(0x71CB844AF744550E)]
		public static float Speed = 1f;

		[Config(0x8143D0F041B0C771)]
		public static bool ReducedMotion = false;

		[Config(0x734F82D3279F482D)]
		public static float MotionAmount = 1f;

		[Config(0xB8CE8EA63AB2063E)]
		public static float DimStrength = 1f;

		[Config(0x81D5285B6D14A088)]
		public static bool HoverFeedback = true;

		[Config(0xC167D7F597061271)]
		public static bool PressFeedback = true;

		[Config(0x93EDCA5D50F8FB4E)]
		public static bool TabTransitions = true;

		[Config(0xC6BCCFC0597B1AE0)]
		public static bool StaggerBlocks = true;

		[Config(0xFA5BA1C5E3F68747)]
		public static bool ContainerShuffle = true;

		[Config(0xD845AED5934D4A49)]
		public static bool PopupMotion = true;

		[Config(0x0EE1F451B0497678)]
		public static bool SectionReveals = true;

		[Config(0xAF2249A9AD7E4868)]
		public static bool SmoothScrolling = true;

		[Config(0x6C08F47FE8BD9BC9)]
		public static bool WindowMotion = true;

		[Config(0xF5DEE02AAAECB9C8)]
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
