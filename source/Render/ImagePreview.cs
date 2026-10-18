using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace Trellis
{
	// cpu copy of a small mip so overlays can match what the image shows under them
	internal sealed class ImagePreview
	{
		private const float kChannelScale = 255f;

		private readonly Color32[] m_pixels;
		private readonly int m_width;
		private readonly int m_height;

		internal Color mean { get; }

		private ImagePreview(Color32[] pixels, int width, int height)
		{
			m_pixels = pixels;
			m_width = width;
			m_height = height;
			mean = ImagePreview.average(pixels);
		}

		internal static ImagePreview capture(Texture2D texture, int maxTexels)
		{
			int level = ImagePreview.mipFor(texture, maxTexels);

			int width = FastMath.max(1, texture.width >> level);

			int height = FastMath.max(1, texture.height >> level);

			Il2CppStructArray<Color32> pixels = texture.GetPixels32(level);

			Span<Color32> span = pixels.AsSpan();
			if (span.Length != width * height)
				throw new InvalidOperationException($"mip {level} of a {texture.width}x{texture.height} image returned {span.Length} pixels, expected {width * height}");

			return new ImagePreview(span.ToArray(), width, height);
		}

		// uv is in texture space with v up and clamps edges like the bilinear sampler
		internal Color sample(float u, float v)
		{
			float x = FastMath.clamp(u * m_width - 0.5f, 0f, m_width - 1);

			float y = FastMath.clamp(v * m_height - 0.5f, 0f, m_height - 1);

			int x0 = FastMath.floorToInt(x);

			int y0 = FastMath.floorToInt(y);

			int x1 = FastMath.min(x0 + 1, m_width - 1);

			int y1 = FastMath.min(y0 + 1, m_height - 1);

			float tx = x - x0;

			float ty = y - y0;

			Color bottom = ColorMath.lerp(texel(x0, y0), texel(x1, y0), tx);

			Color top = ColorMath.lerp(texel(x0, y1), texel(x1, y1), tx);
			return ColorMath.lerp(bottom, top, ty);
		}

		private Color texel(int x, int y)
		{
			Color32 pixel = m_pixels[y * m_width + x];
			return ColorMath.rgba(pixel.r / ImagePreview.kChannelScale, pixel.g / ImagePreview.kChannelScale, pixel.b / ImagePreview.kChannelScale, 1f);
		}

		private static Color average(Color32[] pixels)
		{
			long red = 0;

			long green = 0;

			long blue = 0;
			for (int i = 0; i < pixels.Length; i++)
			{
				red += pixels[i].r;

				green += pixels[i].g;

				blue += pixels[i].b;
			}

			float scale = 1f / (pixels.Length * ImagePreview.kChannelScale);
			return ColorMath.rgba(red * scale, green * scale, blue * scale, 1f);
		}

		private static int mipFor(Texture2D texture, int maxTexels)
		{
			int width = texture.width;

			int height = texture.height;

			int lastLevel = texture.mipmapCount - 1;

			int level = 0;
			while (level < lastLevel && FastMath.max(1, width >> level) * FastMath.max(1, height >> level) > maxTexels)
				level++;
			return level;
		}
	}
}
