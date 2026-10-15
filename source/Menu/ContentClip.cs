using System.Collections.Generic;
using UnityEngine;

namespace Trellis
{
	internal static class ContentClip
	{
		private static readonly List<Frame> kStack = new List<Frame>();

		private struct Frame
		{
			internal Vector2 m_blockOffset;
			internal Vector2 m_scrollMouse;
			internal bool m_enabled;
		}

		// hit rects dont line up mid reveal so drop input
		internal static void push(float y, float revealH)
		{
			Vector2 offset = MenuHost.m_blockOffset;

			Rect clip = RectMath.create(offset.x - MenuHost.kContentPad, offset.y + y, MenuHost.m_contentW + MenuHost.kContentPad * 2f, revealH);
			Gui.beginClip(clip);

			ContentClip.kStack.Add(new Frame
			{
				m_blockOffset = offset,
				m_scrollMouse = MenuHost.scrollMouse,
				m_enabled = Gui.enabled
			});

			MenuHost.m_blockOffset = VectorMath.v2(offset.x - RectMath.x(clip), offset.y - RectMath.y(clip));
			MenuHost.scrollMouse = MenuHost.kOffscreenMouse;

			Gui.enabled = false;
		}

		internal static void pop()
		{
			int top = ContentClip.kStack.Count - 1;
			if (top < 0)
				return;

			Gui.endClip();

			Frame frame = ContentClip.kStack[top];

			ContentClip.kStack.RemoveAt(top);

			ContentClip.restore(frame);
		}

		// a clip left on the gui stack corrupts every later control
		internal static void unwind()
		{
			if (ContentClip.kStack.Count == 0)
				return;

			for (int i = 0; i < ContentClip.kStack.Count; i++)
				Gui.endClip();

			Frame outermost = ContentClip.kStack[0];

			ContentClip.kStack.Clear();

			ContentClip.restore(outermost);
		}

		internal static void reset() => ContentClip.kStack.Clear();

		private static void restore(in Frame frame)
		{
			MenuHost.m_blockOffset = frame.m_blockOffset;
			MenuHost.scrollMouse = frame.m_scrollMouse;

			Gui.enabled = frame.m_enabled;
		}
	}
}
