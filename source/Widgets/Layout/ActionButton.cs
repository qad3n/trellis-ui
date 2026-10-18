using System;
using UnityEngine;

namespace Trellis
{
	internal readonly struct ActionButton
	{
		internal readonly string m_label;
		internal readonly GUIStyle m_style;
		internal readonly Action m_onClick;
		internal readonly bool m_disabled;
		internal readonly string m_tooltip;

		internal ActionButton(string label, GUIStyle style, Action onClick, bool disabled = false, string tooltip = null)
		{
			m_label = label;
			m_style = style;
			m_onClick = onClick;
			m_disabled = disabled;
			m_tooltip = tooltip;
		}
	}
}
