using System.Runtime.CompilerServices;
using UnityEngine;

namespace Trellis
{
	// unity Vector2 operators are invoke stubs
	internal static class FitMath
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Vector2 cover(in Vector2 box, in Vector2 source) => FitMath.scaled(box, source, FastMath.max(FitMath.ratio(box.x, source.x), FitMath.ratio(box.y, source.y)));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Vector2 contain(in Vector2 box, in Vector2 source) => FitMath.scaled(box, source, FastMath.min(FitMath.ratio(box.x, source.x), FitMath.ratio(box.y, source.y)));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Vector2 tileCount(in Vector2 box, in Vector2 source) => VectorMath.v2(FitMath.ratio(box.x, source.x), FitMath.ratio(box.y, source.y));

		private static Vector2 scaled(in Vector2 box, in Vector2 source, float scale)
		{
			if (scale <= 0f)
				return box;

			return VectorMath.v2(source.x * scale, source.y * scale);
		}

		private static float ratio(float box, float source) => source > 0f ? box / source : 0f;
	}
}
