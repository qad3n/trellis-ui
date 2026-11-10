namespace System.Runtime.CompilerServices
{
	// unity 6 interop embeds its own copy and two referenced copies leave the compiler with neither, a source copy wins
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Delegate | AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Struct)]
	internal sealed class NullableContextAttribute : Attribute
	{
		internal readonly byte m_flag;

		internal NullableContextAttribute(byte flag)
		{
			m_flag = flag;
		}
	}
}
