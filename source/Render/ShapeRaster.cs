using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace Trellis
{
	internal static class ShapeRaster
	{
		private const float kHalfPixel = 0.5f;

		internal static Texture2D build(in CrosshairSpec geometry, bool outline)
		{
			int size = geometry.m_textureSize;

			float center = size * 0.5f;

			float expand = outline ? geometry.m_outlineThickness : 0f;

			// a managed array into SetPixels32 is weakly held and can get collected mid copy
			Il2CppStructArray<Color32> buffer = new Il2CppStructArray<Color32>(size * size);

			Span<Color32> pixels = buffer.AsSpan();
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					float dx = x + ShapeRaster.kHalfPixel - center;

					float dy = y + ShapeRaster.kHalfPixel - center;

					float coverage = ShapeRaster.sampleCoverage(geometry, dx, dy, expand);

					byte alpha = ColorMath.toByte(coverage);
					pixels[y * size + x] = ColorMath.rgba32(255, 255, 255, alpha);
				}
			}

			Texture2D texture = Textures.createPoint(size, size);
			texture.SetPixels32(buffer);

			GC.KeepAlive(buffer);

			texture.Apply(false, false);
			return texture;
		}

		private static float sampleCoverage(in CrosshairSpec geometry, float dx, float dy, float expand)
		{
			float coverage = ShapeRaster.shapeCoverage(geometry, dx, dy, expand);
			if (geometry.m_centerDot)
			{
				float distance = VectorMath.distance2D(dx, dy);

				coverage = FastMath.max(coverage, ShapeRaster.discCoverage(distance, geometry.m_dotSize + expand));
			}
			return coverage;
		}

		private static float shapeCoverage(in CrosshairSpec geometry, float dx, float dy, float expand)
		{
			switch (geometry.m_shape)
			{
				case CrosshairShape.Cross:
					return ShapeRaster.crossCoverage(geometry, dx, dy, expand);

				case CrosshairShape.Circle:
					return ShapeRaster.ringCoverage(VectorMath.distance2D(dx, dy), geometry.m_size - geometry.m_thickness - expand, geometry.m_size + expand);

				case CrosshairShape.Dot:
					return ShapeRaster.discCoverage(VectorMath.distance2D(dx, dy), geometry.m_size + expand);

				default:
					return 0f;
			}
		}

		private static float crossCoverage(in CrosshairSpec geometry, float dx, float dy, float expand)
		{
			float halfThickness = geometry.m_thickness * 0.5f + expand;

			float inner = geometry.m_gap - expand;

			float outer = geometry.m_gap + geometry.m_size + expand;

			float coverage = ShapeRaster.rectCoverage(dx, dy, inner, outer, -halfThickness, halfThickness);

			coverage = FastMath.max(coverage, ShapeRaster.rectCoverage(dx, dy, -outer, -inner, -halfThickness, halfThickness));
			coverage = FastMath.max(coverage, ShapeRaster.rectCoverage(dx, dy, -halfThickness, halfThickness, inner, outer));
			coverage = FastMath.max(coverage, ShapeRaster.rectCoverage(dx, dy, -halfThickness, halfThickness, -outer, -inner));
			return coverage;
		}

		private static float rectCoverage(float dx, float dy, float minX, float maxX, float minY, float maxY)
		{
			float overlapX = FastMath.min(dx + ShapeRaster.kHalfPixel, maxX) - FastMath.max(dx - ShapeRaster.kHalfPixel, minX);

			float overlapY = FastMath.min(dy + ShapeRaster.kHalfPixel, maxY) - FastMath.max(dy - ShapeRaster.kHalfPixel, minY);
			if (overlapX <= 0f || overlapY <= 0f)
				return 0f;

			return FastMath.min(overlapX, 1f) * FastMath.min(overlapY, 1f);
		}

		private static float discCoverage(float distance, float radius)
		{
			if (radius <= 0f)
				return 0f;

			return FastMath.clamp01(radius + ShapeRaster.kHalfPixel - distance);
		}

		private static float ringCoverage(float distance, float innerRadius, float outerRadius)
		{
			float outerEdge = FastMath.clamp01(outerRadius + ShapeRaster.kHalfPixel - distance);
			if (innerRadius <= 0f)
				return outerEdge;

			float innerEdge = FastMath.clamp01(distance - (innerRadius - ShapeRaster.kHalfPixel));
			return FastMath.min(outerEdge, innerEdge);
		}
	}
}
