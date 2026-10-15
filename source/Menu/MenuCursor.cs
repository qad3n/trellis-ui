using UnityEngine;

namespace Trellis
{
	internal static partial class MenuHost
	{
		private const int kRestoreFrames = 3;

		private static Vector2 m_savedCursor;
		private static bool m_cursorSaved;
		private static int m_restoreUntil = -1;

		private static void onMenuOpened()
		{
			if (!MenuHost.m_cursorSaved)
				return;

			MenuHost.m_restoreUntil = Live.frameCount + MenuHost.kRestoreFrames;
		}

		private static void onMenuClosed()
		{
			MenuHost.m_savedCursor = VectorMath.flatten(Input.mousePosition);
			MenuHost.m_cursorSaved = true;
			MenuHost.m_restoreUntil = -1;
		}

		private static void restoreCursor()
		{
			if (MenuHost.m_restoreUntil < 0)
				return;

			Vector2 current = VectorMath.flatten(Input.mousePosition);
			CursorWarp.moveBy(VectorMath.sub(MenuHost.m_savedCursor, current));

			if (Live.frameCount < MenuHost.m_restoreUntil)
				return;

			MenuHost.m_restoreUntil = -1;
		}
	}
}
