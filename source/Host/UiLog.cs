using System;

namespace Trellis
{
	// the host owns every sink so the framework never writes a log of its own
	internal static class UiLog
	{
		private static Action<string> m_debug;
		private static Action<string> m_info;
		private static Action<string> m_warn;
		private static Action<string> m_trace;
		private static Action<string, Exception> m_exception;
		private static Func<bool> m_debugEnabled;

		internal static void bind(Action<string> debug, Action<string> info, Action<string> warn, Action<string> trace, Action<string, Exception> exception, Func<bool> debugEnabled)
		{
			UiLog.m_debug = debug;
			UiLog.m_info = info;
			UiLog.m_warn = warn;
			UiLog.m_trace = trace;
			UiLog.m_exception = exception;
			UiLog.m_debugEnabled = debugEnabled;
		}

		internal static bool debugEnabled => UiLog.m_debugEnabled != null && UiLog.m_debugEnabled();
		internal static void debug(string message) => UiLog.m_debug?.Invoke(message);
		internal static void info(string message) => UiLog.m_info?.Invoke(message);
		internal static void warn(string message) => UiLog.m_warn?.Invoke(message);
		internal static void trace(string location) => UiLog.m_trace?.Invoke(location);
		internal static void logException(string location, Exception exception) => UiLog.m_exception?.Invoke(location, exception);
	}
}
