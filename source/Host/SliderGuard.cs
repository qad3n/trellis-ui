using System;

namespace Trellis
{
	// typed values past a slider range are the host's to keep or clamp on save
	internal static class SliderGuard
	{
		internal static Action m_flush;
		internal static Action<string> m_release;
		internal static Action<string, float, float, float> m_noteUnsafe;
		internal static SafeDefault m_safeDefault;

		internal static void flush() => SliderGuard.m_flush?.Invoke();
		internal static void release(string sliderId) => SliderGuard.m_release?.Invoke(sliderId);
		internal static void noteUnsafe(string sliderId, float value, float min, float max) => SliderGuard.m_noteUnsafe?.Invoke(sliderId, value, min, max);

		internal static bool trySafeDefault(string sliderId, out float value)
		{
			value = 0f;
			return SliderGuard.m_safeDefault != null && SliderGuard.m_safeDefault(sliderId, out value);
		}
	}
}
