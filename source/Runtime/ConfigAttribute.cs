using System;

namespace Trellis
{
	// needs the Obfuscation opt out too and moving a field changes its key
	[AttributeUsage(AttributeTargets.Field)]
	internal sealed class ConfigAttribute : Attribute {}
}
