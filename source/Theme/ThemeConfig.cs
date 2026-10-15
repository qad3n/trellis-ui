using System.Reflection;
using UnityEngine;

namespace Trellis
{
	[Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
	internal static class ThemeConfig
	{
		private const float kDefaultDim = 0.35f;
		private const float kDefaultBlockOpacity = 0.92f;
		private const float kDefaultPanelOpacity = 0.92f;
		private const float kDefaultContrast = 0.85f;
		private const string kDefaultTint = "00AAFF";
		private const int kHashMix = 397;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static float SurfaceTint;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static float SurfaceContrast = ThemeConfig.kDefaultContrast;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static float Brightness = 1f;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static float BorderStrength = 1f;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static float BorderWidth = 1f;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static float AccentIntensity = 1f;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static float TextBrightness = 1f;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static float CornerRadius;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static bool TacticalHeaders;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static SurfaceTintSource TintSource = SurfaceTintSource.Accent;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static Color TintColor = ColorHex.parse(ThemeConfig.kDefaultTint);

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static bool[] SlotOverride = new bool[ThemeSlots.kCount];

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static Color[] SlotColor = new Color[ThemeSlots.kCount];

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static MenuWindowBgMode BackgroundMode = MenuWindowBgMode.Solid;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static MenuWindowBgFit BackgroundFit = MenuWindowBgFit.Cover;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static string BackgroundImage = string.Empty;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static Color BackgroundTint = ColorMath.kWhite;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static float BackgroundDim = ThemeConfig.kDefaultDim;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static float MenuOpacity = 1f;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static float PanelOpacity = ThemeConfig.kDefaultPanelOpacity;

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		public static float BlockOpacity = ThemeConfig.kDefaultBlockOpacity;

		internal static class Limits
		{
			internal const float kMinTint = 0f;
			internal const float kMaxTint = 0.12f;
			internal const float kMinContrast = 0f;
			internal const float kMaxContrast = 1.8f;
			internal const float kMinBrightness = 0.7f;
			internal const float kMaxBrightness = 1.35f;
			internal const float kMinBorderStrength = 0f;
			internal const float kMaxBorderStrength = 1.5f;
			internal const float kMinBorderWidth = 0.5f;
			internal const float kMaxBorderWidth = 3f;
			internal const float kMinAccent = 0.3f;
			internal const float kMaxAccent = 1.5f;
			internal const float kMinTextBrightness = 0.8f;
			internal const float kMaxTextBrightness = 1.15f;
			internal const float kMinRadius = 0f;
			internal const float kMaxRadius = 8f;
			internal const float kMinDim = 0f;
			internal const float kMaxDim = 0.85f;
			internal const float kMinOpacity = 0.55f;
			internal const float kMaxOpacity = 1f;
			internal const float kMinMenuOpacity = 0.3f;
			internal const float kMaxMenuOpacity = 1f;
		}

		internal static int bakeHash()
		{
			int hash = ThemeConfig.TacticalHeaders ? 1 : 0;

			hash = ThemeConfig.mixInt(hash, (int)ThemeConfig.BackgroundMode);
			hash = ThemeConfig.mixInt(hash, (int)ThemeConfig.TintSource);
			hash = ThemeConfig.mixFloat(hash, ThemeConfig.SurfaceTint);
			hash = ThemeConfig.mixFloat(hash, ThemeConfig.SurfaceContrast);
			hash = ThemeConfig.mixFloat(hash, ThemeConfig.Brightness);
			hash = ThemeConfig.mixFloat(hash, ThemeConfig.BorderStrength);
			hash = ThemeConfig.mixFloat(hash, ThemeConfig.AccentIntensity);
			hash = ThemeConfig.mixFloat(hash, ThemeConfig.TextBrightness);
			hash = ThemeConfig.mixFloat(hash, ThemeConfig.PanelOpacity);
			hash = ThemeConfig.mixFloat(hash, ThemeConfig.BlockOpacity);
			hash = ThemeConfig.mixColor(hash, ThemeConfig.TintColor);

			int slots = FastMath.min(ThemeConfig.SlotOverride.Length, ThemeConfig.SlotColor.Length);
			for (int i = 0; i < slots; i++)
			{
				if (!ThemeConfig.SlotOverride[i])
					continue;

				hash = ThemeConfig.mixColor(ThemeConfig.mixInt(hash, i), ThemeConfig.SlotColor[i]);
			}
			return hash;
		}

		private static int mixFloat(int hash, float value) => unchecked((hash * ThemeConfig.kHashMix) ^ value.GetHashCode());
		private static int mixColor(int hash, Color value) => ThemeConfig.mixFloat(ThemeConfig.mixFloat(ThemeConfig.mixFloat(ThemeConfig.mixFloat(hash, value.r), value.g), value.b), value.a);
		private static int mixInt(int hash, int value) => unchecked((hash * ThemeConfig.kHashMix) ^ value);
	}
}
