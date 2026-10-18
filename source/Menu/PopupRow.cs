using System;
using UnityEngine;

namespace Trellis
{
	// every hook of this host popup is optional
	internal sealed class PopupRow
	{
		internal readonly Func<bool> m_open;
		internal readonly Func<float> m_alpha;
		internal readonly Action m_close;
		internal readonly Action<Rect, Vector2> m_dismiss;
		internal readonly Action<Rect, Vector2> m_draw;
		// most popups draw under the colour picker and a few sit on top of it
		internal readonly bool m_abovePicker;

		internal PopupRow(Func<bool> open = null, Func<float> alpha = null, Action close = null, Action<Rect, Vector2> dismiss = null, Action<Rect, Vector2> draw = null, bool abovePicker = false)
		{
			m_open = open;
			m_alpha = alpha;
			m_close = close;
			m_dismiss = dismiss;
			m_draw = draw;
			m_abovePicker = abovePicker;
		}
	}
}
