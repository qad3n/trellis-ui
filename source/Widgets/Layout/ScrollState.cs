using UnityEngine;

namespace Trellis
{
	internal struct ScrollState
	{
		internal Vector2 m_position;
		internal Vector2 m_target;
		internal bool m_dragging;
		internal float m_grabOffset;
		internal MotionValue m_scrollMotion;
		internal float m_shownY;
		internal float m_thumbH;
		internal float m_thumbOffset;
		internal float m_trackH;
		internal MotionValue m_thumbMotion;
		internal int m_animId;

		internal void reset()
		{
			m_position = VectorMath.kZero2;
			m_target = VectorMath.kZero2;
			m_dragging = false;
			m_grabOffset = 0f;
			m_scrollMotion = MotionValue.at(0f);
			m_shownY = 0f;
			m_thumbH = 0f;
			m_thumbOffset = 0f;
			m_trackH = 0f;
			m_thumbMotion = MotionValue.at(0f);
		}

		internal void snapY(float y)
		{
			m_position.y = y;
			m_target.y = y;
		}
	}
}
