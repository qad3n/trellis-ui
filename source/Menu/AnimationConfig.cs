using System.Reflection;

namespace Trellis
{
	[Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
	internal static class AnimationConfig
	{
		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static bool Enabled = true;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static float Speed = 1f;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static bool ReducedMotion = false;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static float MotionAmount = 1f;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static float DimStrength = 1f;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static bool HoverFeedback = true;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static bool PressFeedback = true;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static bool TabTransitions = true;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static bool StaggerBlocks = true;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static bool ContainerShuffle = true;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static bool PopupMotion = true;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static bool SectionReveals = true;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static bool SmoothScrolling = true;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static bool WindowMotion = true;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
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
