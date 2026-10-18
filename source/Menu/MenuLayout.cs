using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using static Trellis.Widgets;

namespace Trellis
{
	internal static partial class MenuHost
	{
		private const int kMinSplit = 2;
		private const int kMinCandidate = 2;
		internal const float kContentPad = Layout.kBlockPad;
		private const float kNoteGap = 4f;
		private const int kPageBase = 1 << 20;

		internal static readonly Vector2 kOffscreenMouse = VectorMath.v2(-9999f, -9999f);
		private static readonly Dictionary<int, BlockMeasure> kBlockMeasures = new Dictionary<int, BlockMeasure>();
		private static readonly Dictionary<long, LayoutState> kLayoutStates = new Dictionary<long, LayoutState>();
		private static readonly Dictionary<int, int> kStickyCaps = new Dictionary<int, int>();
		private static readonly Dictionary<int, DrawnPartition> kDrawnPartitions = new Dictionary<int, DrawnPartition>();

		// keyed on the static table reference so it stays as small as the set of pinned pages
		private static readonly List<BlockDraw> kPageDrawers = new List<BlockDraw>();
		private static readonly Dictionary<(int[][] columns, int cols), PinnedLayout> kPinnedLayouts = new Dictionary<(int[][] columns, int cols), PinnedLayout>();

		private static float m_viewTop;
		private static float m_viewBottom;
		private static bool m_cullAllowed;

		// several pages resolve a layout in one frame so a per call flag stranded all but the first
		private static bool m_wasResizing;
		private static bool m_resizeSettled;
		private static int m_resizeFrame = -1;
		private static int m_anchorBlock;
		private static float m_anchorSeen = float.NegativeInfinity;
		internal static Vector2 m_blockOffset;
		internal static float m_contentW;
		internal static int m_currentBlock;
		internal static bool m_inBlock;

		internal readonly struct ContentViewportScope : IDisposable
		{
			private readonly float m_savedTop;
			private readonly float m_savedBottom;
			private readonly bool m_savedAllowed;

			internal ContentViewportScope(float top, float bottom, bool allow)
			{
				m_savedTop = MenuHost.m_viewTop;
				m_savedBottom = MenuHost.m_viewBottom;
				m_savedAllowed = MenuHost.m_cullAllowed;

				MenuHost.m_viewTop = top;
				MenuHost.m_viewBottom = bottom;
				MenuHost.m_cullAllowed = allow && bottom > top;
			}

			public void Dispose()
			{
				MenuHost.m_viewTop = m_savedTop;
				MenuHost.m_viewBottom = m_savedBottom;
				MenuHost.m_cullAllowed = m_savedAllowed;
			}
		}

		private static class Layout
		{
			internal const float kBlockPad = 10f;
			internal const float kBlockGap = 12f;
			internal const float kBlockW = 340f;
			internal const float kMaxBlockW = 520f;
			internal const int kMaxCols = 4;
			internal const float kUnknownHeight = 80f;
		}

		private static class Packing
		{
			internal const float kColumnPenalty = 0.04f;
			internal const float kScoreEpsilon = 0.001f;
			internal const float kQuantumPx = 24f;
			internal const float kSlackBand = 0.15f;
			internal const float kHysteresisPx = 0.5f * (Layout.kBlockW + Layout.kBlockGap);
			internal const int kMaxRepairs = 2;
			internal const float kAnchorHold = 0.35f;
			internal const int kNoAnchor = -1;
		}

		private static class Fnv
		{
			internal const uint kOffsetBasis = 2166136261;
			internal const int kPrime = 16777619;
		}

		private static class Measure
		{
			internal const float kClipY = -100000f;
			internal const float kClipH = 50000f;
			internal const int kNoHeight = int.MinValue;
		}

		private static class Culling
		{
			internal const float kMarginPx = 96f;
		}

		private struct BlockMeasure
		{
			internal float m_liveHeight;
			internal float m_settledHeight;
		}

		private struct LayoutState
		{
			internal int m_cols;
			internal int m_heightSignature;
			internal int[] m_blocks;
			internal int[] m_indices;
			internal int[] m_colAssignments;
			internal bool m_measuring;
			internal int m_heldAnchor;
		}

		private struct PinnedLayout
		{
			internal int[] m_blocks;
			internal int[] m_declared;
			internal int[] m_indices;
			internal int[] m_colAssignments;
			internal int m_layoutKey;
			internal int m_heightSignature;
			internal bool m_measuring;
		}

		private struct DrawnPartition
		{
			internal int m_cols;
			internal int[] m_indices;
			internal int[] m_colAssignments;
			internal int m_frame;
		}

