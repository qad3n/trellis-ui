using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Trellis
{
	internal static class ColorMath
	{
		private const float kChannelScale = 255f;
		private const int kChannelMax = 255;
		private const float kHueSectors = 6f;
		private const float kGreenSector = 2f;
		private const float kBlueSector = 4f;
		// engine Vector4 epsilon squared
		private const float kEqualitySqr = 9.99999944E-11f;

		internal static readonly Color kWhite = ColorMath.rgba(1f, 1f, 1f, 1f);
		internal static readonly Color kBlack = ColorMath.rgba(0f, 0f, 0f, 1f);
		internal static readonly Color kTransparent = ColorMath.rgba(0f, 0f, 0f, 0f);

		private static class Gradient3
		{
			internal const float kStop1 = 0.33f;
			internal const float kStop2 = 0.66f;
			internal const float kSpan = 0.33f;
			internal const float kFinalSpan = 0.34f;
		}

		private static class Srgb
		{
			internal const float kGammaCutoff = 0.04045f;
			internal const float kLinearCutoff = 0.0031308f;
			internal const float kLinearSlope = 12.92f;
			internal const float kOffset = 0.055f;
			internal const float kOffsetScale = 1.055f;
			internal const float kGamma = 2.4f;
			internal const float kHdrGamma = 2.2f;
		}

		private static class Luminance
		{
			internal const float kRedWeight = 0.2126f;
			internal const float kGreenWeight = 0.7152f;
			internal const float kBlueWeight = 0.0722f;
			internal const int kSolveSteps = 12;
			internal const float kFullValue = 1f;
			internal const float kNoSaturation = 0f;
			internal const float kSaturationDecay = 0.5f;
			internal const float kMinSaturation = 0.01f;
		}

		// the Color ctor is an interop invoke
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Color rgba(float r, float g, float b, float a)
		{
			Color c = default;
			c.r = r;
			c.g = g;
			c.b = b;
			c.a = a;
			return c;
		}

		internal static Color32 rgba32(byte r, byte g, byte b, byte a)
		{
			Color32 c = default;
			c.r = r;
			c.g = g;
			c.b = b;
			c.a = a;
			return c;
		}

		internal static Color fromBytes(byte r, byte g, byte b, byte a) => ColorMath.rgba(r / ColorMath.kChannelScale, g / ColorMath.kChannelScale, b / ColorMath.kChannelScale, a / ColorMath.kChannelScale);
		internal static Color fade(Color color, float alpha) => ColorMath.rgba(color.r, color.g, color.b, color.a * alpha);
		internal static Color withAlpha(Color color, float alpha) => ColorMath.rgba(color.r, color.g, color.b, alpha);
		internal static Color withRgb(Color target, Color rgbSource) => ColorMath.rgba(rgbSource.r, rgbSource.g, rgbSource.b, target.a);
		internal static Color modulate(Color tint, Color source) => ColorMath.rgba(tint.r * source.r, tint.g * source.g, tint.b * source.b, tint.a * source.a);

		internal static Color lerp(Color a, Color b, float t)
		{
			float c = FastMath.clamp01(t);
			return ColorMath.rgba(a.r + (b.r - a.r) * c, a.g + (b.g - a.g) * c, a.b + (b.b - a.b) * c, a.a + (b.a - a.a) * c);
		}

		internal static Color lerpVisible(Color a, Color b, float t)
		{
			float c = FastMath.clamp01(t);
			if (a.a <= 0f)
				return ColorMath.withAlpha(b, b.a * c);
			if (b.a <= 0f)
				return ColorMath.withAlpha(a, a.a * (1f - c));

			return ColorMath.lerp(a, b, c);
		}

		internal static Color over(Color top, Color bottom)
		{
			float below = bottom.a * (1f - top.a);

			float alpha = top.a + below;
			if (alpha <= 0f)
				return ColorMath.kTransparent;

			float inverse = 1f / alpha;
			return ColorMath.rgba((top.r * top.a + bottom.r * below) * inverse, (top.g * top.a + bottom.g * below) * inverse, (top.b * top.a + bottom.b * below) * inverse, alpha);
		}

		internal static Color lerpUnclamped(Color a, Color b, float t) => ColorMath.rgba(FastMath.clamp01(a.r + (b.r - a.r) * t), FastMath.clamp01(a.g + (b.g - a.g) * t), FastMath.clamp01(a.b + (b.b - a.b) * t), FastMath.clamp01(a.a + (b.a - a.a) * t));

		internal static float linearToGamma(float channel)
		{
			if (channel <= Srgb.kLinearCutoff)
				return channel * Srgb.kLinearSlope;
			if (channel < 1f)
				return Srgb.kOffsetScale * FastMath.pow(channel, 1f / Srgb.kGamma) - Srgb.kOffset;

			return FastMath.pow(channel, 1f / Srgb.kHdrGamma);
		}

		internal static Color gammaToLinear(Color color) => ColorMath.rgba(ColorMath.gammaToLinear(color.r), ColorMath.gammaToLinear(color.g), ColorMath.gammaToLinear(color.b), color.a);
		internal static Color linearToGamma(Color color) => ColorMath.rgba(ColorMath.linearToGamma(color.r), ColorMath.linearToGamma(color.g), ColorMath.linearToGamma(color.b), color.a);
		internal static float relativeLuminance(Color color) => Luminance.kRedWeight * ColorMath.gammaToLinear(color.r) + Luminance.kGreenWeight * ColorMath.gammaToLinear(color.g) + Luminance.kBlueWeight * ColorMath.gammaToLinear(color.b);

		internal static Color hsvAtLuminance(float hue, float saturation, float targetLuminance, float alpha)
		{
			float usableSaturation = saturation;
			while (usableSaturation > Luminance.kNoSaturation && ColorMath.relativeLuminance(ColorMath.hsvToColor(hue, usableSaturation, Luminance.kFullValue, alpha)) < targetLuminance)
			{
				usableSaturation *= Luminance.kSaturationDecay;
				if (!(usableSaturation < Luminance.kMinSaturation))
					continue;

				usableSaturation = Luminance.kNoSaturation;
			}

			float high = Luminance.kFullValue;

			float low = 0f;
			for (int step = 0; step < Luminance.kSolveSteps; step++)
			{
				float mid = (low + high) * 0.5f;
				if (ColorMath.relativeLuminance(ColorMath.hsvToColor(hue, usableSaturation, mid, alpha)) < targetLuminance)
					low = mid;
				else
					high = mid;
			}
			return ColorMath.hsvToColor(hue, usableSaturation, high, alpha);
		}

		internal static bool engineEquals(Color a, Color b)
		{
			float dr = a.r - b.r;

			float dg = a.g - b.g;

			float db = a.b - b.b;

			float da = a.a - b.a;
			return dr * dr + dg * dg + db * db + da * da < ColorMath.kEqualitySqr;
		}

		internal static bool approxEqual(Color a, Color b, float epsilon = 0.001f) => FastMath.abs(a.r - b.r) < epsilon && FastMath.abs(a.g - b.g) < epsilon && FastMath.abs(a.b - b.b) < epsilon && FastMath.abs(a.a - b.a) < epsilon;

		// the struct GetHashCode is an invoke
		internal static void addHash(ref HashCode hash, in Color color)
		{
			hash.Add(color.r);
			hash.Add(color.g);
			hash.Add(color.b);
			hash.Add(color.a);
		}

		internal static int hash(in Color color)
		{
			HashCode hash = default;
			ColorMath.addHash(ref hash, color);
			return hash.ToHashCode();
		}

		internal static Color hsvToColor(float hue, float saturation, float value, float alpha)
		{
			if (saturation == 0f)
				return ColorMath.rgba(value, value, value, alpha);
			if (value == 0f)
				return ColorMath.rgba(0f, 0f, 0f, alpha);

			float scaledHue = hue * ColorMath.kHueSectors;

			int sector = FastMath.floorToInt(scaledHue);

			float fraction = scaledHue - sector;

			float bottom = value * (1f - saturation);

			float falling = value * (1f - saturation * fraction);

			float rising = value * (1f - saturation * (1f - fraction));

			float b;
			float r;
			float g;
			switch (sector + 1)
			{
				case 0:
					r = value;

					g = bottom;

					b = falling;
					break;

				case 1:
					r = value;

					g = rising;

					b = bottom;
					break;

				case 2:
					r = falling;

					g = value;

					b = bottom;
					break;

				case 3:
					r = bottom;

					g = value;

					b = rising;
					break;

				case 4:
					r = bottom;

					g = falling;

					b = value;
					break;

				case 5:
					r = rising;

					g = bottom;

					b = value;
					break;

				case 6:
					r = value;

					g = bottom;

					b = falling;
					break;

				case 7:
					r = value;

					g = rising;

					b = bottom;
					break;

				default:
					r = 0f;

					g = 0f;

					b = 0f;
					break;
			}
			return ColorMath.rgba(FastMath.clamp01(r), FastMath.clamp01(g), FastMath.clamp01(b), alpha);
		}

		internal static void rgbToHsv(Color color, out float hue, out float saturation, out float value)
		{
			float max = FastMath.max(color.r, FastMath.max(color.g, color.b));

			float min = FastMath.min(color.r, FastMath.min(color.g, color.b));

			float span = max - min;

			value = max;

			if (span <= 0f || max <= 0f)
			{
				hue = 0f;

				saturation = 0f;
				return;
			}
			saturation = span / max;

			if (max == color.r)
				hue = (color.g - color.b) / span / ColorMath.kHueSectors;
			else if (max == color.g)
				hue = (ColorMath.kGreenSector + (color.b - color.r) / span) / ColorMath.kHueSectors;
			else
				hue = (ColorMath.kBlueSector + (color.r - color.g) / span) / ColorMath.kHueSectors;
			if (!(hue < 0f))
				return;

			hue += 1f;
		}

		internal static byte toByte(float value) => (byte)FastMath.clamp(FastMath.roundToInt(value * ColorMath.kChannelScale), 0, ColorMath.kChannelMax);
		internal static byte lerpByte(byte a, byte b, float t) => (byte)(a + (b - a) * t);

		internal static (byte r, byte g, byte b) interpolate((byte r, byte g, byte b)[] colors, float t)
		{
			float position = t * colors.Length;

			int a = (int)position % colors.Length;

			int b = (a + 1) % colors.Length;

			float fraction = position - FastMath.floor(position);
			return (ColorMath.lerpByte(colors[a].r, colors[b].r, fraction), ColorMath.lerpByte(colors[a].g, colors[b].g, fraction), ColorMath.lerpByte(colors[a].b, colors[b].b, fraction));
		}

		internal static (byte r, byte g, byte b) interpolateLinear((byte r, byte g, byte b)[] colors, float t)
		{
			if (colors.Length == 1)
				return colors[0];

			float position = FastMath.clamp01(t) * (colors.Length - 1);

			int a = FastMath.min((int)position, colors.Length - 2);

			int b = a + 1;

			float fraction = position - a;
			return (ColorMath.lerpByte(colors[a].r, colors[b].r, fraction), ColorMath.lerpByte(colors[a].g, colors[b].g, fraction), ColorMath.lerpByte(colors[a].b, colors[b].b, fraction));
		}

		internal static (byte r, byte g, byte b) toBytes(Color color) => (ColorMath.toByte(color.r), ColorMath.toByte(color.g), ColorMath.toByte(color.b));

		internal static Color glow(Color color, float multiplier = 2f) => ColorMath.rgba(FastMath.min(color.r * multiplier, 1f), FastMath.min(color.g * multiplier, 1f), FastMath.min(color.b * multiplier, 1f), color.a);
		// scales down instead of clipping a channel so a bright tint keeps its hue
		internal static Color boost(Color color, float multiplier)
		{
			Color scaled = ColorMath.shade(color, multiplier);
			float peak = FastMath.max(scaled.r, FastMath.max(scaled.g, scaled.b));
			return peak > 1f ? ColorMath.rgba(scaled.r / peak, scaled.g / peak, scaled.b / peak, color.a) : scaled;
		}

		internal static Color shade(Color color, float multiplier = 0.8f) => ColorMath.rgba(color.r * multiplier, color.g * multiplier, color.b * multiplier, color.a);

		internal static Color peakNormalized(Color color)
		{
			float peak = FastMath.max(color.r, FastMath.max(color.g, color.b));
			return peak > 0f ? ColorMath.rgba(color.r / peak, color.g / peak, color.b / peak, color.a) : ColorMath.withAlpha(ColorMath.kWhite, color.a);
		}

		internal static Color addScaled(Color color, Color added, float scale) => ColorMath.rgba(color.r + added.r * scale, color.g + added.g * scale, color.b + added.b * scale, color.a);
		internal static Color glass(Color color, float maxAlpha = 0.35f) => ColorMath.rgba(color.r, color.g, color.b, FastMath.min(color.a, maxAlpha));
		internal static int packRgba(Color color) => ((int)(FastMath.clamp01(color.r) * ColorMath.kChannelScale) << 24) | ((int)(FastMath.clamp01(color.g) * ColorMath.kChannelScale) << 16) | ((int)(FastMath.clamp01(color.b) * ColorMath.kChannelScale) << 8) | (int)(FastMath.clamp01(color.a) * ColorMath.kChannelScale);

		internal static Color lerpGradient3(float t, Color c0, Color c1, Color c2, Color c3)
		{
			if (t < Gradient3.kStop1)
				return ColorMath.lerp(c0, c1, t / Gradient3.kSpan);
			if (t < Gradient3.kStop2)
				return ColorMath.lerp(c1, c2, (t - Gradient3.kStop1) / Gradient3.kSpan);

			return ColorMath.lerp(c2, c3, FastMath.clamp01((t - Gradient3.kStop2) / Gradient3.kFinalSpan));
		}

		internal static (byte r, byte g, byte b)[] uniqueColors((byte r, byte g, byte b)?[] perChar)
		{
			List<(byte r, byte g, byte b)> unique = new List<(byte r, byte g, byte b)>();
			foreach ((byte r, byte g, byte b)? entry in perChar)
			{
				if (!(entry != null && !unique.Contains(entry.Value)))
					continue;

				unique.Add(entry.Value);
			}
			return unique.ToArray();
		}

		private static float gammaToLinear(float channel)
		{
			if (channel <= Srgb.kGammaCutoff)
				return channel / Srgb.kLinearSlope;
			if (channel < 1f)
				return FastMath.pow((channel + Srgb.kOffset) / Srgb.kOffsetScale, Srgb.kGamma);

			return FastMath.pow(channel, Srgb.kHdrGamma);
		}
	}
}
