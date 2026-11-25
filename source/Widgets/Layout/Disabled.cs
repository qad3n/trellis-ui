using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Trellis
{
	internal static partial class Widgets
	{
		private const float kOpaqueThreshold = 0.999f;

		private static readonly Stack<(Color color, bool enabled, bool disabled)> kDisabledStack = new Stack<(Color color, bool enabled, bool disabled)>();
		private static readonly Dictionary<int, MotionValue> kDisabledAlphas = new Dictionary<int, MotionValue>();
		private static readonly Dictionary<int, int> kDisabledCounts = new Dictionary<int, int>();

		private static int m_disabledPass = -1;
		private static int m_disabledScopes;

		internal static void beginDisabled(bool disabled)
		{
			int id = Widgets.nextDisabled();

			float current = Widgets.stepDisabled(id, disabled);

			Widgets.kDisabledStack.Push((Gui.color, Gui.enabled, disabled));

			if (disabled)
			{
				Gui.enabled = false;

				Widgets.m_disabledScopes++;
			}

			if (!(current < Widgets.kOpaqueThreshold))
				return;

			Gui.color = ColorMath.fade(Gui.color, current);
		}

		internal static void endDisabled()
		{
			if (Widgets.kDisabledStack.Count == 0)
				return;

			(Color color, bool enabled, bool disabled) saved = Widgets.kDisabledStack.Pop();
			Gui.color = saved.color;
			Gui.enabled = saved.enabled;
			if (saved.disabled)
				Widgets.m_disabledScopes--;
		}

		internal static int disabledDepth => Widgets.kDisabledStack.Count;

		// a reveal clip or a covered window only switches input off for a frame and is not a scope
		internal static bool disabledScoped => Widgets.m_disabledScopes > 0;

		// a block that throws leaves its scopes open and the caller restores the gui state itself
		internal static void trimDisabled(int depth)
		{
			while (Widgets.kDisabledStack.Count > depth)
			{
				if (Widgets.kDisabledStack.Pop().disabled)
					Widgets.m_disabledScopes--;
			}
		}

		private static int nextDisabled()
		{
			if (Widgets.m_disabledPass != Gui.passToken)
			{
				Widgets.m_disabledPass = Gui.passToken;

				Widgets.kDisabledCounts.Clear();
			}

			int block = MenuHost.m_currentBlock;
			Widgets.kDisabledCounts.TryGetValue(block, out int occurrence);
			Widgets.kDisabledCounts[block] = occurrence + 1;
			return unchecked(((int)block * Widgets.kKeyMix) ^ occurrence);
		}

		private static float stepDisabled(int id, bool disabled)
		{
			ref MotionValue light = ref CollectionsMarshal.GetValueRefOrAddDefault(Widgets.kDisabledAlphas, id, out bool exists);
			if (!exists)
				light = MotionValue.at(disabled ? 0f : 1f);

			return FastMath.lerp(ThemeConstants.Chrome.kDisabledAlpha, 1f, Fade.cut(ref light, !disabled, MenuAnimation.enabled));
		}
	}
}
