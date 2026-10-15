using System;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

namespace Trellis
{
	// native layout fast paths stay off until the host has self tested them against the engine
	internal static class Live
	{
		private static int m_ptrOffset;
		private static int m_idOffset;
		private static IntPtr m_frameIcall;
		private static bool m_fastLiveness;
		private static bool m_fastId;
		private static bool m_fastFrames;

		internal static bool ptrKnown => Live.m_ptrOffset > 0;
		internal static bool idKnown => Live.m_idOffset > 0;
		internal static bool icallKnown => Live.m_frameIcall != IntPtr.Zero;

		internal static void layout(int ptrOffset, int idOffset, IntPtr frameIcall)
		{
			Live.m_ptrOffset = ptrOffset;
			Live.m_idOffset = idOffset;
			Live.m_frameIcall = frameIcall;
		}

		internal static void enable(bool liveness, bool instanceId, bool frames)
		{
			Live.m_fastLiveness = liveness;
			Live.m_fastId = liveness && instanceId;
			Live.m_fastFrames = frames;
		}

		internal static int frameCount => Live.m_fastFrames ? Live.icallFrames() : Time.frameCount;

		// wrapper liveness only since a destroyed object keeps its pointer
		internal static bool isValid(Il2CppObjectBase obj) => obj != null && !obj.WasCollected;

		internal static bool isUnityAlive(UnityEngine.Object obj)
		{
			if (obj is null)
				return false;

			try
			{
				if (Live.m_fastLiveness)
					return Live.cachedPtr(obj) != IntPtr.Zero;

				return (bool)obj;
			}
			catch (Exception ex)
			{
				UiLog.debug($"[Live.isUnityAlive] {ex.Message}");
				return false;
			}
		}

		internal static bool unityEquals(UnityEngine.Object a, UnityEngine.Object b)
		{
			if (a is null)
				return !Live.isUnityAlive(b);
			if (b is null)
				return !Live.isUnityAlive(a);

			return Il2CppCast.sameObject(a, b);
		}

		internal static int instanceId(UnityEngine.Object obj)
		{
			if (obj is null)
				return 0;

			try
			{
				if (Live.m_fastId)
					return Live.readId(obj);

				return obj.GetInstanceID();
			}
			catch (Exception ex)
			{
				UiLog.debug($"[Live.instanceId] {ex.Message}");
				return 0;
			}
		}

		internal static void destroy<T>(ref T obj) where T : UnityEngine.Object
		{
			T target = obj;

			obj = null;

			if (!Live.isUnityAlive(target))
				return;

			try
			{
				UnityEngine.Object.Destroy(target);
			}
			catch (Exception ex)
			{
				UiLog.debug($"[Live.destroy] {ex.Message}");
			}
		}

		// the engine zeroes this when the native object dies
		internal static unsafe IntPtr cachedPtr(UnityEngine.Object obj) => *(IntPtr*)(obj.Pointer + Live.m_ptrOffset);

		internal static unsafe int readId(UnityEngine.Object obj)
		{
			IntPtr native = Live.cachedPtr(obj);
			return native == IntPtr.Zero ? 0 : *(int*)(native + Live.m_idOffset);
		}

		internal static unsafe int icallFrames() => ((delegate* unmanaged[Cdecl]<int>)Live.m_frameIcall)();
	}
}
