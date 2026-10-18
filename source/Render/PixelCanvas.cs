using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace Trellis
{
	internal sealed class PixelCanvas
	{
		private Texture2D m_texture;
		private Il2CppStructArray<Color32> m_pixels;
		private int m_width;
		private int m_height;

		internal Texture2D texture => m_texture;
		internal int width => m_width;
		internal int height => m_height;

		internal bool resize(int pixelWidth, int pixelHeight)
		{
			if (Live.isUnityAlive(m_texture) && pixelWidth == m_width && pixelHeight == m_height)
				return false;

			allocate(FastMath.max(1, pixelWidth), FastMath.max(1, pixelHeight));
			return true;
		}

		internal void clear() => m_pixels.AsSpan().Clear();

		internal void begin(int pixelWidth, int pixelHeight)
		{
			resize(pixelWidth, pixelHeight);
			clear();
		}

		internal void shiftLeft(int columns)
		{
			if (columns <= 0)
				return;

			Span<Color32> span = m_pixels.AsSpan();

			int kept = m_width - columns;
			for (int y = 0; y < m_height; y++)
			{
				Span<Color32> row = span.Slice(y * m_width, m_width);
				if (kept > 0)
					row.Slice(columns, kept).CopyTo(row);

				row.Slice(FastMath.max(0, kept)).Clear();
			}
		}

		internal void fillColumn(int x, int fromY, int toY, Color32 color)
		{
			if (x < 0 || x >= m_width)
				return;

			int start = FastMath.max(0, fromY);

			int end = FastMath.min(m_height, toY);

			Span<Color32> span = m_pixels.AsSpan();
			for (int y = start; y < end; y++)
				span[y * m_width + x] = color;
		}

		internal void setPixel(int x, int y, Color32 color)
		{
			if (x < 0 || x >= m_width || y < 0 || y >= m_height)
				return;

			m_pixels.AsSpan()[y * m_width + x] = color;
		}

		internal void upload()
		{
			m_texture.SetPixels32(m_pixels);
			m_texture.Apply(false, false);
		}

		internal void release()
		{
			Textures.destroy(ref m_texture);

			m_pixels = null;
			m_width = 0;
			m_height = 0;
		}

		private void allocate(int pixelWidth, int pixelHeight)
		{
			Textures.destroy(ref m_texture);

			m_width = pixelWidth;
			m_height = pixelHeight;
			m_pixels = new Il2CppStructArray<Color32>(m_width * m_height);
			m_texture = Textures.createPoint(m_width, m_height);
		}
	}
}
