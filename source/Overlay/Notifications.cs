using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Trellis
{
	internal static class Notifications
	{
		private const float kDefaultMinRead = 1.5f;
		private const int kMaxDigits = 9;

		private static readonly List<Entry> kActive = new List<Entry>();
		private static readonly Queue<Pending> kPending = new Queue<Pending>();
		private static readonly object kLockObject = new object();

		[Config(0x4AF05E50EBC71F5B)]
		internal static float NotifScale = 1f;

		[Config(0x684E0E0C5E034F95)]
		internal static float NotifMinRead = Notifications.kDefaultMinRead;

		[Config(0x1FE9E1EF36A2FF18)]
		internal static bool NotifAnimations = true;

		[Config(0x5327248FE1E1FB0F)]
		internal static int NotifMaxVisible = Notifications.Capacity.kDefaultVisible;

		private static NotifyLevel[] m_levels = Array.Empty<NotifyLevel>();
		private static float m_cardExtent = MotionMath.extent(Layout.kNotifW, Layout.kNotifPadY * 2f);

		internal static class Capacity
		{
			internal const int kDefaultVisible = 5;
			internal const int kMinVisible = 1;
			internal const int kMaxVisible = 16;
			internal const int kMaxPending = 64;
		}

		private static class Timing
		{
			internal const float kDuration = 5f;
		}

		private static class Layout
		{
			internal const float kNotifW = 440f;
			internal const float kNotifMargin = 16f;
			internal const float kNotifGap = 6f;
			internal const float kNotifPadX = 12f;
			internal const float kNotifPadY = 10f;
			internal const float kLeftBarW = 3f;
			internal const float kHeaderGap = 3f;
			internal const float kWashAlpha = 0.28f;
			internal const float kShadowAlpha = 0.85f;
			internal const float kPluckAlpha = 0.9f;
			internal const float kGlowAlpha = 0.4f;
			internal const float kEdgeClearance = 24f;
		}

		// what a host hands over for one toast
		private readonly struct Pending
		{
			internal readonly int m_level;
			internal readonly string m_tag;
			internal readonly string m_message;
			internal readonly int m_count;

			internal Pending(int level, string tag, string message, int count)
			{
				m_level = level;
				m_tag = tag;
				m_message = message;
				m_count = count;
			}
		}

		private class Entry
		{
			internal float m_spawnTime;
			internal string m_header;
			internal string m_body;
			internal string m_rawMessage;
			internal int m_level;
			internal bool m_placed;
			internal float m_duration;
			internal bool m_releasing;
			internal MotionValue m_slide;
			internal MotionValue m_light;
			internal MotionValue m_y;
			internal MotionValue m_pluck;
			internal readonly GUIContent m_headerContent = new GUIContent();
			internal readonly GUIContent m_bodyContent = new GUIContent();
			internal readonly GUIContent m_shadowContent = new GUIContent();
		}

		private static bool animate => Notifications.NotifAnimations && AnimationConfig.Enabled;
		private static int maxVisible => FastMath.clamp(Notifications.NotifMaxVisible, Capacity.kMinVisible, Capacity.kMaxVisible);
		private static float slideDistance => Layout.kNotifW + Layout.kNotifMargin + Layout.kEdgeClearance;

		internal static void levels(NotifyLevel[] rows) => Notifications.m_levels = rows;

		// callable from any thread since the host logs off the main thread
		internal static void push(int level, string tag, string message, int count = 1)
		{
			if (string.IsNullOrEmpty(message))
				return;

			lock (Notifications.kLockObject)
				Notifications.enqueuePending(new Pending(level, tag, message, count));
		}

		internal static void onGUI()
		{
			if (!Gui.repainting)
				return;

			Notifications.drainPending();

			if (Notifications.kActive.Count == 0)
				return;

			Theme.init();

			Matrix4x4 prevMatrix = Gui.beginBase(out float scale, Notifications.NotifScale);

			float virtualW = Gui.screenWidth / scale;

			Color prevColor = Gui.color;

			float time = Gui.unscaledTime;

			float restX = virtualW - Layout.kNotifW - Layout.kNotifMargin;

			float targetY = Layout.kNotifMargin;

			for (int i = Notifications.kActive.Count - 1; i >= 0; i--)
			{
				Entry notif = Notifications.kActive[i];

				if (!notif.m_releasing && time - notif.m_spawnTime >= notif.m_duration)
					Notifications.release(notif);

				float slide = notif.m_slide.sample();
				if (notif.m_releasing && notif.m_slide.atRest)
				{
					Notifications.kActive.RemoveAt(i);
					continue;
				}

				float alpha = notif.m_light.sampleLight();

				float x = restX + slide * Notifications.slideDistance;

				float textW = Layout.kNotifW - Layout.kNotifPadX * 2 - Layout.kLeftBarW;

				float headerH = UiText.measuredHeight(Theme.notifHeader, notif.m_header, notif.m_headerContent, textW);

				float bodyH = UiText.measuredHeight(Theme.notifLabel, notif.m_body, notif.m_bodyContent, textW);

				float notifH = Layout.kNotifPadY + headerH + Layout.kHeaderGap + bodyH + Layout.kNotifPadY;

				Notifications.m_cardExtent = MotionMath.extent(Layout.kNotifW, notifH);
				if (notif.m_placed)
					notif.m_y.reel(targetY, MotionProfile.body(Notifications.m_cardExtent, 1f, Notifications.animate));
				else
					notif.m_y = MotionValue.at(targetY);
				notif.m_placed = true;

				float y = notif.m_y.sample();
				if (alpha > 0f)
				{
					Rect boxRect = RectMath.create(x, y, Layout.kNotifW, notifH);

					Rect inner = Theme.windowInner(boxRect);

					float washX = RectMath.x(inner) + Layout.kLeftBarW;

					float washW = RectMath.w(inner) - Layout.kLeftBarW;

					float contentX = washX + Layout.kNotifPadX;

					Gui.color = ColorMath.withAlpha(ColorMath.kWhite, alpha);

					Theme.drawWindowBg(boxRect);

					float pluck = notif.m_pluck.sampleLight();
					if (pluck > 0f)
						Notifications.drawPluck(boxRect, notif.m_level, pluck);

					Theme.drawWindowRegion(boxRect, RectMath.create(washX, RectMath.y(inner), washW, RectMath.h(inner)), Notifications.levelFade(notif.m_level), ColorMath.withAlpha(ColorMath.kWhite, Layout.kWashAlpha));
					Theme.drawWindowRegion(boxRect, RectMath.create(RectMath.x(inner), RectMath.y(inner), Layout.kLeftBarW, RectMath.h(inner)), Notifications.levelAccent(notif.m_level));

					Rect headerRect = RectMath.create(contentX, y + Layout.kNotifPadY, textW, headerH);

					Rect bodyRect = RectMath.create(contentX, y + Layout.kNotifPadY + headerH + Layout.kHeaderGap, textW, bodyH);

					Color shadowColor = ColorMath.rgba(0f, 0f, 0f, alpha * Layout.kShadowAlpha);
					Gui.drawShadow(headerRect, notif.m_headerContent, Theme.notifHeader, shadowColor);
					Gui.drawShadow(bodyRect, notif.m_shadowContent, Theme.notifLabel, shadowColor);
					Gui.color = ColorMath.withAlpha(ColorMath.kWhite, alpha);
					Gui.label(headerRect, notif.m_headerContent, Theme.notifHeader);
					Gui.color = ColorMath.withAlpha(Notifications.bodyColor(notif.m_level), alpha);
					Gui.label(bodyRect, notif.m_bodyContent, Theme.notifLabel);
				}

				targetY += notifH + Layout.kNotifGap;
			}
			Gui.color = prevColor;
			Gui.restoreGui(prevMatrix);
		}

		internal static void cleanup()
		{
			Notifications.kActive.Clear();

			lock (Notifications.kLockObject)
				Notifications.kPending.Clear();
		}

		private static void enqueuePending(in Pending entry)
		{
			if (Notifications.kPending.Count >= Capacity.kMaxPending)
				Notifications.kPending.Dequeue();

			Notifications.kPending.Enqueue(entry);
		}

		private static void drainPending()
		{
			float time = Gui.unscaledTime;
			lock (Notifications.kLockObject)
			{
				while (Notifications.kPending.Count > 0)
				{
					if (Notifications.countLive() >= Notifications.maxVisible)
					{
						Entry oldest = Notifications.oldestLive();
						if (oldest == null)
							break;

						if (!oldest.m_slide.atRest || time - oldest.m_spawnTime < Notifications.NotifMinRead)
							break;

						Notifications.release(oldest);
					}

					Pending entry = Notifications.kPending.Dequeue();

					int duplicateIndex = Notifications.findDuplicate(entry);

					string body = Notifications.colorizeRefs(entry.m_count > 1 ? $"{entry.m_message} (x{entry.m_count})" : entry.m_message);

					float duration = Notifications.duration();
					if (duplicateIndex >= 0 && Notifications.pluck(Notifications.kActive[duplicateIndex], body, duration, time))
					{
						Notifications.promote(duplicateIndex);
						continue;
					}

					Entry notifEntry = new Entry
					{
						m_spawnTime = time,
						m_header = entry.m_tag,
						m_rawMessage = entry.m_message,
						m_level = entry.m_level,
						m_duration = duration,
						m_slide = MotionValue.at(1f),
						m_light = MotionValue.at(0f),
						m_pluck = MotionValue.at(0f)
					};
					notifEntry.m_slide.reel(0f, MotionProfile.body(Notifications.m_cardExtent, Notifications.slideDistance, Notifications.animate));
					notifEntry.m_light.reel(1f, MotionProfile.light(Notifications.animate));

					notifEntry.m_headerContent.text = notifEntry.m_header;
					Notifications.setBody(notifEntry, body);

					if (duplicateIndex >= 0)
					{
						notifEntry.m_placed = true;
						notifEntry.m_y = Notifications.kActive[duplicateIndex].m_y;

						Notifications.kActive.RemoveAt(duplicateIndex);
					}
					Notifications.kActive.Add(notifEntry);
				}
			}
		}

		private static int findDuplicate(in Pending entry)
		{
			for (int i = Notifications.kActive.Count - 1; i >= 0; i--)
			{
				if (Notifications.kActive[i].m_rawMessage == entry.m_message && Notifications.kActive[i].m_level == entry.m_level)
					return i;
			}
			return -1;
		}

		private static void promote(int index)
		{
			Entry notif = Notifications.kActive[index];

			Notifications.kActive.RemoveAt(index);
			Notifications.kActive.Add(notif);
		}

		private static float duration()
		{
			int pressureCount = Notifications.kActive.Count + Notifications.kPending.Count + 1;

			int visible = Notifications.maxVisible;

			float load = FastMath.clamp01((pressureCount - visible) / (float)visible);

			MotionProfile body = MotionProfile.body(Notifications.m_cardExtent, Notifications.slideDistance, true);

			float durationFloor = MotionMath.settleSeconds(body.m_omega, body.m_damping) + Notifications.NotifMinRead;
			return FastMath.lerp(Timing.kDuration, durationFloor, load);
		}

		private static void release(Entry notif)
		{
			notif.m_releasing = true;
			notif.m_slide.vent(1f, MotionProfile.body(Notifications.m_cardExtent, Notifications.slideDistance, Notifications.animate));
		}

		// colour tags override the shadow tint
		private static void setBody(Entry notif, string body)
		{
			notif.m_body = body;
			notif.m_bodyContent.text = body;
			notif.m_shadowContent.text = RichText.stripColors(body);
		}

		private static bool pluck(Entry notif, string body, float duration, float time)
		{
			if (notif.m_releasing)
				return false;

			Notifications.setBody(notif, body);

			notif.m_duration = duration;
			notif.m_spawnTime = time;
			notif.m_pluck = MotionValue.at(1f);
			notif.m_pluck.vent(0f, MotionProfile.release(Notifications.m_cardExtent, Notifications.animate));
			return true;
		}

		private static int countLive()
		{
			int count = 0;
			for (int i = 0; i < Notifications.kActive.Count; i++)
			{
				if (Notifications.kActive[i].m_releasing)
					continue;

				count++;
			}
			return count;
		}

		private static Entry oldestLive()
		{
			for (int i = 0; i < Notifications.kActive.Count; i++)
			{
				Entry notif = Notifications.kActive[i];
				if (!notif.m_releasing)
					return notif;
			}
			return null;
		}

		private static void drawPluck(Rect boxRect, int level, float pluck)
		{
			Color color = Notifications.levelAccent(level);
			Theme.drawHalo(boxRect, ColorMath.withAlpha(color, pluck * Layout.kGlowAlpha));
			Theme.drawOutline(boxRect, ColorMath.withAlpha(color, pluck * Layout.kPluckAlpha));
		}

		private static string colorizeRefs(string message)
		{
			if (string.IsNullOrEmpty(message))
				return message;

			StringBuilder builder = new StringBuilder(message.Length + 32);

			int length = message.Length;

			int i = 0;
			while (i < length)
			{
				if (message[i] == '[' && i + 1 < length && char.IsDigit(message[i + 1]))
				{
					int j = i + 1;
					while (j < length && char.IsDigit(message[j]))
						j++;

					if (j < length && message[j] == ']' && j - i - 1 <= Notifications.kMaxDigits)
					{
						string idText = message.Substring(i + 1, j - i - 1);
						if (int.TryParse(idText, out int id))
						{
							builder.Append(Theme.tag("[", Theme.Text.m_secondary));
							builder.Append(Theme.tag(idText, Theme.m_accent));
							builder.Append(Theme.tag("]", Theme.Text.m_secondary));

							i = j + 1;

							if (i < length && message[i] == ' ' && Host.notifyRef(id, out string name, out string raw))
							{
								int nameStart = i + 1;
								if (nameStart + name.Length <= length && message.AsSpan(nameStart, name.Length).SequenceEqual(name))
								{
									builder.Append(' ');
									builder.Append(string.IsNullOrEmpty(raw) ? name : raw);

									i = nameStart + name.Length;
								}
							}
							continue;
						}
					}
				}
				builder.Append(message[i]);

				i++;
			}
			return builder.ToString();
		}

		private static Color levelAccent(int level) => Notifications.row(level).m_accent?.Invoke() ?? Theme.m_accent;
		private static Texture2D levelFade(int level) => Notifications.row(level).m_fade?.Invoke();
		private static Color bodyColor(int level) => Notifications.row(level).m_tintsBody ? Notifications.levelAccent(level) : Theme.Text.m_primary;

		private static NotifyLevel row(int level) => level >= 0 && level < Notifications.m_levels.Length ? Notifications.m_levels[level] : default;
	}
}
