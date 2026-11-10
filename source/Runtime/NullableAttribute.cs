namespace System.Runtime.CompilerServices
{
	// unity 6 interop embeds its own copy and two referenced copies leave the compiler with neither, a source copy wins
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Event | AttributeTargets.Field | AttributeTargets.GenericParameter | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue)]
	internal sealed class NullableAttribute : Attribute
	{
		internal readonly byte[] m_nullableFlags;

		internal NullableAttribute(byte flag)
		{
			m_nullableFlags = new[] { flag };
		}

		internal NullableAttribute(byte[] flags)
		{
			m_nullableFlags = flags;
		}
	}
}
