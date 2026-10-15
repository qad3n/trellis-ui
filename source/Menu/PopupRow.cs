using System;
using UnityEngine;

namespace Trellis
{
	// a host popup that lives over the content view, every hook is optional
	internal sealed class PopupRow
	{
		internal readonly Func<bool> m_open;
		internal readonly Func<float> m_alpha;
		internal readonly Action m_close;
		internal readonly Action<Rect, Vector2> m_dismiss;
		internal readonly Action<Rect, Vector2> m_draw;

		internal PopupRow(Func<bool> open = null, Func<float> alpha = null, Action close = null, Action<Rect, Vector2> dismiss = null, Action<Rect, Vector2> draw = null)
		{
			m_open = open;
			m_alpha = alpha;
			m_close = close;
			m_dismiss = dismiss;
			m_draw = draw;
		}
	}
}
