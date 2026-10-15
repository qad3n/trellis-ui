using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Trellis
{
	internal static class CursorWarp
	{
		private static IntPtr m_x11Display = IntPtr.Zero;
		private static bool m_displayResolved;

		private static class Win32
		{
			private const string kUser32 = "user32.dll";

			[StructLayout(LayoutKind.Sequential)]
			internal struct ScreenPoint
			{
				internal int m_x;
				internal int m_y;
			}

			[DllImport(Win32.kUser32)]
			internal static extern bool GetCursorPos(out ScreenPoint point);

			[DllImport(Win32.kUser32)]
			internal static extern bool SetCursorPos(int x, int y);
		}

		private static class X11
		{
			private const string kLibX11 = "libX11.so.6";

			[DllImport(X11.kLibX11)]
			internal static extern IntPtr XOpenDisplay(IntPtr display);

			[DllImport(X11.kLibX11)]
			internal static extern int XWarpPointer(IntPtr display, IntPtr sourceWindow, IntPtr destinationWindow, int sourceX, int sourceY, uint sourceWidth, uint sourceHeight, int destinationX, int destinationY);

			[DllImport(X11.kLibX11)]
			internal static extern int XFlush(IntPtr display);
		}

		internal static void moveBy(Vector2 unityDelta)
		{
			int deltaX = FastMath.roundToInt(unityDelta.x);

			int deltaY = FastMath.roundToInt(-unityDelta.y);
			if (deltaX == 0 && deltaY == 0)
				return;

			switch (Application.platform)
			{
				case RuntimePlatform.WindowsPlayer:
				case RuntimePlatform.WindowsEditor:
					CursorWarp.warpWindows(deltaX, deltaY);
					break;

				case RuntimePlatform.LinuxPlayer:
				case RuntimePlatform.LinuxEditor:
					CursorWarp.warpLinux(deltaX, deltaY);
					break;
			}
		}

		private static void warpWindows(int deltaX, int deltaY)
		{
			try
			{
				if (!Win32.GetCursorPos(out Win32.ScreenPoint current))
					return;

				Win32.SetCursorPos(current.m_x + deltaX, current.m_y + deltaY);
			}
			catch (Exception ex)
			{
				UiLog.debug($"[CursorWarp.warpWindows] {ex.Message}");
			}
		}

		private static void warpLinux(int deltaX, int deltaY)
		{
			try
			{
				IntPtr display = CursorWarp.openDisplay();
				if (display == IntPtr.Zero)
					return;

				X11.XWarpPointer(display, IntPtr.Zero, IntPtr.Zero, 0, 0, 0, 0, deltaX, deltaY);
				X11.XFlush(display);
			}
			catch (Exception ex)
			{
				UiLog.debug($"[CursorWarp.warpLinux] {ex.Message}");
			}
		}

		private static IntPtr openDisplay()
		{
			if (CursorWarp.m_displayResolved)
				return CursorWarp.m_x11Display;

			CursorWarp.m_displayResolved = true;

			try
			{
				CursorWarp.m_x11Display = X11.XOpenDisplay(IntPtr.Zero);
			}
			catch (Exception ex)
			{
				UiLog.debug($"[CursorWarp.openDisplay] {ex.Message}");
			}
			return CursorWarp.m_x11Display;
		}
	}
}
