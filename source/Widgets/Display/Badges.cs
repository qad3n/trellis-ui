using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Trellis
{
	// chips the host fills with its own badge rows, drawn right to left in a header or a row
	internal static class Badges
	{
		internal const float kHeight = 18f;
		internal const float kGap = 6f;
		private const float kTextPad = 14f;
		private const float kFillShade = 0.62f;
		private const float kBorderAlpha = 0.55f;
		// a counting row reads "ADMIN +3" up to this many extras
		private const int kMaxExtra = 13;

		private static readonly StringBuilder kGroupText = new StringBuilder();
		private static BadgeRow[] m_rows = Array.Empty<BadgeRow>();
		private static BadgeGroup[] m_groups = Array.Empty<BadgeGroup>();
		private static string[,] m_labels = new string[0, 0];
		private static CachedMarkup[,] m_markups = new CachedMarkup[0, 0];

		private struct CachedMarkup
		{
			internal string m_hex;
			internal string m_text;
		}

		internal static void rows(BadgeRow[] rows, BadgeGroup[] groups)
		{
			Badges.m_rows = rows;
			Badges.m_groups = groups ?? Array.Empty<BadgeGroup>();
			Badges.m_labels = new string[rows.Length, Badges.kMaxExtra + 1];
			Badges.m_markups = new CachedMarkup[rows.Length, Badges.kMaxExtra + 1];
		}

		internal static Color color(int badge) => Badges.row(badge).m_color?.Invoke() ?? Theme.m_accent;

		internal static string label(int badge, int extra = 0)
		{
			if (!Badges.known(badge))
				return string.Empty;

			int column = Badges.extraColumn(badge, extra);
			return Badges.m_labels[badge, column] ??= column == 0 ? Badges.m_rows[badge].m_label : $"{Badges.m_rows[badge].m_label} +{column}";
		}

		internal static string markup(int badge, int extra = 0)
		{
			if (!Badges.known(badge))
				return string.Empty;

			int column = Badges.extraColumn(badge, extra);

			Color color = Badges.color(badge);

			string hex = ColorHex.of(color);

			ref CachedMarkup cached = ref Badges.m_markups[badge, column];
			if (object.ReferenceEquals(cached.m_hex, hex))
				return cached.m_text;

			cached.m_hex = hex;
			cached.m_text = Theme.tag($"<b>[{Badges.label(badge, column)}]</b>", color);
			return cached.m_text;
		}

		internal static float width(string text) => UiText.measuredWidth(Theme.badgeLabel, text) + Badges.kTextPad;
		internal static float rowY(Rect headerRect) => RectMath.y(headerRect) + (RectMath.h(headerRect) - Badges.kHeight) * 0.5f;

		internal static float draw(float x, float y, string text, Color color, bool rightAligned = false)
		{
			float width = Badges.width(text);

			float drawX = rightAligned ? x - width : x;

			Rect box = RectMath.create(drawX, y, width, Badges.kHeight);
			Badges.drawChip(box, color);

			Gui.label(box, text, Theme.badgeLabel);
			return rightAligned ? drawX - Badges.kGap : drawX + width + Badges.kGap;
		}

		// the row ends at rightX and grows leftwards, so the last badge sits furthest out
		internal static float drawRow<TBadge>(float rightX, float y, List<TBadge> badges, int extra = 0) where TBadge : unmanaged, Enum
		{
			float x = rightX;
			for (int i = badges.Count - 1; i >= 0; i--)
			{
				int badge = MenuHost.key(badges[i]);
				x = Badges.draw(x, y, Badges.label(badge, extra), Badges.color(badge), true);
			}
			return x;
		}

		internal static void appendMarkup<TBadge>(List<string> segments, List<TBadge> badges, int extra = 0) where TBadge : unmanaged, Enum
		{
			for (int i = 0; i < badges.Count; i++)
				segments.Add(Badges.markup(MenuHost.key(badges[i]), extra));
		}

		internal static void appendMarkup<TBadge>(List<string> segments, List<TBadge> badges, int extra, BadgeStyle style) where TBadge : unmanaged, Enum
		{
			switch (style)
			{
				case BadgeStyle.Grouped:
					Badges.appendGrouped(segments, badges, extra);
					break;
				case BadgeStyle.Codes:
					if (badges.Count > 0)
						segments.Add(Badges.codesMarkup(badges, extra));
					break;
				default:
					Badges.appendMarkup(segments, badges, extra);
					break;
			}
		}

		// the title reflows to whatever room the chips leave
		internal static void drawPopupHeader<TBadge>(Rect popupRect, float headerH, float pad, float accentH, string title, GUIStyle titleStyle, List<TBadge> badges, int extra = 0) where TBadge : unmanaged, Enum
		{
			Rect headerRect = UiAt.popupBand(popupRect, headerH, accentH);

			float badgesLeft = RectMath.xMax(headerRect) - pad;
			if (badges.Count > 0)
				badgesLeft = Badges.drawRow(badgesLeft, Badges.rowY(headerRect), badges, extra);

			UiAt.popupTitle(headerRect, pad, badgesLeft, title, titleStyle);
		}

		// a raw texture fill stayed unsnapped while the shader border snapped
		internal static void drawChip(Rect rect, Color color) => Theme.drawRoundedBox(rect, ColorMath.shade(color, Badges.kFillShade), ColorMath.rgba(0f, 0f, 0f, Badges.kBorderAlpha), Theme.cornerR, Theme.borderWidth);

		private static bool known(int badge) => badge >= 0 && badge < Badges.m_rows.Length;
		private static BadgeRow row(int badge) => Badges.known(badge) ? Badges.m_rows[badge] : default;

		private static string prefix(int group) => group >= 0 && group < Badges.m_groups.Length ? Badges.m_groups[group].m_prefix : string.Empty;

		// a group merges into the bracket of its first badge and every part keeps its own colour
		private static void appendGrouped<TBadge>(List<string> segments, List<TBadge> badges, int extra) where TBadge : unmanaged, Enum
		{
			for (int i = 0; i < badges.Count; i++)
			{
				int group = Badges.row(MenuHost.key(badges[i])).m_group;
				if (Badges.groupSeen(badges, i, group))
					continue;

				segments.Add(Badges.groupMarkup(badges, i, group, extra));
			}
		}

		private static bool groupSeen<TBadge>(List<TBadge> badges, int index, int group) where TBadge : unmanaged, Enum
		{
			if (group == BadgeGroup.kNone)
				return false;

			for (int i = 0; i < index; i++)
			{
				if (Badges.row(MenuHost.key(badges[i])).m_group == group)
					return true;
			}
			return false;
		}

		private static string groupMarkup<TBadge>(List<TBadge> badges, int start, int group, int extra) where TBadge : unmanaged, Enum
		{
			StringBuilder text = Badges.kGroupText.Clear();
			text.Append('[').Append(Badges.prefix(group));
			for (int i = start; i < badges.Count; i++)
			{
				int badge = MenuHost.key(badges[i]);
				if (i > start && (group == BadgeGroup.kNone || Badges.row(badge).m_group != group))
					continue;

				text.Append(Theme.tag(Badges.part(badge, Badges.row(badge).m_short, extra), Badges.color(badge)));
			}
			text.Append(']');
			return $"<b>{Theme.tag(text.ToString(), Badges.color(MenuHost.key(badges[start])))}</b>";
		}

		// every badge shares one neutral bracket and only the colour tells codes apart
		private static string codesMarkup<TBadge>(List<TBadge> badges, int extra) where TBadge : unmanaged, Enum
		{
			StringBuilder text = Badges.kGroupText.Clear();
			text.Append('[');
			for (int i = 0; i < badges.Count; i++)
			{
				int badge = MenuHost.key(badges[i]);
				text.Append(Theme.tag(Badges.part(badge, Badges.row(badge).m_code, extra), Badges.color(badge)));
			}
			text.Append(']');
			return $"<b>{Theme.tag(text.ToString(), Theme.Text.m_secondary)}</b>";
		}

		private static string part(int badge, string text, int extra)
		{
			int column = Badges.extraColumn(badge, extra);
			return column == 0 ? text : $"{text}+{column}";
		}

		private static int extraColumn(int badge, int extra)
		{
			if (extra <= 0 || !Badges.row(badge).m_counts)
				return 0;

			return extra > Badges.kMaxExtra ? Badges.kMaxExtra : extra;
		}
	}
}
