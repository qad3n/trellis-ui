using System;
using UnityEngine;

namespace Trellis
{
	// one severity the host logs at, as the toast paints it
	internal readonly struct NotifyLevel
	{
		internal readonly Func<Color> m_accent;
		internal readonly Func<Texture2D> m_fade;
		internal readonly bool m_tintsBody;

		internal NotifyLevel(Func<Color> accent, Func<Texture2D> fade, bool tintsBody = false)
		{
			m_accent = accent;
			m_fade = fade;
			m_tintsBody = tintsBody;
		}
	}
}
