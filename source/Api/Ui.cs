using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Trellis
{
	// a block is a plain method and each widget moves the cursor down on its own
	internal static partial class Ui
	{
		private static readonly Dictionary<Action, int> kPageKeys = new Dictionary<Action, int>();
		private static readonly Dictionary<int, string> kLineKeys = new Dictionary<int, string>();
		private static readonly List<Cursor> kCursors = new List<Cursor>();

		private static float m_y;
		private static float m_w;

		internal readonly struct Reveal : IDisposable
		{
			private readonly MenuAnimation.SectionScope m_scope;
			private readonly bool m_disclosure;

			internal Reveal(MenuAnimation.SectionScope scope, bool disclosure = false)
			{
				m_scope = scope;
				m_disclosure = disclosure;
			}

			public static implicit operator bool(Reveal reveal) => reveal.m_scope.m_render;

			public void Dispose()
			{
				if (m_disclosure)
					Widgets.endDisclosure(ref Ui.m_y, ref Ui.m_w, m_scope);
				else
					MenuAnimation.endSection(ref Ui.m_y, m_scope);
			}
		}

		internal readonly struct Disabled : IDisposable
		{
			public void Dispose() => Widgets.endDisabled();
		}

		internal readonly struct Row : IDisposable
		{
			private readonly MenuRowFlow.RowScope m_scope;

			internal Row(MenuRowFlow.RowScope scope, Rect rect)
			{
				m_scope = scope;
				this.rect = rect;
			}

			internal Rect rect { get; }

			public void Dispose()
			{
				MenuRowFlow.RowScope scope = m_scope;
				Widgets.endRow(ref Ui.m_y, ref scope);
			}
		}

		// brackets a run of custom drawing so the row still animates in and out
		internal readonly struct Group : IDisposable
		{
			private readonly MenuRowFlow.RowScope m_scope;

			internal Group(MenuRowFlow.RowScope scope) => m_scope = scope;

			public void Dispose()
			{
				MenuRowFlow.RowScope scope = m_scope;
				Widgets.endRow(ref Ui.m_y, ref scope);
			}
		}

		// narrows the row width while something else sits beside the rows
		internal readonly struct Inset : IDisposable
		{
			private readonly float m_width;

			internal Inset(float width) => m_width = width;

			public void Dispose() => Ui.m_w = m_width;
		}

		private readonly struct Cursor
		{
			internal readonly float m_y;
			internal readonly float m_w;
			internal readonly Vector2 m_offset;

			internal Cursor(float y, float w, Vector2 offset)
			{
				m_y = y;
				m_w = w;
				m_offset = offset;
			}
		}

		// the cursor for widgets that have no short form here
		internal static ref float y => ref Ui.m_y;
		internal static float width => Ui.m_w;

		internal static Page page(params Action[] blocks) => new Page(Ui.blockKeys(blocks));

		// each column keeps its blocks and extra columns fold onto the shortest one
		internal static Page columns(params Action[][] columns)
		{
			int[][] keys = new int[columns.Length][];
			for (int c = 0; c < columns.Length; c++)
				keys[c] = Ui.blockKeys(columns[c]);
			return new Page(keys);
		}

		// draws a stack of rows outside a block, at the caller's own cursor
		internal static void push(float y, float w) => Ui.push(y, w, MenuHost.m_blockOffset);

		// an absolute origin, for an overlay that owns its own screen position
		internal static void push(float x, float y, float w) => Ui.push(y, w, VectorMath.v2(x, 0f));

		internal static float pop()
		{
			float advanced = Ui.m_y;

			int last = Ui.kCursors.Count - 1;

			Cursor saved = Ui.kCursors[last];
			Ui.kCursors.RemoveAt(last);

			Ui.m_y = saved.m_y;
			Ui.m_w = saved.m_w;
			MenuHost.m_blockOffset = saved.m_offset;
			return advanced;
		}

		// pass a key when the same call runs more than once in a block
		internal static Reveal reveal(bool visible, string key = null, [CallerLineNumber] int line = 0) => new Reveal(MenuAnimation.beginSection(ref Ui.m_y, key ?? Ui.lineKey(line), visible));

		// a header row that folds its indented body open and shut and the trailing text shows state while folded
		internal static Reveal disclosure(string title, ref bool expanded, string animId, string bodyKey, bool locked = false, string trailing = null) => new Reveal(Widgets.beginDisclosure(ref Ui.m_y, ref Ui.m_w, animId, title, ref expanded, bodyKey, locked, trailing), true);

		internal static Disabled disabled(bool disabled)
		{
			Widgets.beginDisabled(disabled);
			return default;
		}

		internal static Inset inset(float reserve)
		{
			Inset scope = new Inset(Ui.m_w);

			Ui.m_w -= reserve;
			return scope;
		}

		internal static Group group() => new Group(Widgets.beginRow(ref Ui.m_y, Widgets.RowPrefix.kGroup, null));
		internal static Group group(int key) => new Group(Widgets.beginRow(ref Ui.m_y, key));
		internal static Group group(string prefix, string label) => new Group(Widgets.beginRow(ref Ui.m_y, prefix, label));

		internal static Row row(float height)
		{
			MenuRowFlow.RowScope scope = Widgets.beginRow(ref Ui.m_y, Widgets.RowPrefix.kCustom, null);
			Rect rect = RectMath.create(MenuHost.m_blockOffset.x, MenuHost.m_blockOffset.y + Ui.m_y, Ui.m_w, height);
			Ui.m_y += height + Widgets.kFieldGap;
			return new Row(scope, rect);
		}

		private static void push(float y, float w, Vector2 offset)
		{
			Ui.kCursors.Add(new Cursor(Ui.m_y, Ui.m_w, MenuHost.m_blockOffset));

			Ui.m_y = y;
			Ui.m_w = w;
			MenuHost.m_blockOffset = offset;
		}

		private static int[] blockKeys(Action[] blocks)
		{
			int[] keys = new int[blocks.Length];
			for (int i = 0; i < blocks.Length; i++)
				keys[i] = Ui.blockKey(blocks[i]);
			return keys;
		}

		private static int blockKey(Action draw)
		{
			if (Ui.kPageKeys.TryGetValue(draw, out int key))
				return key;

			key = MenuHost.addPageBlock((ref float y, float w) => Ui.run(draw, ref y, w));
			Ui.kPageKeys[draw] = key;
			return key;
		}

		// blocks can nest so the outer cursor is put back afterwards
		private static void run(Action draw, ref float y, float w)
		{
			float savedY = Ui.m_y;
			float savedW = Ui.m_w;

			Ui.m_y = y;
			Ui.m_w = w;
			try
			{
				draw();
			}
			finally
			{
				y = Ui.m_y;
				Ui.m_y = savedY;
				Ui.m_w = savedW;
			}
		}

		private static bool set<T>(ref T value, T next) where T : IEquatable<T>
		{
			if (value.Equals(next))
				return false;

			value = next;
			return true;
		}

		private static bool set(ref string value, string next)
		{
			if (string.Equals(value, next))
				return false;

			value = next;
			return true;
		}

		private static bool set(ref KeyChord value, KeyChord next)
		{
			if (value.Equals(next))
				return false;

			value = next;
			return true;
		}

		private static string lineKey(int line)
		{
			if (Ui.kLineKeys.TryGetValue(line, out string key))
				return key;

			key = $"ui:{line}";
			Ui.kLineKeys[line] = key;
			return key;
		}
	}
}
