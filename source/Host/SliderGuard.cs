using System;

namespace Trellis
{
	// typed values past a slider range are the host's to keep or clamp on save
	internal static class SliderGuard
	{
		private static Action m_flush;
		private static Action<string> m_release;
		private static Action<string, float, float, float> m_noteUnsafe;
		private static SafeDefault m_safeDefault;

		internal static void bind(Action flush, Action<string> release, Action<string, float, float, float> noteUnsafe, SafeDefault safeDefault)
		{
			SliderGuard.m_flush = flush;
			SliderGuard.m_release = release;
			SliderGuard.m_noteUnsafe = noteUnsafe;
			SliderGuard.m_safeDefault = safeDefault;
		}

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
