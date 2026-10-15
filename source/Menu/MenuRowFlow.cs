using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Trellis
{
	internal static class MenuRowFlow
	{
		private const int kRootContainer = 1;

		private static readonly Dictionary<int, BlockRows> kBlockRows = new Dictionary<int, BlockRows>();
		private static readonly List<int> kPruneScratch = new List<int>();
		private static readonly List<int> kContainerStack = new List<int>();

		private static BlockRows m_current;
		private static int m_emittedKey;
		private static int m_rowDepth;

		internal struct RowScope
		{
			internal bool m_active;
			internal bool m_faded;
			internal bool m_clipped;
			internal int m_key;
			internal float m_startY;
			internal float m_progress;
			internal Color m_savedColor;
		}

		private static class Limits
		{
			internal const float kMinReveal = 2f;
			internal const float kHeightEpsilon = 0.01f;
			internal const float kBulkRatio = 0.5f;
			internal const int kBulkFloor = 4;
		}

		private struct RowState
		{
			internal float m_height;
			internal MotionValue m_payout;
			internal MotionValue m_light;
			internal bool m_settled;
			internal int m_container;
		}

		private struct GhostRow
		{
			internal int m_afterKey;
			internal float m_height;
			internal MotionValue m_payout;
			internal bool m_emitted;
		}

		private sealed class BlockRows
		{
			internal readonly List<int> m_order = new List<int>();
			internal readonly List<int> m_seen = new List<int>();
			internal readonly HashSet<int> m_seenSet = new HashSet<int>();
			internal readonly Dictionary<int, RowState> m_rows = new Dictionary<int, RowState>();
			internal readonly List<GhostRow> m_ghosts = new List<GhostRow>();
			internal readonly HashSet<int> m_enteredNow = new HashSet<int>();
			internal readonly HashSet<int> m_enteredPrev = new HashSet<int>();
			internal int m_arrived;
			internal bool m_initialized;
		}

		private static int currentContainer => MenuRowFlow.kContainerStack.Count > 0 ? MenuRowFlow.kContainerStack[^1] : MenuRowFlow.kRootContainer;

		internal static void reset()
		{
			MenuRowFlow.kBlockRows.Clear();

			MenuRowFlow.m_current = null;
			MenuRowFlow.m_emittedKey = 0;
			MenuRowFlow.m_rowDepth = 0;

			MenuRowFlow.kContainerStack.Clear();
		}

		internal static void beginBlock(int id)
		{
			if (!MenuRowFlow.kBlockRows.TryGetValue(id, out BlockRows rows))
			{
				rows = new BlockRows();
				MenuRowFlow.kBlockRows[id] = rows;
			}
			MenuRowFlow.m_current = rows;
			MenuRowFlow.m_emittedKey = 0;
			MenuRowFlow.m_rowDepth = 0;

			MenuRowFlow.kContainerStack.Clear();

			rows.m_seen.Clear();
			rows.m_seenSet.Clear();
			rows.m_enteredNow.Clear();
			rows.m_enteredNow.Add(MenuRowFlow.kRootContainer);
			for (int i = 0; i < rows.m_ghosts.Count; i++)
			{
				GhostRow ghost = rows.m_ghosts[i];
				ghost.m_emitted = false;
				rows.m_ghosts[i] = ghost;
			}
		}

		internal static float endBlock(float contentH, bool drewCleanly)
		{
			BlockRows rows = MenuRowFlow.m_current;

			MenuRowFlow.m_current = null;

			if (rows == null)
				return contentH;

			contentH += MenuRowFlow.emitGhosts(rows, MenuRowFlow.m_emittedKey);

			if (drewCleanly && Gui.repainting)
				MenuRowFlow.commit(rows);
			return contentH;
		}

		internal static void pushContainer(int id)
		{
			MenuRowFlow.kContainerStack.Add(id);

			MenuRowFlow.m_current?.m_enteredNow.Add(id);
		}

		internal static void popContainer()
		{
			if (MenuRowFlow.kContainerStack.Count <= 0)
				return;

			MenuRowFlow.kContainerStack.RemoveAt(MenuRowFlow.kContainerStack.Count - 1);
		}

		internal static void abort()
		{
			MenuRowFlow.m_current = null;
			MenuRowFlow.m_rowDepth = 0;

			MenuRowFlow.kContainerStack.Clear();
		}

		internal static RowScope beginRow(ref float y, int key)
		{
			BlockRows rows = MenuRowFlow.m_current;
			if (rows == null || key == 0 || MenuRowFlow.m_rowDepth > 0)
				return default;
			if (!rows.m_seenSet.Add(key))
				return default;

			y += MenuRowFlow.emitGhosts(rows, MenuRowFlow.m_emittedKey);

			MenuRowFlow.m_emittedKey = key;

			rows.m_seen.Add(key);

			int container = MenuRowFlow.currentContainer;
			if (!rows.m_rows.TryGetValue(key, out RowState state))
			{
				bool animate = rows.m_initialized && AnimationConfig.rowsEnabled && rows.m_enteredPrev.Contains(container);

				state.m_height = 0f;
				state.m_settled = !animate;
				state.m_payout = MotionValue.at(animate ? 0f : 1f);
				state.m_light = MotionValue.at(animate ? 0f : 1f);
				if (animate)
				{
					state.m_payout.reel(1f, MenuRowFlow.payoutProfile(Limits.kMinReveal));
					state.m_light.reel(1f, MotionProfile.light(true));
				}
				state.m_container = container;
				rows.m_rows[key] = state;
				rows.m_arrived++;
			}
			else if (state.m_container != container)
			{
				state.m_container = container;
				rows.m_rows[key] = state;
			}

			MenuRowFlow.m_rowDepth++;

			RowScope scope = new RowScope
			{
				m_active = true,
				m_key = key,
				m_startY = y,
				m_progress = MenuRowFlow.enterProgress(rows.m_rows, key)
			};
			if (scope.m_progress >= 1f)
				return scope;

			scope.m_savedColor = Gui.color;
			Gui.color = ColorMath.fade(scope.m_savedColor, MenuRowFlow.enterLight(rows.m_rows, key));

			scope.m_faded = true;

			float full = state.m_height > 0f ? state.m_height : Limits.kMinReveal;
			ContentClip.push(y, FastMath.max(Limits.kMinReveal, full * scope.m_progress));

			scope.m_clipped = true;
			return scope;
		}

		internal static void endRow(ref float y, ref RowScope scope)
		{
			if (!scope.m_active)
				return;

			scope.m_active = false;

			MenuRowFlow.m_rowDepth--;

			float natural = y - scope.m_startY;

			if (scope.m_clipped)
				ContentClip.pop();

			if (scope.m_faded)
				Gui.color = scope.m_savedColor;

			BlockRows rows = MenuRowFlow.m_current;
			if (rows != null)
			{
				ref RowState state = ref CollectionsMarshal.GetValueRefOrNullRef(rows.m_rows, scope.m_key);
				if (!Unsafe.IsNullRef(ref state))
				{
					state.m_height = natural;
					if (!state.m_settled)
						state.m_payout.retune(MenuRowFlow.payoutProfile(natural));
				}
			}

			if (scope.m_progress >= 1f)
				return;

			float shown = natural * scope.m_progress;
			y = scope.m_startY + shown;

			MenuAnimation.noteChild(shown, natural);
		}

		private static float enterProgress(Dictionary<int, RowState> table, int key)
		{
			ref RowState state = ref CollectionsMarshal.GetValueRefOrNullRef(table, key);
			if (Unsafe.IsNullRef(ref state) || state.m_settled)
				return 1f;

			float paid = FastMath.clamp01(state.m_payout.sample());
			if (paid >= 1f && state.m_light.atRest)
				state.m_settled = true;
			return paid;
		}

		private static float enterLight(Dictionary<int, RowState> table, int key)
		{
			ref RowState state = ref CollectionsMarshal.GetValueRefOrNullRef(table, key);
			return Unsafe.IsNullRef(ref state) ? 1f : state.m_light.sampleLight();
		}

		private static MotionProfile payoutProfile(float height) => MotionProfile.payout(MotionMath.extent(MenuHost.m_contentW, height), height, AnimationConfig.rowsEnabled);

		private static float emitGhosts(BlockRows rows, int afterKey)
		{
			if (rows.m_ghosts.Count == 0)
				return 0f;

			float total = 0f;

			Span<GhostRow> ghosts = CollectionsMarshal.AsSpan(rows.m_ghosts);
			for (int i = 0; i < ghosts.Length; i++)
			{
				ref GhostRow ghost = ref ghosts[i];
				if (ghost.m_emitted || ghost.m_afterKey != afterKey)
					continue;

				ghost.m_emitted = true;

				float height = ghost.m_height * FastMath.clamp01(ghost.m_payout.sample());
				if (height <= Limits.kHeightEpsilon)
					continue;

				total += height;

				MenuAnimation.noteChild(height, 0f);
			}
			return total;
		}

		private static void commit(BlockRows rows)
		{
			if (!rows.m_initialized)
			{
				rows.m_initialized = true;
				MenuRowFlow.settle(rows);
			}
			else if (MenuRowFlow.bulkChange(rows))
			{
				rows.m_ghosts.Clear();

				MenuRowFlow.settle(rows);
			}
			else
				MenuRowFlow.buildGhosts(rows);
			rows.m_arrived = 0;
			MenuRowFlow.dropGhosts(rows);
			MenuRowFlow.pruneRows(rows);

			rows.m_order.Clear();
			rows.m_order.AddRange(rows.m_seen);
			rows.m_enteredPrev.Clear();
			foreach (int container in rows.m_enteredNow)
				rows.m_enteredPrev.Add(container);
		}

		private static void settle(BlockRows rows)
		{
			for (int i = 0; i < rows.m_seen.Count; i++)
			{
				ref RowState state = ref CollectionsMarshal.GetValueRefOrNullRef(rows.m_rows, rows.m_seen[i]);
				state.m_settled = true;
				state.m_payout.snap(1f);
				state.m_light.snap(1f);
			}
		}

		private static bool bulkChange(BlockRows rows)
		{
			int changed = rows.m_arrived;
			for (int i = 0; i < rows.m_order.Count; i++)
			{
				if (rows.m_seenSet.Contains(rows.m_order[i]))
					continue;

				changed++;
			}
			if (changed < Limits.kBulkFloor)
				return false;

			int reference = FastMath.max(rows.m_order.Count, rows.m_seen.Count);
			return changed >= reference * Limits.kBulkRatio;
		}

		private static void buildGhosts(BlockRows rows)
		{
			if (!AnimationConfig.rowsEnabled)
				return;

			for (int i = 0; i < rows.m_order.Count; i++)
			{
				int key = rows.m_order[i];
				if (rows.m_seenSet.Contains(key))
					continue;
				if (!rows.m_rows.TryGetValue(key, out RowState state) || state.m_height <= Limits.kHeightEpsilon)
					continue;
				if (!rows.m_enteredNow.Contains(state.m_container))
					continue;

				MotionValue payout = state.m_settled ? MotionValue.at(1f) : state.m_payout;
				payout.vent(0f, MenuRowFlow.payoutProfile(state.m_height));
				rows.m_ghosts.Add(new GhostRow
				{
					m_afterKey = MenuRowFlow.precedingKey(rows, i),
					m_height = state.m_height,
					m_payout = payout
				});
			}
		}

		private static int precedingKey(BlockRows rows, int index)
		{
			for (int i = index - 1; i >= 0; i--)
			{
				if (rows.m_seenSet.Contains(rows.m_order[i]))
					return rows.m_order[i];
			}
			return 0;
		}

		private static void dropGhosts(BlockRows rows)
		{
			Span<GhostRow> ghosts = CollectionsMarshal.AsSpan(rows.m_ghosts);
			for (int i = ghosts.Length - 1; i >= 0; i--)
			{
				if (!(ghosts[i].m_payout.sample() <= 0f))
					continue;

				rows.m_ghosts.RemoveAt(i);
			}
		}

		private static void pruneRows(BlockRows rows)
		{
			if (rows.m_rows.Count == rows.m_seenSet.Count)
				return;

			MenuRowFlow.kPruneScratch.Clear();
			foreach (KeyValuePair<int, RowState> entry in rows.m_rows)
			{
				if (rows.m_seenSet.Contains(entry.Key))
					continue;

				MenuRowFlow.kPruneScratch.Add(entry.Key);
			}

			for (int i = 0; i < MenuRowFlow.kPruneScratch.Count; i++)
				rows.m_rows.Remove(MenuRowFlow.kPruneScratch[i]);
		}
	}
}
