using System;
using UnityEngine;

namespace Trellis
{
	// the colour is read back every time so a theme rebuild reaches the chip
	internal readonly struct BadgeRow
	{
		internal readonly string m_label;
		internal readonly Func<Color> m_color;
		internal readonly string m_short;
		internal readonly string m_code;
		internal readonly int m_group;
		internal readonly bool m_counts;

		internal BadgeRow(string label, Func<Color> color, string shortLabel, string code, int group = BadgeGroup.kNone, bool counts = false)
		{
			m_label = label;
			m_color = color;
			m_short = shortLabel;
			m_code = code;
			m_group = group;
			m_counts = counts;
		}
	}
}
