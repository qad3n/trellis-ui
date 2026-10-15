using System;
using UnityEngine;

namespace Trellis
{
	internal sealed class DerivedStyle
	{
		private const int kUnsetSize = -1;

		private readonly Func<GUIStyle> m_source;
		private readonly Action<GUIStyle> m_configure;
		private GUIStyle m_style;
		private GUIStyle m_builtFrom;
		private int m_fontSize = DerivedStyle.kUnsetSize;

		internal DerivedStyle(Func<GUIStyle> source, Action<GUIStyle> configure = null)
		{
			m_source = source;
			m_configure = configure;
		}

		internal GUIStyle get()
		{
			GUIStyle from = m_source();
			if (m_style != null && object.ReferenceEquals(from, m_builtFrom))
				return m_style;

			m_style = Theme.derive(from);
			m_configure?.Invoke(m_style);

			m_builtFrom = from;
			m_fontSize = DerivedStyle.kUnsetSize;
			return m_style;
		}

		internal GUIStyle get(int size)
		{
			GUIStyle derived = get();
			if (m_fontSize != size)
			{
				derived.fontSize = size;

				m_fontSize = size;
			}
			return derived;
		}
	}
}
