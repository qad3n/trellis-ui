using System;

namespace Trellis
{
	// the key is the saved identity so the host can rename or move the field without a lookup by name
	[AttributeUsage(AttributeTargets.Field)]
	internal sealed class ConfigAttribute : Attribute
	{
		internal readonly ulong m_key;

		internal ConfigAttribute(ulong key)
		{
			m_key = key;
		}
	}
}
