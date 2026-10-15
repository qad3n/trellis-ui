using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace Trellis
{
	internal static class Textures
	{
		private const int kAnisoLevel = 8;

		internal delegate Color PixelShader(int x, int y);

		internal static void destroy(ref Texture2D texture) => Live.destroy(ref texture);
		internal static void destroy(ref Texture3D texture) => Live.destroy(ref texture);

		// il2cpp arrays into SetPixels32 are weakly held and a 1MB one got collected before the copy so pin a managed one
		internal static unsafe Texture3D createVolume(int width, int height, int depth, Color32[] pixels, string name)
		{
			Texture3D texture = new Texture3D(width, height, depth, TextureFormat.RGBA32, false)
			{
				name = name,
				hideFlags = HideFlags.HideAndDontSave,
				wrapMode = TextureWrapMode.Repeat,
				filterMode = FilterMode.Bilinear
			};

			bool uploaded;
			fixed (Color32* data = pixels)
				uploaded = texture.SetPixelDataImpl((IntPtr)data, 0, sizeof(Color32), pixels.Length, 0);

			if (!uploaded)
			{
				Textures.destroy(ref texture);
				throw new InvalidOperationException($"'{name}' refused {pixels.Length} pixels for {width}x{height}x{depth}");
			}
			texture.Apply(false, true);
			return texture;
		}

		internal static void destroy(Texture2D texture) => Live.destroy(ref texture);

		// a shared evicting cache destroyed textures out from under live materials
		internal static Texture2D createSolid(Color color)
		{
			Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
			texture.SetPixel(0, 0, color);
			texture.Apply(false, false);
			texture.hideFlags = HideFlags.HideAndDontSave;
			texture.wrapMode = TextureWrapMode.Repeat;
			return texture;
		}

		internal static Texture2D createPoint(int width, int height)
		{
			return new Texture2D(width, height, TextureFormat.RGBA32, false)
			{
				filterMode = FilterMode.Point,
				wrapMode = TextureWrapMode.Clamp,
				hideFlags = HideFlags.HideAndDontSave
			};
		}

		// gui bakes only since scene materials must stay raw
		internal static Texture2D createPixel(int width, int height, FilterMode filterMode, PixelShader shade)
		{
			Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

			Il2CppStructArray<Color> buffer = new Il2CppStructArray<Color>(width * height);

			Span<Color> pixels = buffer.AsSpan();
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
					pixels[y * width + x] = GraphicsInfo.texel(shade(x, y));
			}

			texture.SetPixels(buffer);

			GC.KeepAlive(buffer);

			texture.Apply();
			texture.filterMode = filterMode;
			texture.wrapMode = TextureWrapMode.Clamp;
			texture.hideFlags = HideFlags.HideAndDontSave;
			return texture;
		}

		internal static Texture2D createData(int width, int height, TextureFormat format, PixelShader value, string name)
		{
			Texture2D texture = new Texture2D(width, height, format, false, true);

			Il2CppStructArray<Color> buffer = new Il2CppStructArray<Color>(width * height);

			Span<Color> pixels = buffer.AsSpan();
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
					pixels[y * width + x] = value(x, y);
			}

			texture.SetPixels(buffer);

			GC.KeepAlive(buffer);

			texture.Apply(false, true);
			texture.name = name;
			texture.filterMode = FilterMode.Bilinear;
			texture.wrapMode = TextureWrapMode.Clamp;
			texture.hideFlags = HideFlags.HideAndDontSave;
			return texture;
		}

		internal static Texture2D createTiled(int size)
		{
			Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
			{
				filterMode = FilterMode.Trilinear,
				anisoLevel = Textures.kAnisoLevel,
				wrapMode = TextureWrapMode.Repeat,
				hideFlags = HideFlags.HideAndDontSave
			};
			return texture;
		}
	}
}
