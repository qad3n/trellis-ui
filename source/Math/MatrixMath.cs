using UnityEngine;

namespace Trellis
{
	// operand order follows the engine so it matches bit for bit
	internal static class MatrixMath
	{
		internal static Matrix4x4 multiply(in Matrix4x4 lhs, in Matrix4x4 rhs)
		{
			Matrix4x4 result = default;
			result.m00 = lhs.m00 * rhs.m00 + lhs.m01 * rhs.m10 + lhs.m02 * rhs.m20 + lhs.m03 * rhs.m30;
			result.m01 = lhs.m00 * rhs.m01 + lhs.m01 * rhs.m11 + lhs.m02 * rhs.m21 + lhs.m03 * rhs.m31;
			result.m02 = lhs.m00 * rhs.m02 + lhs.m01 * rhs.m12 + lhs.m02 * rhs.m22 + lhs.m03 * rhs.m32;
			result.m03 = lhs.m00 * rhs.m03 + lhs.m01 * rhs.m13 + lhs.m02 * rhs.m23 + lhs.m03 * rhs.m33;
			result.m10 = lhs.m10 * rhs.m00 + lhs.m11 * rhs.m10 + lhs.m12 * rhs.m20 + lhs.m13 * rhs.m30;
			result.m11 = lhs.m10 * rhs.m01 + lhs.m11 * rhs.m11 + lhs.m12 * rhs.m21 + lhs.m13 * rhs.m31;
			result.m12 = lhs.m10 * rhs.m02 + lhs.m11 * rhs.m12 + lhs.m12 * rhs.m22 + lhs.m13 * rhs.m32;
			result.m13 = lhs.m10 * rhs.m03 + lhs.m11 * rhs.m13 + lhs.m12 * rhs.m23 + lhs.m13 * rhs.m33;
			result.m20 = lhs.m20 * rhs.m00 + lhs.m21 * rhs.m10 + lhs.m22 * rhs.m20 + lhs.m23 * rhs.m30;
			result.m21 = lhs.m20 * rhs.m01 + lhs.m21 * rhs.m11 + lhs.m22 * rhs.m21 + lhs.m23 * rhs.m31;
			result.m22 = lhs.m20 * rhs.m02 + lhs.m21 * rhs.m12 + lhs.m22 * rhs.m22 + lhs.m23 * rhs.m32;
			result.m23 = lhs.m20 * rhs.m03 + lhs.m21 * rhs.m13 + lhs.m22 * rhs.m23 + lhs.m23 * rhs.m33;
			result.m30 = lhs.m30 * rhs.m00 + lhs.m31 * rhs.m10 + lhs.m32 * rhs.m20 + lhs.m33 * rhs.m30;
			result.m31 = lhs.m30 * rhs.m01 + lhs.m31 * rhs.m11 + lhs.m32 * rhs.m21 + lhs.m33 * rhs.m31;
			result.m32 = lhs.m30 * rhs.m02 + lhs.m31 * rhs.m12 + lhs.m32 * rhs.m22 + lhs.m33 * rhs.m32;
			result.m33 = lhs.m30 * rhs.m03 + lhs.m31 * rhs.m13 + lhs.m32 * rhs.m23 + lhs.m33 * rhs.m33;
			return result;
		}

		internal static Vector3 multiplyVector(in Matrix4x4 m, in Vector3 v) => VectorMath.v3(m.m00 * v.x + m.m01 * v.y + m.m02 * v.z, m.m10 * v.x + m.m11 * v.y + m.m12 * v.z, m.m20 * v.x + m.m21 * v.y + m.m22 * v.z);

		// unity SetRow is an interop invoke
		internal static Matrix4x4 fromRows(in Vector4 row0, in Vector4 row1, in Vector4 row2, in Vector4 row3)
		{
			Matrix4x4 m = default;
			m.m00 = row0.x;
			m.m01 = row0.y;
			m.m02 = row0.z;
			m.m03 = row0.w;
			m.m10 = row1.x;
			m.m11 = row1.y;
			m.m12 = row1.z;
			m.m13 = row1.w;
			m.m20 = row2.x;
			m.m21 = row2.y;
			m.m22 = row2.z;
			m.m23 = row2.w;
			m.m30 = row3.x;
			m.m31 = row3.y;
			m.m32 = row3.z;
			m.m33 = row3.w;
			return m;
		}

		internal static Matrix4x4 translation(in Vector3 offset)
		{
			Matrix4x4 m = VectorMath.kIdentityMatrix;
			m.m03 = offset.x;
			m.m13 = offset.y;
			m.m23 = offset.z;
			return m;
		}

		internal static Matrix4x4 scale(in Vector3 scale)
		{
			Matrix4x4 m = VectorMath.kIdentityMatrix;
			m.m00 = scale.x;
			m.m11 = scale.y;
			m.m22 = scale.z;
			return m;
		}
	}
}
