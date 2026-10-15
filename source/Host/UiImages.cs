using System;
using UnityEngine;

namespace Trellis
{
	// the host owns where background images live, the framework never touches the disk
	internal static class UiImages
	{
		private static Func<string[]> m_list;
		private static ImageLoad m_load;

		internal static string hint { get; private set; } = string.Empty;

		internal static void bind(Func<string[]> list, ImageLoad load, string hint)
		{
			UiImages.m_list = list;
			UiImages.m_load = load;
			UiImages.hint = hint ?? string.Empty;
		}

		internal static string[] list() => UiImages.m_list?.Invoke() ?? Array.Empty<string>();

		internal static bool tryLoad(string name, TextureWrapMode wrapMode, out Texture2D texture, out string status, out ImagePreview preview)
		{
			if (UiImages.m_load != null)
				return UiImages.m_load(name, wrapMode, out texture, out status, out preview);

			texture = null;
			status = UiImages.hint;
			preview = null;
			return false;
		}
	}
}
