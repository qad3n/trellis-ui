using UnityEngine;

namespace Trellis
{
	internal static class MenuWindowBackground
	{
		private static Texture2D m_texture;
		private static ImagePreview m_preview;
		private static Vector2 m_imageSize;
		private static string m_loadedName;
		private static bool m_loadAttempted;
		private static string m_status = string.Empty;

		internal static bool active => ThemeConfig.BackgroundMode == MenuWindowBgMode.Image && Live.isUnityAlive(MenuWindowBackground.m_texture);
		internal static string status => string.IsNullOrEmpty(MenuWindowBackground.m_status) ? UiImages.hint : MenuWindowBackground.m_status;

		internal static Color backdropColor
		{
			get
			{
				if (!MenuWindowBackground.active)
					return Theme.Surfaces.m_base;

				return MenuWindowBackground.dimmed(MenuWindowBackground.m_preview.mean);
			}
		}

		internal static ImagePreview preview => MenuWindowBackground.active ? MenuWindowBackground.m_preview : null;

		// what the window shows at a gui point, image letterbox included
		internal static Color backdropAt(Rect windowRect, float x, float y)
		{
			if (!MenuWindowBackground.active)
				return Theme.Surfaces.m_base;

			Vector2 box = VectorMath.v2(RectMath.w(windowRect), RectMath.h(windowRect));

			Vector2 shown = MenuWindowBackground.shownSize(box);
			if (shown.x <= 0f || shown.y <= 0f)
				return MenuWindowBackground.backdropColor;

			float u = (x - RectMath.x(windowRect) - LayoutMath.centerOffset(box.x, shown.x)) / shown.x;

			float v = 1f - (y - RectMath.y(windowRect) - LayoutMath.centerOffset(box.y, shown.y)) / shown.y;
			if (u < 0f || u > 1f || v < 0f || v > 1f)
				return GraphicsInfo.guiOver(MenuWindowBackground.scrim, Theme.Surfaces.m_base);

			return MenuWindowBackground.dimmed(MenuWindowBackground.m_preview.sample(u, v));
		}

		private static Color scrim => ColorMath.rgba(0f, 0f, 0f, MenuWindowBackground.scrimAlpha);

		private static Color dimmed(Color texel)
		{
			Color tinted = ColorMath.modulate(texel, GraphicsInfo.shaderColor(ThemeConfig.BackgroundTint));

			Color image = ColorMath.withAlpha(GraphicsInfo.fromTexel(tinted), 1f);
			return GraphicsInfo.guiOver(MenuWindowBackground.scrim, image);
		}

		private static Vector2 shownSize(Vector2 box)
		{
			switch (ThemeConfig.BackgroundFit)
			{
				case MenuWindowBgFit.Fit:
					return FitMath.contain(box, MenuWindowBackground.m_imageSize);

				case MenuWindowBgFit.Stretch:
					return box;

				default:
					return FitMath.cover(box, MenuWindowBackground.m_imageSize);
			}
		}

		private static float scrimAlpha => FastMath.clamp(ThemeConfig.BackgroundDim, ThemeConfig.Limits.kMinDim, ThemeConfig.Limits.kMaxDim);
		internal static void invalidate() => MenuWindowBackground.m_loadAttempted = false;

		internal static void cleanup()
		{
			Textures.destroy(ref MenuWindowBackground.m_texture);

			MenuWindowBackground.m_preview = null;

			MenuWindowBackground.m_loadedName = null;
			MenuWindowBackground.m_loadAttempted = false;
		}

		internal static bool draw(Rect rect)
		{
			MenuWindowBackground.ensureLoaded();

			if (!MenuWindowBackground.active)
				return false;
			if (!Gui.repainting)
				return true;

			if (ThemeConfig.BackgroundFit == MenuWindowBgFit.Fit)
				Theme.drawWindowFill(rect);

			Theme.drawWindowImage(rect, MenuWindowBackground.m_texture, MenuWindowBackground.scaleMode(), ThemeConfig.BackgroundTint);

			Theme.drawRoundedTint(rect, MenuWindowBackground.scrim);
			return true;
		}

		private static void ensureLoaded()
		{
			if (ThemeConfig.BackgroundMode != MenuWindowBgMode.Image)
				return;
			if (MenuWindowBackground.m_loadAttempted && MenuWindowBackground.m_loadedName == ThemeConfig.BackgroundImage)
				return;

			MenuWindowBackground.m_loadAttempted = true;
			MenuWindowBackground.m_loadedName = ThemeConfig.BackgroundImage;
			MenuWindowBackground.load(MenuWindowBackground.m_loadedName);
		}

		private static void load(string name)
		{
			Textures.destroy(ref MenuWindowBackground.m_texture);
			if (UiImages.tryLoad(name, TextureWrapMode.Clamp, out MenuWindowBackground.m_texture, out MenuWindowBackground.m_status, out MenuWindowBackground.m_preview))
				MenuWindowBackground.m_imageSize = VectorMath.v2(MenuWindowBackground.m_texture.width, MenuWindowBackground.m_texture.height);
		}

		private static ScaleMode scaleMode()
		{
			switch (ThemeConfig.BackgroundFit)
			{
				case MenuWindowBgFit.Fit:
					return ScaleMode.ScaleToFit;

				case MenuWindowBgFit.Stretch:
					return ScaleMode.StretchToFill;

				default:
					return ScaleMode.ScaleAndCrop;
			}
		}
	}
}
