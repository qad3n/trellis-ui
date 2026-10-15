using UnityEngine;

namespace Trellis
{
	internal readonly record struct HsvDelta(float sat, float val)
	{
		// unity RGBToHSV says hue 0 for every grey so adding saturation turns grey red
		internal Color apply(float h, float s, float v) => ColorMath.hsvToColor(h, FastMath.clamp01(s + (sat > 0f ? sat * s : sat)), FastMath.clamp01(v + val), 1f);
	}
}
