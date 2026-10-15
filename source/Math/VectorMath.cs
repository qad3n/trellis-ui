using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Trellis
{
	internal static class VectorMath
	{
		private const float kMinSegment = 0.0001f;
		private const float kDegenerateSqr = 0.000001f;
		private const float kMaxAlignment = 0.95f;

		// unity Vector3.zero getters box through interop
		internal static readonly Vector3 kZero = VectorMath.v3(0f, 0f, 0f);
		internal static readonly Vector3 kOne = VectorMath.v3(1f, 1f, 1f);
		internal static readonly Vector3 kRight = VectorMath.v3(1f, 0f, 0f);
		internal static readonly Vector3 kUp = VectorMath.v3(0f, 1f, 0f);
		internal static readonly Vector3 kDown = VectorMath.v3(0f, -1f, 0f);
		internal static readonly Vector3 kForward = VectorMath.v3(0f, 0f, 1f);
		internal static readonly Vector2 kZero2 = VectorMath.v2(0f, 0f);
		internal static readonly Quaternion kIdentityRotation = VectorMath.identityRotation();
		internal static readonly Matrix4x4 kIdentityMatrix = VectorMath.identityMatrix();

		private static class Engine
		{
			internal const float kEqualitySqr = 9.99999944E-11f;
			internal const float kNormalizeEpsilon = 1E-05f;
			internal const float kAngleEpsilon = 1E-15f;
			internal const float kEqualDot = 1f - 0.000001f;
			internal const float kLinearDot = 0.9995f;
			internal const float kFullTurn = 360f;
		}

		// unity Vector3 operators are interop invoke stubs while x y z are real fields
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Vector3 v3(float x, float y, float z)
		{
			Vector3 v = default;
			v.x = x;
			v.y = y;
			v.z = z;
			return v;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Vector2 v2(float x, float y)
		{
			Vector2 v = default;
			v.x = x;
			v.y = y;
			return v;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Vector4 v4(float x, float y, float z, float w)
		{
			Vector4 v = default;
			v.x = x;
			v.y = y;
			v.z = z;
			v.w = w;
			return v;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Quaternion q(float x, float y, float z, float w)
		{
			Quaternion q = default;
			q.x = x;
			q.y = y;
			q.z = z;
			q.w = w;
			return q;
		}

		// unity Vector2 equality is an interop invoke and approximate
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool isZero(in Vector2 v) => v.x == 0f && v.y == 0f;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Vector3 add(in Vector3 a, in Vector3 b) => VectorMath.v3(a.x + b.x, a.y + b.y, a.z + b.z);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Vector3 sub(in Vector3 a, in Vector3 b) => VectorMath.v3(a.x - b.x, a.y - b.y, a.z - b.z);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Vector3 mul(in Vector3 a, float s) => VectorMath.v3(a.x * s, a.y * s, a.z * s);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Vector3 div(in Vector3 a, float s) => s == 0f ? default : VectorMath.mul(a, 1f / s);

		// zero divisor gives infinity like the engine
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Vector3 divide(in Vector3 a, float d) => VectorMath.v3(a.x / d, a.y / d, a.z / d);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Vector3 negate(in Vector3 v) => VectorMath.v3(-v.x, -v.y, -v.z);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Vector2 add(in Vector2 a, in Vector2 b) => VectorMath.v2(a.x + b.x, a.y + b.y);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Vector2 sub(in Vector2 a, in Vector2 b) => VectorMath.v2(a.x - b.x, a.y - b.y);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Vector2 flatten(in Vector3 v) => VectorMath.v2(v.x, v.y);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool engineEquals(in Vector2 a, in Vector2 b)
		{
			float dx = a.x - b.x;

			float dy = a.y - b.y;
			return dx * dx + dy * dy < Engine.kEqualitySqr;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool engineEquals(in Vector3 a, in Vector3 b) => VectorMath.sqrDistance(a, b) < Engine.kEqualitySqr;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Vector3 addScaled(in Vector3 a, in Vector3 b, float s) => VectorMath.v3(a.x + b.x * s, a.y + b.y * s, a.z + b.z * s);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float dot(in Vector3 a, in Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Vector3 lerp(in Vector3 a, in Vector3 b, float t)
		{
			float k = FastMath.clamp01(t);
			return VectorMath.v3(a.x + (b.x - a.x) * k, a.y + (b.y - a.y) * k, a.z + (b.z - a.z) * k);
		}

		internal static Vector3 clampMagnitude(in Vector3 v, float maxLength)
		{
			float squared = VectorMath.sqrMagnitude(v);
			if (squared <= maxLength * maxLength)
				return v;

			float scale = maxLength / FastMath.sqrt(squared);
			return VectorMath.mul(v, scale);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Vector3 scale(in Vector3 a, in Vector3 b) => VectorMath.v3(a.x * b.x, a.y * b.y, a.z * b.z);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Vector3 cross(in Vector3 a, in Vector3 b) => VectorMath.v3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float sqrMagnitude(in Vector3 v) => v.x * v.x + v.y * v.y + v.z * v.z;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float magnitude(in Vector3 v) => FastMath.sqrt(VectorMath.sqrMagnitude(v));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float sqrDistance(in Vector3 a, in Vector3 b)
		{
			float dx = a.x - b.x;

			float dy = a.y - b.y;

			float dz = a.z - b.z;
			return dx * dx + dy * dy + dz * dz;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float distance(in Vector3 a, in Vector3 b) => FastMath.sqrt(VectorMath.sqrDistance(a, b));

		// returns zero for short or NaN like Vector3.normalized
		internal static Vector3 normalized(in Vector3 v)
		{
			float magnitude = VectorMath.magnitude(v);
			return magnitude > Engine.kNormalizeEpsilon ? VectorMath.divide(v, magnitude) : default;
		}

		internal static float distance2D(float dx, float dy) => FastMath.sqrt(dx * dx + dy * dy);

		internal static float deltaAngle(float dy, float dx) => FastMath.atan2(dy, dx) * FastMath.kRad2Deg;
		internal static bool beyond(Vector3 from, Vector3 to, float maxDistance) => VectorMath.sqrDistance(to, from) > maxDistance * maxDistance;
		internal static bool beyond(Vector3 delta, float maxDistance) => VectorMath.sqrMagnitude(delta) > maxDistance * maxDistance;

		internal static float distanceToSegment(Vector3 point, Vector3 origin, Vector3 direction, float length)
		{
			Vector3 delta = VectorMath.sub(point, origin);

			float projection = FastMath.clamp(VectorMath.dot(delta, direction), 0f, length);
			return VectorMath.magnitude(VectorMath.sub(delta, VectorMath.mul(direction, projection)));
		}

		// unity Vector3.Angle uses the double precision acos
		internal static float angleDeg(in Vector3 a, in Vector3 b)
		{
			float lengthProduct = FastMath.sqrt(VectorMath.sqrMagnitude(a) * VectorMath.sqrMagnitude(b));
			if (lengthProduct < Engine.kAngleEpsilon)
				return 0f;

			float cosine = FastMath.clamp(VectorMath.dot(a, b) / lengthProduct, -1f, 1f);
			return (float)Math.Acos(cosine) * FastMath.kRad2Deg;
		}

		// unity Quaternion.Angle uses single precision acos
		internal static float angleDeg(in Quaternion a, in Quaternion b)
		{
			float dot = FastMath.min(FastMath.abs(a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w), 1f);
			if (dot > Engine.kEqualDot)
				return 0f;

			return MathF.Acos(dot) * 2f * FastMath.kRad2Deg;
		}

		internal static Vector3 rotate(in Quaternion rotation, in Vector3 point)
		{
			float x = rotation.x * 2f;

			float y = rotation.y * 2f;

			float z = rotation.z * 2f;

			float xx = rotation.x * x;

			float yy = rotation.y * y;

			float zz = rotation.z * z;

			float xy = rotation.x * y;

			float xz = rotation.x * z;

			float yz = rotation.y * z;

			float wx = rotation.w * x;

			float wy = rotation.w * y;

			float wz = rotation.w * z;
			return VectorMath.v3((1f - (yy + zz)) * point.x + (xy - wz) * point.y + (xz + wy) * point.z, (xy + wz) * point.x + (1f - (xx + zz)) * point.y + (yz - wx) * point.z, (xz - wy) * point.x + (yz + wx) * point.y + (1f - (xx + yy)) * point.z);
		}

		internal static Quaternion multiply(in Quaternion lhs, in Quaternion rhs) => VectorMath.q(lhs.w * rhs.x + lhs.x * rhs.w + lhs.y * rhs.z - lhs.z * rhs.y, lhs.w * rhs.y + lhs.y * rhs.w + lhs.z * rhs.x - lhs.x * rhs.z, lhs.w * rhs.z + lhs.z * rhs.w + lhs.x * rhs.y - lhs.y * rhs.x, lhs.w * rhs.w - lhs.x * rhs.x - lhs.y * rhs.y - lhs.z * rhs.z);
		// the engine inverse is the conjugate and never divides by the norm
		internal static Quaternion inverse(in Quaternion q) => VectorMath.q(-q.x, -q.y, -q.z, q.w);

		internal static Quaternion angleAxisUnit(float degrees, in Vector3 unitAxis)
		{
			float half = degrees * FastMath.kDeg2Rad * 0.5f;

			float sin = FastMath.sin(half);
			return VectorMath.q(unitAxis.x * sin, unitAxis.y * sin, unitAxis.z * sin, FastMath.cos(half));
		}

		internal static float yawDeg(in Vector3 direction) => FastMath.repeat(FastMath.atan2(direction.x, direction.z) * FastMath.kRad2Deg, Engine.kFullTurn);
		// breaks near plus or minus 90 pitch where roll and yaw merge
		internal static float rollDeg(in Quaternion q) => FastMath.atan2(2f * (q.w * q.z + q.x * q.y), 1f - 2f * (q.x * q.x + q.z * q.z)) * FastMath.kRad2Deg;
		internal static Quaternion yawRoll(float yawDeg, float rollDeg) => VectorMath.multiply(VectorMath.angleAxisUnit(yawDeg, VectorMath.kUp), VectorMath.angleAxisUnit(rollDeg, VectorMath.kForward));

		internal static Quaternion slerp(in Quaternion a, in Quaternion b, float t)
		{
			float k = FastMath.clamp01(t);

			float dot = a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;

			float sign = 1f;
			if (dot < 0f)
			{
				dot = -dot;

				sign = -1f;
			}

			float weightA = 1f - k;

			float weightB = k;
			if (dot < Engine.kLinearDot)
			{
				float theta = MathF.Acos(dot);

				float inverseSin = 1f / MathF.Sin(theta);

				weightA = MathF.Sin(weightA * theta) * inverseSin;

				weightB = MathF.Sin(k * theta) * inverseSin;
			}

			weightB *= sign;

			float x = a.x * weightA + b.x * weightB;

			float y = a.y * weightA + b.y * weightB;

			float z = a.z * weightA + b.z * weightB;

			float w = a.w * weightA + b.w * weightB;

			float lengthSqr = x * x + y * y + z * z + w * w;
			if (lengthSqr < Engine.kNormalizeEpsilon)
				return VectorMath.kIdentityRotation;

			float inverseLength = 1f / FastMath.sqrt(lengthSqr);
			return VectorMath.q(x * inverseLength, y * inverseLength, z * inverseLength, w * inverseLength);
		}

		// the game only writes this on receive so exact compare is fine
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool bitEquals(in Vector3 a, in Vector3 b) => a.x == b.x && a.y == b.y && a.z == b.z;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool bitEquals(in Quaternion a, in Quaternion b) => a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool bitEquals(in Vector4 a, in Vector4 b) => a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w;

		internal static float minScaledDelta(in Vector3 v, in Vector3 offset, float scaleMin, float scaleMax)
		{
			float lengthSqr = VectorMath.sqrMagnitude(v);
			if (lengthSqr <= VectorMath.kDegenerateSqr)
				return VectorMath.magnitude(offset);

			float best = FastMath.clamp(VectorMath.dot(v, offset) / lengthSqr, scaleMin, scaleMax);
			return VectorMath.magnitude(VectorMath.sub(VectorMath.mul(v, best), offset));
		}

		internal static Vector3 closestPoint(Vector3 point, Vector3 start, Vector3 end)
		{
			Vector3 span = VectorMath.sub(end, start);

			float length = VectorMath.magnitude(span);
			if (length < VectorMath.kMinSegment)
				return start;

			Vector3 direction = VectorMath.v3(span.x / length, span.y / length, span.z / length);
			return VectorMath.add(start, VectorMath.mul(direction, FastMath.clamp(VectorMath.dot(VectorMath.sub(point, start), direction), 0f, length)));
		}

		internal static float segmentDistance(Vector3 aStart, Vector3 aEnd, Vector3 bStart, Vector3 bEnd)
		{
			Vector3 spanA = VectorMath.sub(aEnd, aStart);

			Vector3 spanB = VectorMath.sub(bEnd, bStart);

			Vector3 offset = VectorMath.sub(aStart, bStart);

			float lengthSqrA = VectorMath.dot(spanA, spanA);

			float lengthSqrB = VectorMath.dot(spanB, spanB);

			float projectionB = VectorMath.dot(spanB, offset);

			if (lengthSqrA <= VectorMath.kDegenerateSqr && lengthSqrB <= VectorMath.kDegenerateSqr)
				return VectorMath.magnitude(offset);

			float t;
			float s;
			if (lengthSqrA <= VectorMath.kDegenerateSqr)
			{
				s = 0f;

				t = FastMath.clamp01(projectionB / lengthSqrB);
			}
			else
			{
				float projectionA = VectorMath.dot(spanA, offset);
				if (lengthSqrB <= VectorMath.kDegenerateSqr)
				{
					t = 0f;

					s = FastMath.clamp01(-projectionA / lengthSqrA);
				}
				else
				{
					float cross = VectorMath.dot(spanA, spanB);

					float denominator = lengthSqrA * lengthSqrB - cross * cross;

					s = denominator > VectorMath.kDegenerateSqr ? FastMath.clamp01((cross * projectionB - projectionA * lengthSqrB) / denominator) : 0f;

					t = (cross * s + projectionB) / lengthSqrB;
					if (t < 0f)
					{
						t = 0f;

						s = FastMath.clamp01(-projectionA / lengthSqrA);
					}
					else if (t > 1f)
					{
						t = 1f;

						s = FastMath.clamp01((cross - projectionA) / lengthSqrA);
					}
				}
			}
			return VectorMath.distance(VectorMath.add(aStart, VectorMath.mul(spanA, s)), VectorMath.add(bStart, VectorMath.mul(spanB, t)));
		}

		internal static float magnitudePow(Vector3 v, float exponent) => FastMath.pow(VectorMath.magnitude(v), exponent);
		internal static bool nearZero(Vector3 v, float sqrEpsilon) => VectorMath.sqrMagnitude(v) < sqrEpsilon;
		internal static Vector3 circlePoint(Vector3 center, Vector3 axisA, Vector3 axisB, float angle, float radius) => VectorMath.circlePointFromTrig(center, axisA, axisB, FastMath.cos(angle), FastMath.sin(angle), radius);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Vector3 circlePointFromTrig(Vector3 center, Vector3 axisA, Vector3 axisB, float cos, float sin, float radius) => VectorMath.v3(center.x + (axisA.x * cos + axisB.x * sin) * radius, center.y + (axisA.y * cos + axisB.y * sin) * radius, center.z + (axisA.z * cos + axisB.z * sin) * radius);

		internal static Vector3 arcPoint(Vector3 center, Vector3 axis, Vector3 perpendicular, float angle, float radius)
		{
			float cos = FastMath.cos(angle);

			float sin = FastMath.sin(angle);
			return VectorMath.v3(center.x + perpendicular.x * cos * radius + axis.x * sin * radius, center.y + perpendicular.y * cos * radius + axis.y * sin * radius, center.z + perpendicular.z * cos * radius + axis.z * sin * radius);
		}

		internal static Vector3 perpendicularAxis(Vector3 axisWorld)
		{
			Vector3 reference = FastMath.abs(axisWorld.y) < VectorMath.kMaxAlignment ? VectorMath.kUp : VectorMath.kRight;
			return VectorMath.normalized(VectorMath.cross(axisWorld, reference));
		}

		internal static float maxScale(Vector3 lossy) => FastMath.max(FastMath.max(FastMath.abs(lossy.x), FastMath.abs(lossy.y)), FastMath.abs(lossy.z));
		internal static float segmentRadians(int segments) => 2f * FastMath.kPI / segments;
		internal static float radiansPerHalfSegment(int segments) => FastMath.kPI / segments;

		internal static Vector3 spherePoint(float heightUnit, float angleUnit)
		{
			float height = heightUnit * 2f - 1f;

			float ringRadius = VectorMath.latitudeRadius(1f, height);

			float angle = angleUnit * 2f * FastMath.kPI;
			return VectorMath.v3(ringRadius * FastMath.cos(angle), height, ringRadius * FastMath.sin(angle));
		}

		internal static float latitudeRadius(float radius, float fraction)
		{
			float remainder = 1f - fraction * fraction;
			return remainder <= 0f ? 0f : radius * FastMath.sqrt(remainder);
		}

		internal static Vector3 edgeStub(Vector3 from, Vector3 to, float fraction) => VectorMath.v3(from.x + (to.x - from.x) * fraction, from.y + (to.y - from.y) * fraction, from.z + (to.z - from.z) * fraction);
		internal static Vector2 edgeStub(Vector2 from, Vector2 to, float fraction) => VectorMath.v2(from.x + (to.x - from.x) * fraction, from.y + (to.y - from.y) * fraction);

		internal static void perpendicularBasis(Vector3 axis, out Vector3 axisA, out Vector3 axisB)
		{
			axisA = VectorMath.perpendicularAxis(axis);
			axisB = VectorMath.normalized(VectorMath.cross(axis, axisA));
		}

		internal static void rotateBasis(ref Vector3 axisA, ref Vector3 axisB, float angle)
		{
			float cos = FastMath.cos(angle);

			float sin = FastMath.sin(angle);

			Vector3 rotatedA = VectorMath.add(VectorMath.mul(axisA, cos), VectorMath.mul(axisB, sin));

			axisB = VectorMath.sub(VectorMath.mul(axisB, cos), VectorMath.mul(axisA, sin));

			axisA = rotatedA;
		}

		private static Quaternion identityRotation()
		{
			Quaternion q = default;
			q.w = 1f;
			return q;
		}

		private static Matrix4x4 identityMatrix()
		{
			Matrix4x4 m = default;
			m.m00 = 1f;
			m.m11 = 1f;
			m.m22 = 1f;
			m.m33 = 1f;
			return m;
		}
	}
}
