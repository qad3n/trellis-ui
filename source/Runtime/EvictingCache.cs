using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Trellis
{
	// insertion ordered so a hit doesnt move the key
	internal sealed class EvictingCache<TKey, TValue>
	{
		private readonly Dictionary<TKey, TValue> m_entries;
		private readonly Queue<TKey> m_order;
		private readonly int m_capacity;
		private readonly int m_evictBatch;

		internal EvictingCache(int capacity)
		{
			m_capacity = capacity;
			m_evictBatch = capacity >> 2 > 0 ? capacity >> 2 : 1;
			m_entries = new Dictionary<TKey, TValue>(capacity);
			m_order = new Queue<TKey>(capacity);
		}

		internal bool tryGetValue(TKey key, out TValue value) => m_entries.TryGetValue(key, out value);
		// the ref is only valid until the next Set
		internal ref TValue find(TKey key) => ref CollectionsMarshal.GetValueRefOrNullRef(m_entries, key);

		internal void set(TKey key, TValue value)
		{
			ref TValue slot = ref CollectionsMarshal.GetValueRefOrNullRef(m_entries, key);
			if (!Unsafe.IsNullRef(ref slot))
			{
				slot = value;
				return;
			}

			if (m_entries.Count >= m_capacity)
				evictOldest();

			m_entries.Add(key, value);

			m_order.Enqueue(key);
		}

		internal void clear()
		{
			m_entries.Clear();

			m_order.Clear();
		}

		private void evictOldest()
		{
			int target = m_evictBatch;
			while (target > 0 && m_order.Count > 0)
			{
				TKey key = m_order.Dequeue();
				if (!m_entries.Remove(key))
					continue;

				target--;
			}
		}
	}
}
