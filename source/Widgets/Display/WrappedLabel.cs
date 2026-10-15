using System.Collections.Generic;
using UnityEngine;

namespace Trellis
{
	internal static partial class Widgets
	{
		private static readonly Dictionary<GUIStyle, GUIStyle> kWrapStyles = new Dictionary<GUIStyle, GUIStyle>();

		private static int m_wrapVersion = -1;

		internal static GUIStyle wrapStyle(GUIStyle baseStyle)
		{
			if (Widgets.m_wrapVersion != Theme.layoutVersion)
			{
				Widgets.kWrapStyles.Clear();

				Widgets.m_wrapVersion = Theme.layoutVersion;
			}

			if (Widgets.kWrapStyles.TryGetValue(baseStyle, out GUIStyle cached))
				return cached;

			GUIStyle wrapped = Theme.derive(baseStyle);
			wrapped.wordWrap = true;
			wrapped.clipping = TextClipping.Clip;
			wrapped.alignment = TextAnchor.UpperLeft;
			Widgets.kWrapStyles[baseStyle] = wrapped;
			return wrapped;
		}

		internal static void wrappedLabel(float x, float y, float width, string text, GUIStyle baseStyle, out float height)
		{
			GUIStyle style = Widgets.wrapStyle(baseStyle);
			height = Widgets.measuredHeight(style, text, width);
			Gui.label(RectMath.create(x, y, width, height), text, style);
		}
	}
}
