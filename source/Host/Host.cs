using System;
using UnityEngine;

namespace Trellis
{
	// the whole host contract in one place, bound once before the first OnGUI
	internal static class Host
	{
		// a null sink is ignored so a host only fills what it has
		internal sealed class Bindings
		{
			internal Action<string> m_debug;
			internal Action<string> m_info;
			internal Action<string> m_warn;
			internal Action<string> m_trace;
			internal Action<string, Exception> m_exception;
			internal Func<bool> m_debugEnabled;
			internal Func<Font> m_loadFont;
			internal Func<Font> m_resolvedFont;
			internal Func<string[]> m_listImages;
			internal ImageLoad m_loadImage;
			internal string m_imageHint;
			internal Action m_sliderFlush;
			internal Action<string> m_sliderRelease;
			internal Action<string, float, float, float> m_sliderUnsafe;
			internal SafeDefault m_sliderDefault;
			internal Func<bool> m_inputClaimed;
			internal int m_nameMarkupLimit;
			internal int m_nameVisibleLimit;
			internal BadgeRow[] m_badges;
			internal BadgeGroup[] m_badgeGroups;
			internal NotifyLevel[] m_levels;
			internal NotifyRef m_notifyRef;
		}

		private static NotifyRef m_notifyRef;

		internal static bool notifyRef(int id, out string plain, out string raw)
		{
			plain = null;
			raw = null;
			return Host.m_notifyRef != null && Host.m_notifyRef(id, out plain, out raw);
		}

		internal static void bind(Bindings host)
		{
			UiLog.m_debug = host.m_debug;
			UiLog.m_info = host.m_info;
			UiLog.m_warn = host.m_warn;
			UiLog.m_trace = host.m_trace;
			UiLog.m_exception = host.m_exception;
			UiLog.m_debugEnabled = host.m_debugEnabled;

			UiFont.m_load = host.m_loadFont;
			UiFont.m_resolved = host.m_resolvedFont;

			UiImages.m_list = host.m_listImages;
			UiImages.m_load = host.m_loadImage;
			UiImages.m_hint = host.m_imageHint ?? string.Empty;

			SliderGuard.m_flush = host.m_sliderFlush;
			SliderGuard.m_release = host.m_sliderRelease;
			SliderGuard.m_noteUnsafe = host.m_sliderUnsafe;
			SliderGuard.m_safeDefault = host.m_sliderDefault;

			if (host.m_nameMarkupLimit > 0 && host.m_nameVisibleLimit > 0)
				RichText.nameLimits(host.m_nameMarkupLimit, host.m_nameVisibleLimit);

			if (host.m_badges != null)
				Badges.rows(host.m_badges, host.m_badgeGroups);

			if (host.m_levels != null)
				Notifications.levels(host.m_levels);

			Host.m_notifyRef = host.m_notifyRef;

			if (host.m_inputClaimed == null)
				return;

			MenuHost.addClaim(host.m_inputClaimed);
		}
	}
}
