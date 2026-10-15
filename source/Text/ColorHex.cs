using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Trellis
{
	internal static class ColorHex
	{
		internal const char kPrefix = '#';
		private const char kQuote = '"';
		private const char kListSeparator = ',';
		private const string kUppercase = "0123456789ABCDEF";
		private const byte kOpaque = 255;
		private const int kCacheMax = 4096;
		private const int kNotHex = -1;
		private const string kRainbowList = "FF0000,FFAA00,FFFF00,00FF88,0088FF,AA00FF";

		// same values as vanilla MiscExtensions.NamedColorHex
		private static readonly Dictionary<string, string> kNamedColors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "aqua", "00ffff" }, { "black", "000000" }, { "blue", "0000ff" }, { "brown", "a52a2a" }, { "cyan", "00ffff" }, { "darkblue", "0000a0" }, { "fuchsia", "ff00ff" }, { "green", "008000" }, { "grey", "808080" }, { "lightblue", "add8e6" }, { "lime", "00ff00" }, { "magenta", "ff00ff" }, { "maroon", "800000" }, { "navy", "000080" }, { "olive", "808000" }, { "orange", "ffa500" }, { "purple", "800080" }, { "red", "ff0000" }, { "silver", "c0c0c0" }, { "teal", "008080" }, { "white", "ffffff" }, { "yellow", "ffff00" } };
		private static readonly (byte r, byte g, byte b)[] kDefaultRainbow = ColorHex.parseList(ColorHex.kRainbowList);
		// color Equals and GetHashCode are interop invokes
		private static readonly Dictionary<int, string> kCache = new Dictionary<int, string>();

		private static class Digits
		{
			internal const int kShortRgb = 3;
			internal const int kShortRgba = 4;
			internal const int kRgb = 6;
			internal const int kRgba = 8;
			internal const int kPerByte = 2;
			internal const int kRed = 0;
			internal const int kGreen = 2;
			internal const int kBlue = 4;
			internal const int kAlpha = 6;
		}

		private static class Bits
		{
			internal const int kNibbleShift = 4;
			internal const int kNibbleMask = 0xF;
			internal const int kLetterValue = 10;
			internal const int kRedShift = 24;
			internal const int kGreenShift = 16;
			internal const int kBlueShift = 8;
		}

		internal static bool hexDigit(char c) => ColorHex.nibble(c) != ColorHex.kNotHex;
		internal static bool validHex(string value) => value != null && ColorHex.validHex(value.AsSpan());

		// legacy Text and IMGUI only render 6 and 8 digit hex
		internal static bool tryExpand(string value, out string digits)
		{
			digits = null;

			if (string.IsNullOrEmpty(value))
				return false;

			ReadOnlySpan<char> span = value.AsSpan().Trim().Trim(ColorHex.kQuote);
			if (span.Length == 0)
				return false;

			if (span[0] == ColorHex.kPrefix)
				span = span[1..];
			else if (ColorHex.kNamedColors.TryGetValue(span.ToString(), out digits))
				return true;
			if (!ColorHex.validHex(span))
				return false;

			switch (span.Length)
			{
				case Digits.kShortRgb:
				case Digits.kShortRgba:
					digits = ColorHex.widen(span);
					return true;

				case Digits.kRgb:
				case Digits.kRgba:
					digits = span.ToString();
					return true;

				default:
					return false;
			}
		}

		internal static bool tryParse(string value, out Color color)
		{
			if (!ColorHex.tryExpand(value, out string digits))
			{
				color = ColorMath.kWhite;
				return false;
			}

			byte alpha = digits.Length == Digits.kRgba ? ColorHex.byteAt(digits, Digits.kAlpha) : ColorHex.kOpaque;
			color = ColorMath.fromBytes(ColorHex.byteAt(digits, Digits.kRed), ColorHex.byteAt(digits, Digits.kGreen), ColorHex.byteAt(digits, Digits.kBlue), alpha);
			return true;
		}

		internal static bool tryParseRgb(string value, out (byte r, byte g, byte b) rgb)
		{
			if (!ColorHex.tryExpand(value, out string digits))
			{
				rgb = default;
				return false;
			}
			rgb = (ColorHex.byteAt(digits, Digits.kRed), ColorHex.byteAt(digits, Digits.kGreen), ColorHex.byteAt(digits, Digits.kBlue));
			return true;
		}

		internal static Color parse(string value)
		{
			if (!ColorHex.tryParse(value, out Color color))
				throw new FormatException($"'{value}' is not a hex colour");

			return color;
		}

		internal static (byte r, byte g, byte b)[] parseList(string csv)
		{
			if (string.IsNullOrWhiteSpace(csv))
				return ColorHex.kDefaultRainbow;

			List<(byte r, byte g, byte b)> result = new List<(byte r, byte g, byte b)>();
			foreach (string part in csv.Split(ColorHex.kListSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			{
				if (!ColorHex.tryParseRgb(part, out (byte r, byte g, byte b) rgb))
					continue;

				result.Add(rgb);
			}
			return result.Count > 0 ? result.ToArray() : ColorHex.kDefaultRainbow;
		}

		// clamp or an hdr value prints a third digit and breaks the markup
		internal static string of(Color color)
		{
			byte r = ColorMath.toByte(color.r);

			byte g = ColorMath.toByte(color.g);

			byte b = ColorMath.toByte(color.b);

			byte a = ColorMath.toByte(color.a);

			int key = (r << Bits.kRedShift) | (g << Bits.kGreenShift) | (b << Bits.kBlueShift) | a;
			if (ColorHex.kCache.TryGetValue(key, out string hex))
				return hex;

			if (ColorHex.kCache.Count >= ColorHex.kCacheMax)
				ColorHex.kCache.Clear();

			StringBuilder builder = new StringBuilder(Digits.kRgba);
			ColorHex.append(builder, (r, g, b));

			if (a != ColorHex.kOpaque)
				ColorHex.append(builder, a);

			hex = builder.ToString();
			ColorHex.kCache[key] = hex;
			return hex;
		}

		internal static void clearCache() => ColorHex.kCache.Clear();

		internal static void append(StringBuilder builder, (byte r, byte g, byte b) rgb)
		{
			ColorHex.append(builder, rgb.r);
			ColorHex.append(builder, rgb.g);
			ColorHex.append(builder, rgb.b);
		}

		private static bool validHex(ReadOnlySpan<char> value)
		{
			foreach (char c in value)
			{
				if (ColorHex.nibble(c) == ColorHex.kNotHex)
					return false;
			}
			return true;
		}

		private static void append(StringBuilder builder, byte value)
		{
			builder.Append(ColorHex.kUppercase[value >> Bits.kNibbleShift]);
			builder.Append(ColorHex.kUppercase[value & Bits.kNibbleMask]);
		}

		private static string widen(ReadOnlySpan<char> shorthand)
		{
			Span<char> wide = stackalloc char[shorthand.Length * Digits.kPerByte];
			for (int i = 0; i < shorthand.Length; i++)
			{
				wide[i * Digits.kPerByte] = shorthand[i];
				wide[i * Digits.kPerByte + 1] = shorthand[i];
			}
			return new string(wide);
		}

		private static byte byteAt(string digits, int index) => (byte)((ColorHex.nibble(digits[index]) << Bits.kNibbleShift) | ColorHex.nibble(digits[index + 1]));

		private static int nibble(char c)
		{
			if (c >= '0' && c <= '9')
				return c - '0';
			if (c >= 'a' && c <= 'f')
				return c - 'a' + Bits.kLetterValue;
			if (c >= 'A' && c <= 'F')
				return c - 'A' + Bits.kLetterValue;

			return ColorHex.kNotHex;
		}
	}
}
