using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace Trellis
{
	// an image backdrop changes colour under the band so the fade bakes what it covers
	internal static class ScrollFade
	{
		private static readonly Band kTop = new Band();
		private static readonly Band kBottom = new Band();

		private static class Bake
		{
			internal const int kWidth = 256;
			internal const int kHeight = 16;
			// sub pixel drift while the window moves must not rebake
			internal const float kKeySlack = 0.05f;
		}

		private sealed class Band
		{
			internal Texture2D m_texture;
			internal Il2CppStructArray<Color32> m_pixels;
			internal ImagePreview m_preview;
			internal MenuWindowBgFit m_fit;
			internal Color m_flat;
			internal float m_windowW;
			internal float m_windowH;
			internal float m_offsetX;
			internal float m_offsetY;
			internal float m_w;
			internal float m_h;
			internal float m_endWidth;
		}

		internal static void draw(Rect windowRect, Rect rect, bool top, float endWidth)
		{
			ImagePreview preview = MenuWindowBackground.preview;
			if (preview == null)
			{
				Theme.drawEdgeFade(rect, top ? Theme.scrollFadeTop : Theme.scrollFadeBottom, endWidth);
				return;
			}

			if (!Gui.repainting)
				return;

			Rect snapped = Gui.snapRect(rect);
			if (RectMath.w(snapped) <= 0f || RectMath.h(snapped) <= 0f)
				return;

			Band band = top ? ScrollFade.kTop : ScrollFade.kBottom;
			if (!ScrollFade.current(band, preview, windowRect, snapped, endWidth))
				ScrollFade.bake(band, preview, windowRect, snapped, top, endWidth);

			Gui.drawTexture(snapped, band.m_texture);
		}

		internal static void cleanup()
		{
			ScrollFade.release(ScrollFade.kTop);
			ScrollFade.release(ScrollFade.kBottom);
		}

		private static bool current(Band band, ImagePreview preview, Rect windowRect, Rect rect, float endWidth)
		{
			if (!Live.isUnityAlive(band.m_texture) || !object.ReferenceEquals(band.m_preview, preview) || band.m_fit != ThemeConfig.BackgroundFit)
				return false;
			if (!ColorMath.approxEqual(band.m_flat, Theme.scrollBackdrop))
				return false;

			return ScrollFade.near(band.m_windowW, RectMath.w(windowRect)) && ScrollFade.near(band.m_windowH, RectMath.h(windowRect)) && ScrollFade.near(band.m_offsetX, RectMath.x(rect) - RectMath.x(windowRect)) && ScrollFade.near(band.m_offsetY, RectMath.y(rect) - RectMath.y(windowRect)) && ScrollFade.near(band.m_w, RectMath.w(rect)) && ScrollFade.near(band.m_h, RectMath.h(rect)) && ScrollFade.near(band.m_endWidth, endWidth);
		}

		private static bool near(float a, float b) => FastMath.abs(a - b) < Bake.kKeySlack;

		private static void bake(Band band, ImagePreview preview, Rect windowRect, Rect rect, bool top, float endWidth)
		{
			if (!Live.isUnityAlive(band.m_texture))
			{
				band.m_texture = new Texture2D(Bake.kWidth, Bake.kHeight, TextureFormat.RGBA32, false)
				{
					filterMode = FilterMode.Bilinear,
					wrapMode = TextureWrapMode.Clamp,
					hideFlags = HideFlags.HideAndDontSave
				};
				band.m_pixels = new Il2CppStructArray<Color32>(Bake.kWidth * Bake.kHeight);
			}

			ScrollFade.fill(band.m_pixels, windowRect, rect, top, endWidth);

			band.m_texture.SetPixels32(band.m_pixels);
			band.m_texture.Apply(false, false);

			band.m_preview = preview;
			band.m_fit = ThemeConfig.BackgroundFit;
			band.m_flat = Theme.scrollBackdrop;
			band.m_windowW = RectMath.w(windowRect);
			band.m_windowH = RectMath.h(windowRect);
			band.m_offsetX = RectMath.x(rect) - RectMath.x(windowRect);
			band.m_offsetY = RectMath.y(rect) - RectMath.y(windowRect);
			band.m_w = RectMath.w(rect);
			band.m_h = RectMath.h(rect);
			band.m_endWidth = endWidth;
		}

		// texture rows run bottom up
		private static void fill(Il2CppStructArray<Color32> pixels, Rect windowRect, Rect rect, bool top, float endWidth)
		{
			Color panel = Theme.panelFill;

			float w = RectMath.w(rect);

			float h = RectMath.h(rect);

			float taperWidth = FastMath.min(endWidth, w * 0.5f);

			Span<Color32> span = pixels.AsSpan();
			for (int row = 0; row < Bake.kHeight; row++)
			{
				float fromBottom = (row + 0.5f) / Bake.kHeight;

				float guiY = RectMath.yMax(rect) - fromBottom * h;

				float rowAlpha = GraphicsInfo.fadeAlpha(Theme.edgeWeight(top ? 1f - fromBottom : fromBottom));
				for (int column = 0; column < Bake.kWidth; column++)
				{
					float offset = (column + 0.5f) / Bake.kWidth * w;

					float guiX = RectMath.x(rect) + offset;

					float taper = taperWidth > 0f ? EasingMath.smoothStep(FastMath.min(offset, w - offset) / taperWidth) : 1f;

					Color backdrop = GraphicsInfo.guiOver(panel, MenuWindowBackground.backdropAt(windowRect, guiX, guiY));
					span[row * Bake.kWidth + column] = ThemeTint.toColor32(GraphicsInfo.texel(ColorMath.withAlpha(backdrop, rowAlpha * taper)));
				}
			}
		}

		private static void release(Band band)
		{
			Textures.destroy(ref band.m_texture);

			band.m_pixels = null;
			band.m_preview = null;
		}
	}
}
