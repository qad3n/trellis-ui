using System.Text;
using UnityEngine;

namespace Trellis
{
	internal static partial class Widgets
	{
		internal static bool copyPressed(TextSelectionOwner owner)
		{
			if (TextSelectionFocus.m_current != owner)
				return false;

			Event current = Event.current;
			return current.type == EventType.KeyDown && current.keyCode == KeyCode.C && (current.control || current.command);
		}

		internal static void setClipboard(string text) => GUIUtility.systemCopyBuffer = text ?? string.Empty;

		internal static void copySelection(StringBuilder lines)
		{
			Widgets.setClipboard(lines.ToString().TrimEnd());

			Event.current.Use();
		}
	}
}
