using System;
using UnityEngine;

namespace Trellis
{
	// the host ships and loads the font, the framework only asks for it
	internal static class UiFont
	{
		private static Func<Font> m_load;
		private static Func<Font> m_resolved;

		internal static void bind(Func<Font> load, Func<Font> resolved)
		{
			UiFont.m_load = load;
			UiFont.m_resolved = resolved;
		}

		// null falls back to the imgui default font
		internal static Font load() => UiFont.m_load?.Invoke();

		internal static Font resolved => UiFont.m_resolved?.Invoke();
	}
}
