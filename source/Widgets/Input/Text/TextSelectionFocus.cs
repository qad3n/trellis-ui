using UnityEngine;

namespace Trellis
{
	internal static class TextSelectionFocus
	{
		internal static TextSelectionOwner m_current;

		internal static void claim(TextSelectionOwner owner)
		{
			if (TextSelectionFocus.m_current == owner)
				return;

			TextSelectionFocus.m_current?.clear();
			TextSelectionFocus.m_current = owner;

			GUIUtility.keyboardControl = 0;
		}

		internal static void release(TextSelectionOwner owner)
		{
			if (TextSelectionFocus.m_current != owner)
				return;

			TextSelectionFocus.m_current = null;
		}
	}
}
