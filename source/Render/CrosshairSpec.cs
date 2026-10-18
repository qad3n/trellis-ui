namespace Trellis
{
	// one crosshair in whole pixels, with the texture it rasterizes into
	internal readonly struct CrosshairSpec
	{
		internal readonly CrosshairShape m_shape;
		internal readonly float m_size;
		internal readonly float m_thickness;
		internal readonly float m_gap;
		internal readonly bool m_centerDot;
		internal readonly float m_dotSize;
		internal readonly bool m_outlined;
		internal readonly float m_outlineThickness;
		internal readonly bool m_pixelCentered;
		internal readonly int m_textureSize;

		private static class Raster
		{
			internal const int kPadding = 2;
			internal const int kMaxSize = 513;
		}

		internal CrosshairSpec(CrosshairShape shape, float size, float thickness, float gap, bool centerDot, float dotSize, bool outlined, float outlineThickness)
		{
			m_shape = shape;
			m_thickness = CrosshairSpec.wholePixels(thickness);
			m_pixelCentered = shape != CrosshairShape.Cross || ((int)m_thickness & 1) == 1;
			m_size = CrosshairSpec.wholePixels(size);
			m_gap = CrosshairSpec.armStart(FastMath.max(0f, gap), m_pixelCentered);
			m_centerDot = centerDot && shape != CrosshairShape.Dot;
			m_dotSize = FastMath.max(1f, dotSize);
			m_outlined = outlined;
			m_outlineThickness = CrosshairSpec.wholePixels(outlineThickness);
			m_textureSize = CrosshairSpec.sizeFor(shape, m_size, m_thickness, m_gap, m_centerDot, m_dotSize, outlined, m_outlineThickness, m_pixelCentered);
		}

		internal bool equals(in CrosshairSpec other) => m_shape == other.m_shape && m_size == other.m_size && m_thickness == other.m_thickness && m_gap == other.m_gap && m_centerDot == other.m_centerDot && m_dotSize == other.m_dotSize && m_outlined == other.m_outlined && m_outlineThickness == other.m_outlineThickness && m_pixelCentered == other.m_pixelCentered;

		private static int sizeFor(CrosshairShape shape, float size, float thickness, float gap, bool centerDot, float dotSize, bool outline, float outlineThickness, bool pixelCentered)
		{
			float halfThickness = FastMath.max(1f, thickness) * 0.5f;

			float reach = shape == CrosshairShape.Cross ? FastMath.max(gap + size, halfThickness) : size;
			if (centerDot && shape != CrosshairShape.Dot)
				reach = FastMath.max(reach, dotSize);

			if (outline)
				reach += FastMath.max(1f, outlineThickness);

			int half = FastMath.ceilToInt(reach) + Raster.kPadding;

			int computed = pixelCentered ? half * 2 + 1 : half * 2;
			return FastMath.min(computed, pixelCentered ? Raster.kMaxSize : Raster.kMaxSize - 1);
		}

		private static float wholePixels(float value) => FastMath.max(1f, PixelGrid.roundHalfUp(value));

		private static float armStart(float gap, bool pixelCentered)
		{
			if (gap <= 0f)
				return 0f;

			return pixelCentered ? FastMath.floor(gap) + 0.5f : PixelGrid.roundHalfUp(gap);
		}
	}
}
