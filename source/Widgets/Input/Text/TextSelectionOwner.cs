using System;

namespace Trellis
{
	// one per selectable surface so claiming focus can clear whoever held it
	internal sealed class TextSelectionOwner
	{
		private readonly Action m_clear;

		internal TextSelectionOwner(Action clear) => m_clear = clear;

		internal void clear() => m_clear();
	}
}
