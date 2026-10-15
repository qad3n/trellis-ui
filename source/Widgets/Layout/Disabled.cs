using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Trellis
{
	internal static partial class Widgets
	{
		private const float kDisabledAlpha = 0.35f;
		private const float kOpaqueThreshold = 0.999f;

		private static readonly Stack<(Color color, bool enabled)> kDisabledStack = new Stack<(Color color, bool enabled)>();
		private static readonly Dictionary<int, MotionValue> kDisabledAlphas = new Dictionary<int, MotionValue>();
		private static readonly Dictionary<int, int> kDisabledCounts = new Dictionary<int, int>();

		private static int m_disabledPass = -1;

		internal static void beginDisabled(bool disabled)
		{
			int id = Widgets.nextDisabled();

			float current = Widgets.stepDisabled(id, disabled);

			Widgets.kDisabledStack.Push((Gui.color, Gui.enabled));

			if (disabled)
				Gui.enabled = false;

			if (!(current < Widgets.kOpaqueThreshold))
				return;

			Gui.color = ColorMath.fade(Gui.color, current);
		}

		internal static void endDisabled()
		{
			if (Widgets.kDisabledStack.Count == 0)
				return;

			(Color color, bool enabled) saved = Widgets.kDisabledStack.Pop();
			Gui.color = saved.color;
			Gui.enabled = saved.enabled;
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

			return FastMath.lerp(Widgets.kDisabledAlpha, 1f, Fade.cut(ref light, !disabled, MenuAnimation.enabled));
		}
	}
}
