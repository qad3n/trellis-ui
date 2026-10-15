using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Trellis
{
	internal static partial class MenuHost
	{
		internal const int kFirstExtra = 2;
		private const int kLastExtra = 6;

		private static EventSystem m_disabledEvents;
		private static int m_inputFrame = -1;
		private static int m_mouseFrame = -1;
		private static float m_mouseX;
		private static float m_mouseY;
		private static bool m_rawMouseDown0;
		private static bool m_mouseDown0Consumed;
		private static bool m_inputClaim;

		internal static bool mouseDown0 => MenuHost.m_rawMouseDown0 && !MenuHost.m_mouseDown0Consumed;
		internal static bool rawMouseDown0 => MenuHost.m_rawMouseDown0;

		internal static bool mouseDown1 { get; private set; }
		internal static bool mouseHeld0 { get; private set; }
		internal static int extraButton { get; private set; }
		internal static float scrollDelta { get; private set; }

		// an imgui window doesnt stop a ugui raycast so one click hits both
		private static readonly List<Func<bool>> kClaims = new List<Func<bool>>();
		internal static void consumeMouseDown0() => MenuHost.m_mouseDown0Consumed = true;
		internal static void consumeScroll() => MenuHost.scrollDelta = 0f;

		internal static void cacheInput()
		{
			int frame = Live.frameCount;
			if (MenuHost.m_inputFrame == frame)
			{
				MenuHost.m_rawMouseDown0 = false;
				MenuHost.m_mouseDown0Consumed = false;
				MenuHost.mouseDown1 = false;
				MenuHost.extraButton = -1;
				MenuHost.scrollDelta = 0f;
				return;
			}

			MenuHost.m_inputFrame = frame;
			MenuHost.primeMouse(frame);
			MenuHost.m_rawMouseDown0 = Input.GetMouseButtonDown(0);
			MenuHost.m_mouseDown0Consumed = false;
			MenuHost.mouseHeld0 = Input.GetMouseButton(0);
			MenuHost.mouseDown1 = Input.GetMouseButtonDown(1);
			MenuHost.extraButton = -1;
			for (int button = MenuHost.kFirstExtra; button <= MenuHost.kLastExtra; button++)
			{
				if (!Input.GetMouseButtonDown(button))
					continue;

				MenuHost.extraButton = button;
				break;
			}

			MenuHost.scrollDelta = Input.mouseScrollDelta.y;
		}

		internal static Vector2 baseMouse() => MenuHost.designMouse(Gui.baseScale());
		internal static Vector2 designMouse(float scale) => VectorMath.v2(MenuHost.m_mouseX / scale, (Gui.screenHeight - MenuHost.m_mouseY) / scale);
		internal static bool coversCursor() => MenuHost.coversPoint(MenuHost.scaledMouse());

		internal static void addClaim(Func<bool> claimed) => MenuHost.kClaims.Add(claimed);

		private static bool overlayClaimed()
		{
			foreach (Func<bool> claimed in MenuHost.kClaims)
			{
				if (claimed())
					return true;
			}
			return false;
		}

		internal static void applyClaims()
		{
			if (MenuHost.m_inputClaim || MenuHost.overlayClaimed())
			{
				if (Live.isUnityAlive(MenuHost.m_disabledEvents))
					return;

				EventSystem eventSystem = EventSystem.current;
				if (Live.isUnityAlive(eventSystem) && eventSystem.enabled)
				{
					eventSystem.enabled = false;
					MenuHost.m_disabledEvents = eventSystem;
				}
				return;
			}

			MenuHost.enableEvents();
		}

		private static void primeMouse(int frame)
		{
			if (MenuHost.m_mouseFrame == frame)
				return;

			MenuHost.m_mouseFrame = frame;

			Vector3 mouse = Input.mousePosition;
			MenuHost.m_mouseX = mouse.x;
			MenuHost.m_mouseY = mouse.y;
		}

		internal static Vector2 scaledMouse() => MenuHost.designMouse(Gui.designScale());

		private static bool coversPoint(Vector2 mouse)
		{
			if (!MenuHost.m_visible)
				return false;

			return RectMath.contains(RectMath.create(MenuHost.m_winX, MenuHost.m_winY, MenuHost.winW, MenuHost.winH), mouse);
		}

		private static void blockInput()
		{
			Vector2 mouse = MenuHost.scaledMouse();

			Rect winRect = RectMath.create(MenuHost.m_winX, MenuHost.m_winY, MenuHost.winW, MenuHost.winH);
			MenuHost.setClaim(RectMath.contains(winRect, mouse) || MenuHost.popupShown);
		}

		private static void setClaim(bool claimed)
		{
			MenuHost.m_inputClaim = claimed;
			MenuHost.applyClaims();
		}

		private static void restoreInput() => MenuHost.setClaim(false);

		private static void enableEvents()
		{
			EventSystem saved = MenuHost.m_disabledEvents;

			MenuHost.m_disabledEvents = null;

			if (!Live.isUnityAlive(saved))
				return;

			try
			{
				saved.enabled = true;
			}
			catch (Exception ex)
			{
				UiLog.logException("MenuHost.enableEvents", ex);
			}
		}
	}
}
