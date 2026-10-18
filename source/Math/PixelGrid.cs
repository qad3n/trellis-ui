using System.Runtime.CompilerServices;

namespace Trellis
{
	internal static class PixelGrid
	{
		// half to even gives equal spans different pixel counts
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float roundHalfUp(float value) => FastMath.floor(value + 0.5f);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float snap(float value, float pixelsPerUnit)
		{
			if (pixelsPerUnit <= 0f)
				return value;

			return PixelGrid.roundHalfUp(value * pixelsPerUnit) / pixelsPerUnit;
		}

		// the phase is where the local origin sits on the device grid in units
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float snap(float value, float pixelsPerUnit, float phase) => PixelGrid.snap(value + phase, pixelsPerUnit) - phase;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float snapSize(float size, float pixelsPerUnit)
		{
			if (pixelsPerUnit <= 0f)
				return size;

			return FastMath.max(1f, PixelGrid.roundHalfUp(size * pixelsPerUnit)) / pixelsPerUnit;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float ceil(float value, float pixelsPerUnit)
		{
			if (pixelsPerUnit <= 0f)
				return value;

			return FastMath.ceil(value * pixelsPerUnit) / pixelsPerUnit;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float snapBand(float start, float size, float snappedSize, float pixelsPerUnit)
		{
			if (pixelsPerUnit <= 0f)
				return start;

			float centre = (start + size * 0.5f) * pixelsPerUnit;
			return PixelGrid.roundHalfUp(centre - snappedSize * pixelsPerUnit * 0.5f) / pixelsPerUnit;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float snapBand(float start, float size, float snappedSize, float pixelsPerUnit, float phase) => PixelGrid.snapBand(start + phase, size, snappedSize, pixelsPerUnit) - phase;
	}
}
