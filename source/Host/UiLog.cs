using System;

namespace Trellis
{
	// the host owns every sink so the framework never writes a log of its own
	internal static class UiLog
	{
		internal static Action<string> m_debug;
		internal static Action<string> m_info;
		internal static Action<string> m_warn;
		internal static Action<string> m_trace;
		internal static Action<string, Exception> m_exception;
		internal static Func<bool> m_debugEnabled;

		internal static bool debugEnabled => UiLog.m_debugEnabled != null && UiLog.m_debugEnabled();
		internal static void debug(string message) => UiLog.m_debug?.Invoke(message);
		internal static void info(string message) => UiLog.m_info?.Invoke(message);
		internal static void warn(string message) => UiLog.m_warn?.Invoke(message);
		internal static void trace(string location) => UiLog.m_trace?.Invoke(location);
		internal static void logException(string location, Exception exception) => UiLog.m_exception?.Invoke(location, exception);
	}
}
