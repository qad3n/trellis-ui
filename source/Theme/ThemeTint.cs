using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace Trellis
{
	internal static class ThemeTint
	{
		private const float kByteMax = 255f;

		private static readonly List<Entry> kEntries = new List<Entry>();
		private static readonly List<Entry> kPersistentEntries = new List<Entry>();

		private sealed class Entry
		{
			internal Texture2D m_texture;
			internal float[] m_alphaWeight;
			internal Il2CppStructArray<Color32> m_pixels;
			internal Func<Color> m_source;
			internal Color m_applied;
			internal bool m_primed;
		}

		internal static Texture2D create(int width, int height, float[] alphaWeight, Func<Color> source, FilterMode filter, bool persistent = false)
		{
			Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
			{
				filterMode = filter,
				wrapMode = TextureWrapMode.Clamp,
				hideFlags = HideFlags.HideAndDontSave
			};

			Entry entry = new Entry
			{
				m_texture = texture,
				m_alphaWeight = alphaWeight,
				m_pixels = new Il2CppStructArray<Color32>(width * height),
				m_source = source
			};
			if (persistent)
				ThemeTint.kPersistentEntries.Add(entry);
			else
				ThemeTint.kEntries.Add(entry);

			ThemeTint.compose(entry, source());
			return texture;
		}

		internal static void retintChanged()
		{
			ThemeTint.retintList(ThemeTint.kEntries);
			ThemeTint.retintList(ThemeTint.kPersistentEntries);
		}

		internal static void onCleanup()
		{
			ThemeTint.kEntries.Clear();

			for (int i = 0; i < ThemeTint.kPersistentEntries.Count; i++)
				ThemeTint.kPersistentEntries[i].m_primed = false;
		}

		private static void retintList(List<Entry> list)
		{
			for (int i = 0; i < list.Count; i++)
			{
				Entry entry = list[i];

				Color color = entry.m_source();
				if (entry.m_primed && ColorMath.approxEqual(entry.m_applied, color))
					continue;

				ThemeTint.compose(entry, color);
			}
		}

		private static void compose(Entry entry, Color color)
		{
			float[] alphaWeight = entry.m_alphaWeight;

			Span<Color32> pixels = entry.m_pixels.AsSpan();

			int count = FastMath.min(alphaWeight.Length, pixels.Length);
			for (int i = 0; i < count; i++)
			{
				Color texel = GraphicsInfo.texel(ColorMath.rgba(color.r, color.g, color.b, color.a * alphaWeight[i]));
				pixels[i] = ThemeTint.toColor32(texel);
			}

			entry.m_texture.SetPixels32(entry.m_pixels);
			entry.m_texture.Apply(false, false);
			entry.m_applied = color;
			entry.m_primed = true;
		}

		internal static Color32 toColor32(in Color color) => ColorMath.rgba32((byte)FastMath.roundToInt(FastMath.clamp01(color.r) * ThemeTint.kByteMax), (byte)FastMath.roundToInt(FastMath.clamp01(color.g) * ThemeTint.kByteMax), (byte)FastMath.roundToInt(FastMath.clamp01(color.b) * ThemeTint.kByteMax), (byte)FastMath.roundToInt(FastMath.clamp01(color.a) * ThemeTint.kByteMax));
	}
}
