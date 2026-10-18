using System;
using System.Text;
using UnityEngine;

namespace Trellis
{
	// keys held together in press order and compared as a set so Ctrl + I equals I + Ctrl
	internal readonly struct KeyChord : IEquatable<KeyChord>
	{
		internal const int kMaxKeys = 4;
		private const string kUnbound = "None";
		private const string kJoin = " + ";

		private readonly KeyCode[] m_keys;

		internal KeyChord(KeyCode key)
		{
			m_keys = key == KeyCode.None ? null : new[] { key };
		}

		internal KeyChord(KeyCode[] keys, int count)
		{
			KeyCode[] kept = new KeyCode[FastMath.min(count, KeyChord.kMaxKeys)];

			int length = 0;
			for (int i = 0; i < count && length < kept.Length; i++)
			{
				KeyCode key = keys[i];
				if (key != KeyCode.None && Array.IndexOf(kept, key, 0, length) < 0)
					kept[length++] = key;
			}

			if (length < kept.Length)
				Array.Resize(ref kept, length);
			m_keys = length == 0 ? null : kept;
		}

		internal int count => m_keys == null ? 0 : m_keys.Length;
		internal bool bound => m_keys != null;
		internal KeyCode this[int index] => m_keys[index];

		internal bool contains(KeyCode key) => m_keys != null && Array.IndexOf(m_keys, key) >= 0;

		internal bool covers(KeyChord other)
		{
			for (int i = 0; i < other.count; i++)
			{
				if (!this.contains(other[i]))
					return false;
			}
			return true;
		}

		public bool Equals(KeyChord other) => this.count == other.count && this.covers(other);
		public override bool Equals(object other) => other is KeyChord chord && this.Equals(chord);

		public override int GetHashCode()
		{
			int hash = this.count;
			for (int i = 0; i < this.count; i++)
				hash ^= (int)m_keys[i] * Widgets.kKeyMix;
			return hash;
		}

		public override string ToString()
		{
			if (m_keys == null)
				return KeyChord.kUnbound;

			StringBuilder text = new StringBuilder();
			for (int i = 0; i < m_keys.Length; i++)
			{
				if (i > 0)
					text.Append(KeyChord.kJoin);
				text.Append(m_keys[i].ToString());
			}
			return text.ToString();
		}
	}
}
