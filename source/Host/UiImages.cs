using System;
using UnityEngine;

namespace Trellis
{
	// the host owns where background images live so the framework never touches the disk
	internal static class UiImages
	{
		internal static Func<string[]> m_list;
		internal static ImageLoad m_load;
		internal static string m_hint = string.Empty;

		internal static string hint => UiImages.m_hint;

		internal static string[] list() => UiImages.m_list?.Invoke() ?? Array.Empty<string>();

		internal static bool tryLoad(string name, TextureWrapMode wrapMode, bool linear, out Texture2D texture, out string status, out ImagePreview preview)
		{
			if (UiImages.m_load != null)
				return UiImages.m_load(name, wrapMode, linear, out texture, out status, out preview);

			texture = null;
			status = UiImages.hint;
			preview = null;
			return false;
		}
	}
}
