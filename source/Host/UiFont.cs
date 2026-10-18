using System;
using UnityEngine;

namespace Trellis
{
	// the host ships and loads the font and the framework only asks for it
	internal static class UiFont
	{
		internal static Func<Font> m_load;
		internal static Func<Font> m_resolved;

		// null falls back to the imgui default font
		internal static Font load() => UiFont.m_load?.Invoke();

		internal static Font resolved => UiFont.m_resolved?.Invoke();
	}
}
