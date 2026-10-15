using UnityEngine;

namespace Trellis
{
	internal struct WindowDragState
	{
		internal bool m_dragging;
		internal int m_resizeEdges;
		private Vector2 m_grab;
		private Rect m_start;

		internal readonly bool active => m_dragging || m_resizeEdges != 0;

		internal void beginMove(Vector2 mouse, Rect window)
		{
			m_dragging = true;
			m_resizeEdges = 0;
			m_grab = VectorMath.v2(mouse.x - RectMath.x(window), mouse.y - RectMath.y(window));
		}

		internal void beginResize(int edges, Vector2 mouse, Rect window)
		{
			m_dragging = false;
			m_resizeEdges = edges;
			m_grab = mouse;
			m_start = window;
		}

		internal void end()
		{
			m_dragging = false;
			m_resizeEdges = 0;
		}

		internal readonly Vector2 movePosition(Vector2 mouse) => VectorMath.v2(mouse.x - m_grab.x, mouse.y - m_grab.y);
		internal readonly Rect resize(Vector2 mouse, float minW, float minH) => WindowChrome.resize(m_start, m_resizeEdges, VectorMath.sub(mouse, m_grab), minW, minH);
	}
}
