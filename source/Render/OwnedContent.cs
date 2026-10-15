using UnityEngine;

namespace Trellis
{
	internal sealed class OwnedContent
	{
		private readonly GUIContent m_content = new GUIContent();
		private readonly GUIContent m_plain = new GUIContent();
		private string m_source;
		private string m_plainSource;
		private bool m_passthrough;

		internal GUIContent For(string text)
		{
			if (object.ReferenceEquals(text, m_source))
				return m_content;

			m_content.text = text;
			m_source = text;
			return m_content;
		}

		internal GUIContent plainFor(string text)
		{
			if (!object.ReferenceEquals(text, m_plainSource))
			{
				m_plainSource = text;

				string plain = RichText.stripColors(text);
				m_passthrough = object.ReferenceEquals(plain, text);
				if (!m_passthrough)
					m_plain.text = plain;
			}
			return m_passthrough ? For(text) : m_plain;
		}
	}
}
