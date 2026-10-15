namespace Trellis
{
	internal static partial class Widgets
	{
		private const float kDoubleClickSeconds = 0.35f;

		internal struct DoubleClick
		{
			private int m_lastKey;
			private float m_lastTime;
			private bool m_armed;

			internal bool register(int key)
			{
				float now = Gui.unscaledTime;

				bool repeat = m_armed && key == m_lastKey && now - m_lastTime <= Widgets.kDoubleClickSeconds;
				m_armed = !repeat;
				m_lastKey = key;
				m_lastTime = now;
				return repeat;
			}

			internal void reset() => m_armed = false;
		}
	}
}