		private readonly struct MeasurePassScope : IDisposable
		{
			private readonly bool m_active;
			private readonly bool m_savedEnabled;
			private readonly TooltipFrame m_savedTooltip;
			private readonly Vector2 m_savedMouse;

			internal MeasurePassScope(bool measuring, float w)
			{
				m_active = measuring;
				m_savedEnabled = Gui.enabled;
				m_savedTooltip = MenuHost.m_frameTooltip;
				m_savedMouse = MenuHost.scrollMouse;

				if (!measuring)
					return;

				Gui.enabled = false;

				MenuHost.scrollMouse = MenuHost.kOffscreenMouse;

				Gui.beginClip(RectMath.create(0f, Measure.kClipY, w, Measure.kClipH));
			}

			public void Dispose()
			{
				if (!m_active)
					return;

				Gui.endClip();

				Gui.enabled = m_savedEnabled;

				MenuHost.m_frameTooltip = m_savedTooltip;
				MenuHost.scrollMouse = m_savedMouse;
			}
		}

		// page blocks get ids far above any host enum so the two never collide
		internal static int addPageBlock(BlockDraw draw)
		{
			MenuHost.kPageDrawers.Add(draw);
			return MenuHost.kPageBase + MenuHost.kPageDrawers.Count - 1;
		}

		internal static float drawPage(float w, float baseY, int[] blocks) => MenuHost.layoutBlocks(w, baseY, blocks);
		internal static float drawPageColumns(float w, float baseY, int[][] columns) => MenuHost.layoutColumns(w, baseY, columns);

		internal static int key<TBlock>(TBlock id) where TBlock : unmanaged, Enum => Unsafe.SizeOf<TBlock>() == sizeof(int) ? Unsafe.As<TBlock, int>(ref id) : Convert.ToInt32(id);

		internal static ContentViewportScope beginViewport(float top, float bottom, bool allow) => new ContentViewportScope(top, bottom, allow);

		// imgui doesnt skip clipped content so still advance the cursor past a culled row
		internal static bool rowCullable(float top, float height)
		{
			if (!MenuHost.m_cullAllowed)
				return false;

			return top - Culling.kMarginPx > MenuHost.m_viewBottom || top + height + Culling.kMarginPx < MenuHost.m_viewTop;
		}

		internal static void invalidateLayout(bool clearHeights = false)
		{
			MenuHost.kLayoutStates.Clear();

			MenuHost.kStickyCaps.Clear();

			MenuHost.kDrawnPartitions.Clear();

			if (clearHeights)
				MenuHost.kBlockMeasures.Clear();

			Widgets.closeDropdown();
			Widgets.closeMultiSelect();
		}

		private static float layoutBlocks(float w, float baseY, int[] blocks)
		{
			if (blocks == null || blocks.Length == 0)
				return baseY;

			MenuHost.planLayout(blocks, w, out int cols, out int[] indices, out int[] colAssignments, out bool measuring);

			float colW = MenuHost.columnWidth(w, cols);

			Span<float> colYs = stackalloc float[cols];
			using (MenuHost.beginMeasure(measuring, w))
			{
				for (int i = 0; i < blocks.Length; i++)
				{
					int blockIndex = indices[i];
					MenuHost.drawBlock(colYs, colAssignments[blockIndex], colW, baseY, w, blocks[blockIndex], measuring);
				}
			}
			return baseY + MenuHost.trimGap(MenuHost.bottomY(colYs));
		}

		private static float layoutColumns(float w, float baseY, int[][] columns)
		{
			if (columns.Length == 0)
				return baseY;

			int cols = FastMath.min(columns.Length, LayoutMath.columnCount(w, Layout.kBlockW, Layout.kBlockGap, Layout.kMaxCols));

			cols = FastMath.max(1, cols);

			PinnedLayout layout = MenuHost.pinnedLayout(columns, cols);
			if (layout.m_blocks.Length == 0)
				return baseY;
			return MenuHost.drawGrid(w, baseY, layout.m_blocks, layout.m_colAssignments, cols);
		}

		private static PinnedLayout pinnedLayout(int[][] columns, int cols)
		{
			bool known = MenuHost.kPinnedLayouts.TryGetValue((columns, cols), out PinnedLayout layout);
			if (!known)
			{
				layout = MenuHost.flattenColumns(columns);
				MenuHost.kPinnedLayouts[(columns, cols)] = layout;
			}
			if (columns.Length <= cols || layout.m_blocks.Length == 0)
				return layout;

			int frame = Gui.frameCount;

			bool entering = MenuHost.entering(layout.m_layoutKey, frame);

			bool measuring = MenuHost.unmeasured(layout.m_blocks);

			bool measuringEnded = false;

			int signature = MenuHost.heightSignature(layout.m_blocks);
			if (!known || signature != layout.m_heightSignature)
			{
				measuringEnded = known && layout.m_measuring && !measuring;

				layout.m_colAssignments = MenuHost.placeOverflow(layout.m_blocks, layout.m_declared, cols);

				layout.m_heightSignature = signature;

				layout.m_measuring = measuring;
				MenuHost.kPinnedLayouts[(columns, cols)] = layout;
			}
			MenuHost.notePartition(layout.m_layoutKey, layout.m_blocks, cols, layout.m_indices, layout.m_colAssignments, frame, measuring || measuringEnded || entering);
			return layout;
		}

