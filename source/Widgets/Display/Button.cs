using UnityEngine;
using static Trellis.MenuHost;

namespace Trellis
{
	internal static partial class Widgets
	{
		internal static bool scrollButton(Rect rect, string text, GUIStyle style) => MenuAnimation.drawButton(rect, text, style, MenuHost.scrollMouse);
		internal static bool scaledButton(Rect rect, string text, GUIStyle style, Vector2 mouse) => MenuAnimation.drawButton(rect, text, style, mouse);
	}
}
