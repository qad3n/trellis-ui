using System;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.Runtime;

namespace Trellis
{
	internal static class Il2CppCast
	{
		internal static IntPtr pointerOf(Il2CppObjectBase obj)
		{
			if (obj == null)
				return IntPtr.Zero;

			try
			{
				return obj.Pointer;
			}
			catch (ObjectCollectedException ex)
			{
				UiLog.debug($"[Il2CppCast.pointerOf] {ex.Message}");
				return IntPtr.Zero;
			}
		}

		internal static bool sameObject(Il2CppObjectBase a, Il2CppObjectBase b)
		{
			if (a == null || b == null)
				return object.ReferenceEquals(a, b);

			IntPtr pointer = Il2CppCast.pointerOf(a);
			return pointer != IntPtr.Zero && pointer == Il2CppCast.pointerOf(b);
		}

		internal static bool Is<T>(Il2CppObjectBase obj) where T : Il2CppObjectBase
		{
			IntPtr pointer = Il2CppCast.pointerOf(obj);
			if (pointer == IntPtr.Zero)
				return false;
			if (obj is T)
				return true;

			try
			{
				return Il2CppCast.assignable<T>(pointer);
			}
			catch (Exception ex)
			{
				UiLog.logException($"Il2CppCast.Is<{typeof(T).Name}>", ex);
				return false;
			}
		}

		internal static T As<T>(Il2CppObjectBase obj) where T : Il2CppObjectBase
		{
			IntPtr pointer = Il2CppCast.pointerOf(obj);
			if (pointer == IntPtr.Zero)
				return null;
			if (obj is T typed)
				return typed;

			try
			{
				return Il2CppCast.assignable<T>(pointer) ? Il2CppObjectPool.Get<T>(pointer) : null;
			}
			catch (Exception ex)
			{
				UiLog.logException($"Il2CppCast.As<{typeof(T).Name}>", ex);
				return null;
			}
		}

		private static bool assignable<T>(IntPtr pointer)
		{
			IntPtr target = Il2CppClassPointerStore<T>.NativeClassPtr;
			if (target == IntPtr.Zero)
				throw new ArgumentException($"{typeof(T).FullName} has no il2cpp class");

			return IL2CPP.il2cpp_class_is_assignable_from(target, IL2CPP.il2cpp_object_get_class(pointer));
		}
	}
}
