using System;

namespace Trellis
{
	// the key is the saved identity so the host can rename or move the field without a lookup by name
	// the host build stamps it from Owner.Field and a renamed field pins its old Owner.Field
	[AttributeUsage(AttributeTargets.Field)]
	internal sealed class ConfigAttribute : Attribute
	{
		internal readonly ulong m_key;

		internal ConfigAttribute()
		{
		}

		internal ConfigAttribute(string pinned)
		{
		}

		internal ConfigAttribute(ulong key)
		{
			m_key = key;
		}
	}
}
