using System;
using System.Runtime.CompilerServices;

namespace Trellis
{
	// mathf goes through interop and boxes while MathF is a jit intrinsic
	internal static class FastMath
	{
		internal const float kPI = 3.14159274f;
		internal const float kDeg2Rad = FastMath.kPI * 2f / 360f;
		internal const float kRad2Deg = 1f / FastMath.kDeg2Rad;
		private const float kEpsilon = float.Epsilon;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float abs(float value) => MathF.Abs(value);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float max(float a, float b) => a > b ? a : b;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int max(int a, int b) => a > b ? a : b;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float min(float a, float b) => a < b ? a : b;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int min(int a, int b) => a < b ? a : b;

		// mathf params overloads allocate an array per call
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float min(float a, float b, float c) => FastMath.min(FastMath.min(a, b), c);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float max(float a, float b, float c) => FastMath.max(FastMath.max(a, b), c);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float clamp(float value, float min, float max)
		{
			if (value < min)
				return min;
			if (value > max)
				return max;

			return value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int clamp(int value, int min, int max)
		{
			if (value < min)
				return min;
			if (value > max)
				return max;

			return value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int wrap(int value, int count) => (value % count + count) % count;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float clamp01(float value)
		{
			if (value < 0f)
				return 0f;
			if (value > 1f)
				return 1f;

			return value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float lerp(float a, float b, float t) => a + (b - a) * FastMath.clamp01(t);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float lerpUnclamped(float a, float b, float t) => a + (b - a) * t;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float inverseLerp(float a, float b, float value) => a == b ? 0f : FastMath.clamp01((value - a) / (b - a));

		// managed MathF.Sign throws on NaN where Mathf.Sign gives minus one
		internal static float moveTowards(float current, float target, float maxDelta)
		{
			float delta = target - current;
			if (MathF.Abs(delta) <= maxDelta)
				return target;

			return current + FastMath.sign(delta) * maxDelta;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float sqrt(float value) => MathF.Sqrt(value);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float round(float value) => MathF.Round(value);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int roundToInt(float value) => (int)MathF.Round(value);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int quantise(float value, float quantum) => (int)(value * quantum);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float floor(float value) => MathF.Floor(value);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int floorToInt(float value) => (int)MathF.Floor(value);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float ceil(float value) => MathF.Ceiling(value);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int ceilToInt(float value) => (int)MathF.Ceiling(value);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float sin(float radians) => MathF.Sin(radians);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float cos(float radians) => MathF.Cos(radians);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float tan(float radians) => MathF.Tan(radians);

		internal static float asin(float value) => MathF.Asin(value);
		internal static float acos(float value) => MathF.Acos(value);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float atan2(float y, float x) => MathF.Atan2(y, x);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float exp(float power) => MathF.Exp(power);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float pow(float value, float power) => MathF.Pow(value, power);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float log2(float value) => MathF.Log2(value);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float log(float value) => MathF.Log(value);

		internal static float repeat(float t, float length) => FastMath.clamp(t - MathF.Floor(t / length) * length, 0f, length);

		internal static float pingPong(float t, float length)
		{
			float wrapped = FastMath.repeat(t, length * 2f);
			return length - MathF.Abs(wrapped - length);
		}

		internal static float smoothStep(float from, float to, float t)
		{
			float c = FastMath.clamp01(t);

			c = -2f * c * c * c + 3f * c * c;
			return to * c + from * (1f - c);
		}

		// unity tolerance formula copied exactly
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool approximately(float a, float b) => MathF.Abs(b - a) < FastMath.max(1E-06f * FastMath.max(MathF.Abs(a), MathF.Abs(b)), FastMath.kEpsilon * 8f);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static float sign(float value) => value >= 0f ? 1f : -1f;
	}
}