		private static PinnedLayout flattenColumns(int[][] columns)
		{
			int total = 0;
			foreach (int[] column in columns)
				total += column?.Length ?? 0;

			int[] blocks = new int[total];

			int[] declared = new int[total];

			int[] indices = new int[total];

			int next = 0;
			for (int c = 0; c < columns.Length; c++)
			{
				if (columns[c] == null)
					continue;

				foreach (int block in columns[c])
				{
					blocks[next] = block;

					declared[next] = c;

					indices[next] = next;

					next++;
				}
			}

			return new PinnedLayout
			{
				m_blocks = blocks,
				m_declared = declared,
				m_indices = indices,
				m_colAssignments = declared,
				m_layoutKey = MenuHost.hashBlocks(blocks)
			};
		}

		// columns that do not fit drop each block onto the shortest column instead of piling onto the last one
		// a block only looks at earlier blocks so opening a drawer never moves its own block
		private static int[] placeOverflow(int[] blocks, int[] declared, int cols)
		{
			int[] colAssignments = new int[blocks.Length];

			Span<float> colHeights = stackalloc float[cols];

			Span<int> colCounts = stackalloc int[cols];
			for (int i = 0; i < blocks.Length; i++)
			{
				int col = declared[i] < cols ? declared[i] : MenuHost.shortestColumn(colHeights);
				colAssignments[i] = col;

				colHeights[col] += (colCounts[col] > 0 ? Layout.kBlockGap : 0f) + MenuHost.settledHeight(blocks[i]);

				colCounts[col]++;
			}
			return colAssignments;
		}

		internal static void drawBanner(ref float y, float w, string title, string detail, Color accent)
		{
			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, Widgets.RowPrefix.kBanner, null);
			Widgets.statusBanner(MenuHost.m_blockOffset.x, MenuHost.m_blockOffset.y + y, w, title, detail, accent, out float height);
			y += height + Widgets.kFieldGap;
			Widgets.endRow(ref y, ref row);
		}

		internal static void drawStatus(ref float y, float w, string text) => MenuHost.drawWrapped(ref y, w, UiText.small(text), Theme.statusLabel, MenuHost.kNoteGap);
		internal static void drawNote(ref float y, float w, string text) => MenuHost.drawWrapped(ref y, w, UiText.small(text), Theme.valueLabel, MenuHost.kNoteGap);
		internal static void drawNote(ref float y, float w, string text, Color tone) => MenuHost.drawWrapped(ref y, w, UiText.tinted(UiText.small(text), tone), Theme.valueLabel, MenuHost.kNoteGap);

		internal static void drawWrapped(ref float y, float w, string text, GUIStyle baseStyle = null, float gap = Widgets.kFieldGap)
		{
			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, Widgets.RowPrefix.kLabel, null);
			UiText.wrappedLabel(MenuHost.m_blockOffset.x, MenuHost.m_blockOffset.y + y, w, text, baseStyle ?? Theme.label, out float height);
			y += height + gap;
			Widgets.endRow(ref y, ref row);
		}

		private static bool blockCullable(int id, float top, float height)
		{
			if (!MenuHost.kBlockMeasures.ContainsKey(id))
				return false;

			return MenuHost.rowCullable(top, height);
		}

		private static float drawGrid(float w, float baseY, int[] blocks, int[] colAssignments, int cols)
		{
			bool measuring = MenuHost.unmeasured(blocks);

			float colW = MenuHost.columnWidth(w, cols);

			Span<float> colYs = stackalloc float[cols];
			using (MenuHost.beginMeasure(measuring, w))
			{
				for (int i = 0; i < blocks.Length; i++)
					MenuHost.drawBlock(colYs, colAssignments[i], colW, baseY, w, blocks[i], measuring);
			}

			if (measuring)
				MenuAnimation.snapBlocks(blocks);
			return baseY + MenuHost.trimGap(MenuHost.bottomY(colYs));
		}

		private static MeasurePassScope beginMeasure(bool measuring, float w) => new MeasurePassScope(measuring, w);

		private static float bottomY(Span<float> colYs)
		{
			float max = 0f;
			for (int c = 0; c < colYs.Length; c++)
			{
				if (!(colYs[c] > max))
					continue;

				max = colYs[c];
			}
			return max;
		}

		private static float trimGap(float maxColY) => maxColY > 0f ? maxColY - Layout.kBlockGap : 0f;

		private static float columnWidth(float availableW, int cols)
		{
			float colW = (availableW - Layout.kBlockGap * (cols - 1)) / cols;
			return colW > Layout.kMaxBlockW ? Layout.kMaxBlockW : colW;
		}

		private static void planLayout(int[] blocks, float availableW, out int cols, out int[] indices, out int[] colAssignments, out bool measuring)
		{
			int layoutKey = MenuHost.hashBlocks(blocks);

			bool resizeActive = MenuHost.m_drag.active;

			int frame = Gui.frameCount;
			if (frame != MenuHost.m_resizeFrame)
			{
				MenuHost.m_resizeFrame = frame;
				MenuHost.m_resizeSettled = !resizeActive && MenuHost.m_wasResizing;
				MenuHost.m_wasResizing = resizeActive;
			}

			bool resizeSettled = MenuHost.m_resizeSettled;

			bool entering = MenuHost.entering(layoutKey, frame);

			int widthCap = MenuHost.findCap(layoutKey, availableW, resizeActive, resizeSettled);

			long stateKey = MenuHost.stateKey(layoutKey, widthCap);

			int signature = MenuHost.heightSignature(blocks);

			bool hit = MenuHost.kLayoutStates.TryGetValue(stateKey, out LayoutState cached) && MenuHost.sameBlocks(cached.m_blocks, blocks);
			if (hit && (resizeActive || (!resizeSettled && !entering && cached.m_heightSignature == signature && !MenuHost.anchorReleased(blocks, cached))))
			{
				MenuHost.reuseCached(cached, out cols, out indices, out colAssignments, out measuring);
				MenuHost.notePartition(layoutKey, blocks, cols, indices, colAssignments, frame, measuring);
				return;
			}
			measuring = MenuHost.unmeasured(blocks);
			if (hit && !cached.m_measuring && !resizeSettled && !entering && !measuring)
			{
				int heldAnchor = MenuHost.repairPartition(blocks, cached, widthCap, MenuHost.findAnchor(blocks), out cols, out indices, out colAssignments);
				MenuHost.storeLayout(stateKey, blocks, signature, cols, indices, colAssignments, measuring, heldAnchor);
				MenuHost.notePartition(layoutKey, blocks, cols, indices, colAssignments, frame, false);
				return;
			}
			MenuHost.buildLayout(blocks, widthCap, out cols, out indices, out colAssignments);
			MenuHost.storeLayout(stateKey, blocks, signature, cols, indices, colAssignments, measuring, Packing.kNoAnchor);

			bool measuringEnded = hit && cached.m_measuring && !measuring;
			MenuHost.notePartition(layoutKey, blocks, cols, indices, colAssignments, frame, measuring || measuringEnded || entering);
		}

		private static bool entering(int layoutKey, int frame) => !MenuHost.kDrawnPartitions.TryGetValue(layoutKey, out DrawnPartition prev) || prev.m_frame < frame - 1;

		private static void notePartition(int layoutKey, int[] blocks, int cols, int[] indices, int[] colAssignments, int frame, bool snap)
		{
			bool known = MenuHost.kDrawnPartitions.TryGetValue(layoutKey, out DrawnPartition prev);

			bool changed = !known || prev.m_cols != cols || !MenuHost.sameInts(prev.m_indices, indices) || !MenuHost.sameInts(prev.m_colAssignments, colAssignments);

			MenuHost.kDrawnPartitions[layoutKey] = new DrawnPartition
			{
				m_cols = cols,
				m_indices = indices,
				m_colAssignments = colAssignments,
				m_frame = frame
			};
			if (snap)
				MenuAnimation.snapBlocks(blocks);
			else if (changed)
				MenuAnimation.beginShuffle(blocks, known && prev.m_cols == cols);
		}

		private static void noteHovered(int id, Rect blockRect)
		{
			if (MenuHost.popupShown || !RectMath.contains(blockRect, MenuHost.scrollMouse))
				return;

			MenuHost.m_anchorBlock = id;
			MenuHost.m_anchorSeen = Gui.unscaledTime;
		}

		private static int findAnchor(int[] blocks)
		{
			if (!MenuHost.popupShown && Gui.unscaledTime - MenuHost.m_anchorSeen > Packing.kAnchorHold)
				return Packing.kNoAnchor;

			for (int i = 0; i < blocks.Length; i++)
			{
				if (blocks[i] == MenuHost.m_anchorBlock)
					return i;
			}
			return Packing.kNoAnchor;
		}

		private static bool anchorReleased(int[] blocks, LayoutState cached) => cached.m_heldAnchor != Packing.kNoAnchor && MenuHost.findAnchor(blocks) == Packing.kNoAnchor;

		private static void reuseCached(LayoutState cached, out int cols, out int[] indices, out int[] colAssignments, out bool measuring)
		{
			cols = cached.m_cols;

			indices = cached.m_indices;

			colAssignments = cached.m_colAssignments;

			measuring = cached.m_measuring;
		}

		private static int findCap(int layoutKey, float availableW, bool freeze, bool settle)
		{
			int rawCap = LayoutMath.columnCount(availableW, Layout.kBlockW, Layout.kBlockGap, Layout.kMaxCols);
			if (settle || !MenuHost.kStickyCaps.TryGetValue(layoutKey, out int sticky))
			{
				MenuHost.kStickyCaps[layoutKey] = rawCap;
				return rawCap;
			}
			if (freeze || rawCap == sticky)
				return sticky;

			int resolved = sticky;
			if (rawCap > sticky)
			{
				if (availableW >= MenuHost.capThreshold(sticky + 1) + Packing.kHysteresisPx)
					resolved = rawCap;
			}
			else if (availableW <= MenuHost.capThreshold(sticky) - Packing.kHysteresisPx)
				resolved = rawCap;
			MenuHost.kStickyCaps[layoutKey] = resolved;
			return resolved;
		}

		private static float capThreshold(int cap) => cap * (Layout.kBlockW + Layout.kBlockGap) - Layout.kBlockGap;

		private static int repairPartition(int[] blocks, LayoutState cached, int widthCap, int anchor, out int cols, out int[] indices, out int[] colAssignments)
		{
			int held = anchor == Packing.kNoAnchor ? Packing.kNoAnchor : cached.m_heldAnchor;

			cols = cached.m_cols;

			indices = cached.m_indices;

			colAssignments = cached.m_colAssignments;

			float[] heights = MenuHost.settledHeights(blocks);
			MenuHost.partition(heights, cols, out int[] assignments, out float optimalMax);

			float tolerated = optimalMax * (1f + Packing.kSlackBand);
			if (MenuHost.maxHeight(colAssignments, cols, heights) <= tolerated)
				return held;
			if (MenuHost.hopTail(heights, cols, anchor, held, tolerated, ref indices, ref colAssignments))
				return held;

			MenuHost.buildLayout(blocks, widthCap, out int fullCols, out int[] fullIndices, out int[] fullAssignments);
			if (!MenuHost.keepsSlot(held, heights, cols, indices, colAssignments, fullCols, fullIndices, fullAssignments))
				return held;
			if (!MenuHost.keepsSlot(anchor, heights, cols, indices, colAssignments, fullCols, fullIndices, fullAssignments))
				return anchor;

			cols = fullCols;

			indices = fullIndices;

			colAssignments = fullAssignments;
			return Packing.kNoAnchor;
		}

		private static bool hopTail(float[] heights, int cols, int anchor, int held, float tolerated, ref int[] indices, ref int[] colAssignments)
		{
			Span<float> colHeights = stackalloc float[cols];

			Span<int> colCounts = stackalloc int[cols];
			MenuHost.columnHeights(colAssignments, heights, colHeights, colCounts);

			int[] hopAssignments = null;

			int[] hopIndices = null;
			for (int hop = 0; hop < Packing.kMaxRepairs; hop++)
			{
				int tallest = MenuHost.tallestColumn(colHeights);

				int shortest = MenuHost.shortestColumn(colHeights);

				if (colHeights[tallest] <= tolerated || colCounts[tallest] < 2)
					break;

				int tail = MenuHost.columnTail(hopIndices ?? indices, hopAssignments ?? colAssignments, tallest);
				if (tail == anchor || tail == held)
					break;

				float landed = colHeights[shortest] + (colCounts[shortest] > 0 ? Layout.kBlockGap : 0f) + heights[tail];
				if (landed > colHeights[tallest] - Packing.kQuantumPx)
					break;

				if (hopIndices == null)
				{
					hopIndices = (int[])indices.Clone();

					hopAssignments = (int[])colAssignments.Clone();
				}
				MenuHost.moveLast(hopIndices, tail);

				hopAssignments[tail] = shortest;

				colHeights[tallest] -= heights[tail] + Layout.kBlockGap;

				colCounts[tallest]--;

				colHeights[shortest] = landed;

				colCounts[shortest]++;
			}

			if (hopIndices != null)
			{
				indices = hopIndices;

				colAssignments = hopAssignments;
			}
			return colHeights[MenuHost.tallestColumn(colHeights)] <= tolerated;
		}

		private static void columnHeights(int[] colAssignments, float[] heights, Span<float> colHeights, Span<int> colCounts)
		{
			for (int i = 0; i < heights.Length; i++)
			{
				int c = colAssignments[i];
				colHeights[c] += colCounts[c] > 0 ? heights[i] + Layout.kBlockGap : heights[i];

				colCounts[c]++;
			}
		}

		private static int tallestColumn(Span<float> colHeights)
		{
			int best = 0;
			for (int c = 1; c < colHeights.Length; c++)
			{
				if (!(colHeights[c] > colHeights[best]))
					continue;

				best = c;
			}
			return best;
		}

		private static int shortestColumn(Span<float> colHeights)
		{
			int best = 0;
			for (int c = 1; c < colHeights.Length; c++)
			{
				if (!(colHeights[c] < colHeights[best]))
					continue;

				best = c;
			}
			return best;
		}

		private static int columnTail(int[] indices, int[] colAssignments, int col)
		{
			for (int i = indices.Length - 1; i >= 0; i--)
			{
				if (colAssignments[indices[i]] == col)
					return indices[i];
			}
			return Packing.kNoAnchor;
		}

		private static void moveLast(int[] indices, int blockIndex)
		{
			int at = Array.IndexOf(indices, blockIndex);
			for (int i = at; i < indices.Length - 1; i++)
				indices[i] = indices[i + 1];
			indices[indices.Length - 1] = blockIndex;
		}

		private static bool keepsSlot(int blockIndex, float[] heights, int colsA, int[] indicesA, int[] assignmentsA, int colsB, int[] indicesB, int[] assignmentsB)
		{
			if (blockIndex == Packing.kNoAnchor)
				return true;

			return colsA == colsB && assignmentsA[blockIndex] == assignmentsB[blockIndex] && FastMath.abs(MenuHost.slotTop(blockIndex, heights, indicesA, assignmentsA) - MenuHost.slotTop(blockIndex, heights, indicesB, assignmentsB)) < Packing.kScoreEpsilon;
		}

		private static float slotTop(int blockIndex, float[] heights, int[] indices, int[] colAssignments)
		{
			int col = colAssignments[blockIndex];

			float top = 0f;
			for (int i = 0; i < indices.Length && indices[i] != blockIndex; i++)
			{
				if (colAssignments[indices[i]] != col)
					continue;

				top += heights[indices[i]] + Layout.kBlockGap;
			}
			return top;
		}

		private static float[] settledHeights(int[] blocks)
		{
			float[] heights = new float[blocks.Length];
			for (int i = 0; i < blocks.Length; i++)
				heights[i] = MenuHost.settledHeight(blocks[i]);
			return heights;
		}

		private static long stateKey(int key, int widthCap) => ((long)key << 32) | (uint)widthCap;

		private static void buildLayout(int[] blocks, int widthCap, out int cols, out int[] indices, out int[] colAssignments)
		{
			int n = blocks.Length;
			indices = new int[n];
			for (int i = 0; i < n; i++)
				indices[i] = i;

			float[] heights = MenuHost.settledHeights(blocks);

			int maxCols = FastMath.min(widthCap, n);
			cols = MenuHost.pickColumns(maxCols, heights);
			MenuHost.partition(heights, cols, out colAssignments, out float maxHeight);
		}

		private static void storeLayout(long stateKey, int[] blocks, int signature, int cols, int[] indices, int[] colAssignments, bool measuring, int heldAnchor)
		{
			int[] blockSnapshot = new int[blocks.Length];
			Array.Copy(blocks, blockSnapshot, blocks.Length);

			MenuHost.kLayoutStates[stateKey] = new LayoutState
			{
				m_cols = cols,
				m_heightSignature = signature,
				m_blocks = blockSnapshot,
				m_indices = indices,
				m_colAssignments = colAssignments,
				m_measuring = measuring,
				m_heldAnchor = heldAnchor
			};
		}

		private static int heightSignature(int[] blocks)
		{
			unchecked
			{
				int hash = (int)Fnv.kOffsetBasis;
				foreach (int block in blocks)
				{
					int quantized = MenuHost.kBlockMeasures.TryGetValue(block, out BlockMeasure measure) ? FastMath.roundToInt(measure.m_settledHeight / Packing.kQuantumPx) : Measure.kNoHeight;

					hash = (hash ^ quantized) * Fnv.kPrime;
				}
				return hash;
			}
		}

		private static bool unmeasured(int[] blocks)
		{
			foreach (int block in blocks)
			{
				if (!MenuHost.kBlockMeasures.ContainsKey(block))
					return true;
			}
			return false;
		}

		private static int pickColumns(int maxCols, float[] heights)
		{
			if (maxCols <= 1)
				return 1;

			float bestScore = MenuHost.columnScore(1, maxCols, heights);

			int bestCols = 1;
			for (int candidate = MenuHost.kMinCandidate; candidate <= maxCols; candidate++)
			{
				float score = MenuHost.columnScore(candidate, maxCols, heights);
				if (!(score < bestScore - Packing.kScoreEpsilon))
					continue;

				bestScore = score;

				bestCols = candidate;
			}
			return bestCols;
		}

		private static float columnScore(int candidate, int maxCols, float[] heights)
		{
			MenuHost.partition(heights, candidate, out int[] assignments, out float maxHeight);
			return candidate == 1 ? maxHeight : maxHeight * (1f + Packing.kColumnPenalty * (maxCols - candidate));
		}

		private static void partition(float[] heights, int cols, out int[] assignments, out float maxHeight)
		{
			int n = heights.Length;
			assignments = new int[n];

			if (n == 0)
			{
				maxHeight = 0f;
				return;
			}

			float[] prefix = new float[n + 1];
			for (int i = 0; i < n; i++)
				prefix[i + 1] = prefix[i] + heights[i];

			if (cols <= 1)
			{
				maxHeight = MenuHost.spanHeight(prefix, 0, n);
				return;
			}

			if (cols >= n)
			{
				maxHeight = 0f;
				for (int i = 0; i < n; i++)
				{
					assignments[i] = i;

					if (!(heights[i] > maxHeight))
						continue;

					maxHeight = heights[i];
				}
				return;
			}

			float[,] dpMax = new float[cols + 1, n + 1];

			float[,] dpSumSq = new float[cols + 1, n + 1];

			int[,] parent = new int[cols + 1, n + 1];
			for (int i = 1; i <= n; i++)
			{
				float h = MenuHost.spanHeight(prefix, 0, i);
				dpMax[1, i] = h;

				dpSumSq[1, i] = h * h;

				parent[1, i] = 0;
			}

			for (int c = MenuHost.kMinSplit; c <= cols; c++)
			{
				for (int i = c; i <= n; i++)
				{
					int bestStart = c - 1;

					float bestSumSq = float.MaxValue;

					float bestMax = float.MaxValue;
					for (int j = c - 1; j < i; j++)
					{
						float lastColumn = MenuHost.spanHeight(prefix, j, i);

						float candMax = FastMath.max(dpMax[c - 1, j], lastColumn);

						float candSumSq = dpSumSq[c - 1, j] + lastColumn * lastColumn;

						bool better = candMax < bestMax - Packing.kScoreEpsilon || candMax <= bestMax + Packing.kScoreEpsilon && candSumSq < bestSumSq - Packing.kScoreEpsilon;
						if (!better)
							continue;

						bestMax = candMax;

						bestSumSq = candSumSq;

						bestStart = j;
					}
					dpMax[c, i] = bestMax;

					dpSumSq[c, i] = bestSumSq;

					parent[c, i] = bestStart;
				}
			}
			maxHeight = dpMax[cols, n];

			int end = n;
			for (int c = cols; c >= 1; c--)
			{
				int start = parent[c, end];
				for (int i = start; i < end; i++)
					assignments[i] = c - 1;

				end = start;
			}
		}

		private static float spanHeight(float[] prefix, int start, int end) => prefix[end] - prefix[start] + (end - 1 - start) * Layout.kBlockGap;

		private static float maxHeight(int[] colAssignments, int cols, float[] heights)
		{
			Span<float> sums = stackalloc float[cols];

			Span<int> counts = stackalloc int[cols];
			for (int i = 0; i < heights.Length; i++)
			{
				int c = colAssignments[i];
				sums[c] += heights[i];

				counts[c]++;
			}

			float max = 0f;
			for (int c = 0; c < cols; c++)
			{
				float columnHeight = counts[c] > 0 ? sums[c] + (counts[c] - 1) * Layout.kBlockGap : 0f;
				if (!(columnHeight > max))
					continue;

				max = columnHeight;
			}
			return max;
		}

		private static float liveHeight(int id) => MenuHost.kBlockMeasures.TryGetValue(id, out BlockMeasure measure) ? measure.m_liveHeight : Layout.kUnknownHeight;

		private static float chromeHeight(int id)
		{
			if (!MenuHost.kBlockMeasures.TryGetValue(id, out BlockMeasure measure))
				return Layout.kUnknownHeight;

			return FastMath.max(measure.m_liveHeight, measure.m_settledHeight);
		}

		private static float settledHeight(int id)
		{
			if (!MenuHost.kBlockMeasures.TryGetValue(id, out BlockMeasure measure))
				return Layout.kUnknownHeight;

			return FastMath.round(measure.m_settledHeight / Packing.kQuantumPx) * Packing.kQuantumPx;
		}

		private static int hashBlocks(int[] blocks)
		{
			unchecked
			{
				int hash = (int)Fnv.kOffsetBasis;

				hash = (hash ^ blocks.Length) * Fnv.kPrime;
				foreach (int block in blocks)
					hash = (hash ^ (int)block) * Fnv.kPrime;
				return hash;
			}
		}

		private static bool sameBlocks(int[] a, int[] b)
		{
			if (a.Length != b.Length)
				return false;

			for (int i = 0; i < a.Length; i++)
			{
				if (a[i] != b[i])
					return false;
			}
			return true;
		}

		private static bool sameInts(int[] a, int[] b)
		{
			if (a == null || b == null || a.Length != b.Length)
				return false;

			for (int i = 0; i < a.Length; i++)
			{
				if (a[i] != b[i])
					return false;
			}
			return true;
		}

		private static void drawBlock(Span<float> colYs, int col, float colW, float baseY, float availableW, int id, bool measuring)
		{
			float predictedH = MenuHost.liveHeight(id);

			float blockX = col * (colW + Layout.kBlockGap);

			float blockY = baseY + colYs[col];

			Vector4 target = VectorMath.v4(blockX, blockY, colW, predictedH);

			Vector4 displayed = MenuAnimation.displayedRect(id, target);
			displayed.w = target.w;
			displayed.z = FastMath.min(target.z, availableW);
			displayed.x = FastMath.clamp(displayed.x, 0f, FastMath.max(0f, availableW - displayed.z));

			float innerW = displayed.z - Layout.kBlockPad * 2f;

			float chromeH = FastMath.max(displayed.w, MenuHost.chromeHeight(id));
			if (!measuring && MenuHost.blockCullable(id, displayed.y, chromeH))
			{
				colYs[col] += predictedH + Layout.kBlockGap;
				return;
			}

			Rect blockRect = RectMath.create(displayed.x, displayed.y, displayed.z, chromeH);
			using (MenuAnimation.BlockEntryScope entry = measuring ? default : MenuAnimation.beginEntry(id, blockRect))
			{
				if (!measuring)
					MenuHost.noteHovered(id, blockRect);

				Theme.drawBlockBg(blockRect);
				Theme.drawTopAccent(blockRect, Theme.accentFaint);

				float hopGlow = MenuAnimation.hopGlow(id);
				if (hopGlow > 0f)
					Theme.drawOutline(blockRect, Theme.hopTint(hopGlow));

				Vector2 prevOffset = MenuHost.m_blockOffset;

				float prevWidth = MenuHost.m_contentW;

				int prevBlock = MenuHost.m_currentBlock;

				MenuHost.m_blockOffset = VectorMath.v2(displayed.x + Layout.kBlockPad, displayed.y + Layout.kBlockPad);
				MenuHost.m_contentW = innerW;
				MenuHost.m_currentBlock = id;

				Widgets.resetOccurrences();

				Color savedColor = Gui.color;

				bool savedEnabled = Gui.enabled;

				Matrix4x4 savedMatrix = Gui.matrix;

				MenuAnimation.beginMeasure();

				MenuRowFlow.beginBlock(id);

				MenuHost.m_inBlock = true;

				float contentH = 0f;

				bool drewCleanly = false;
				try
				{
					contentH = MenuHost.fillBlock(id, innerW);

					drewCleanly = true;
				}
				catch (Exception ex)
				{
					UiLog.logException($"MenuHost.drawBlock({id})", ex);

					MenuRowFlow.abort();

					ContentClip.unwind();

					Gui.color = savedColor;
					Gui.enabled = savedEnabled;
					Gui.matrix = savedMatrix;
				}
				finally
				{
					MenuHost.m_inBlock = false;
				}

				contentH = MenuRowFlow.endBlock(contentH, drewCleanly);

				MenuHost.m_blockOffset = prevOffset;
				MenuHost.m_contentW = prevWidth;
				MenuHost.m_currentBlock = prevBlock;

				float settledAdjust = MenuAnimation.consumeAdjust();
				if (drewCleanly)
				{
					float newHeight = contentH + Layout.kBlockPad * 2f;
					MenuHost.kBlockMeasures[id] = new BlockMeasure
					{
						m_liveHeight = newHeight,
						m_settledHeight = newHeight + settledAdjust
					};

					colYs[col] += newHeight + Layout.kBlockGap;
				}
				else
				{
					if (!MenuHost.kBlockMeasures.ContainsKey(id))
					{
						MenuHost.kBlockMeasures[id] = new BlockMeasure
						{
							m_liveHeight = predictedH,
							m_settledHeight = predictedH
						};
					}

					colYs[col] += predictedH + Layout.kBlockGap;
				}
			}
		}

		private static float fillBlock(int id, float w)
		{
			float y = 0f;
			int index = id - MenuHost.kPageBase;
			if (index >= 0 && index < MenuHost.kPageDrawers.Count)
				MenuHost.kPageDrawers[index].Invoke(ref y, w);
			return y;
		}

	}
}
