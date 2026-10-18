using System;
using UnityEngine;

namespace Trellis
{
	internal sealed class MenuTextLayout
	{
		private const int kLineCapacity = 8;
		private const int kCharCapacity = 256;
		private const int kGrowthFactor = 2;
		private const float kMinHeight = 1f;
		private const char kSpace = ' ';
		private const char kNewLine = '\n';
		private const string kOneLine = "a";
		private const string kTwoLines = "a\na";
		private const string kSpacedPair = "a a";
		private const string kPair = "aa";

		private string m_layoutText;
		private float m_areaWidth = -1f;
		private float m_wrapWidth;
		private GUIStyle m_measuredStyle;
		private float m_emptyWidth;
		private float m_spaceWidth;
		private int m_lineCount;
		private Line[] m_lines = new Line[MenuTextLayout.kLineCapacity];
		private float[] m_charX = new float[MenuTextLayout.kCharCapacity];

		private struct Line
		{
			internal int m_start;
			internal int m_end;
			internal float m_width;
			internal string m_text;
		}

		internal int lineCount => m_lineCount;

		internal float lineHeight { get; private set; } = MenuTextLayout.kMinHeight;
		internal float padLeft { get; private set; }
		internal float padTop { get; private set; }
		internal float padRight { get; private set; }
		internal float padBottom { get; private set; }

		internal int lineStart(int line) => m_lines[line].m_start;
		internal float lineWidth(int line) => m_lines[line].m_width;
		internal string lineText(int line) => m_lines[line].m_text;
		internal float caretX(int index) => m_charX[index];

		internal void build(string text, float width, GUIStyle style)
		{
			bool styleChanged = !object.ReferenceEquals(style, m_measuredStyle);
			if (styleChanged)
				measureStyle(style);

			bool widthChanged = width != m_areaWidth;
			if (!styleChanged && !widthChanged && text == m_layoutText)
				return;

			int firstLine = 0;
			if (!styleChanged && !widthChanged && m_layoutText != null)
				firstLine = FastMath.max(0, lineOf(MenuTextLayout.prefixLength(m_layoutText, text)) - 1);

			m_areaWidth = width;
			m_wrapWidth = FastMath.max(0f, width - padLeft - padRight);
			growChars(text.Length + 1);

			int start = firstLine < m_lineCount ? m_lines[firstLine].m_start : 0;
			relayout(text, style, firstLine, start);
			m_layoutText = text;
		}

		internal int lineOf(int index)
		{
			int line = m_lineCount - 1;
			while (line > 0 && m_lines[line].m_start > index)
				line--;
			return FastMath.max(line, 0);
		}

		internal int indexAt(float x, float y)
		{
			if (m_lineCount == 0)
				return 0;

			int line = FastMath.clamp(FastMath.floorToInt(y / lineHeight), 0, m_lineCount - 1);
			return indexInLine(line, x);
		}

		internal int indexInLine(int line, float x)
		{
			int start = m_lines[line].m_start;

			int end = visibleEnd(line);
			for (int index = start; index < end; index++)
			{
				float mid = (m_charX[index] + xAfter(line, index)) * 0.5f;
				if (x < mid)
					return index;
			}
			return end;
		}

		internal int visibleEnd(int line)
		{
			int end = m_lines[line].m_end;

			bool softWrapped = line + 1 < m_lineCount && m_lines[line + 1].m_start == end;
			if (softWrapped && end > m_lines[line].m_start && m_layoutText[end - 1] == MenuTextLayout.kSpace)
				return end - 1;

			return end;
		}

		// calcsize drops trailing spaces so they are added back by count
		internal static float measure(string text, int start, int count, GUIStyle style, float spaceWidth)
		{
			int trimmed = count;
			while (trimmed > 0 && text[start + trimmed - 1] == MenuTextLayout.kSpace)
				trimmed--;

			Theme.kScratchContent.text = text.Substring(start, trimmed);
			return style.CalcSize(Theme.kScratchContent).x + (count - trimmed) * spaceWidth;
		}

		internal static float spaceWidth(GUIStyle style)
		{
			Theme.kScratchContent.text = MenuTextLayout.kSpacedPair;
			float spaced = style.CalcSize(Theme.kScratchContent).x;

			Theme.kScratchContent.text = MenuTextLayout.kPair;
			return spaced - style.CalcSize(Theme.kScratchContent).x;
		}

		internal static int prefixLength(string first, string second)
		{
			int limit = FastMath.min(first.Length, second.Length);

			int index = 0;
			while (index < limit && first[index] == second[index])
				index++;
			return index;
		}

		private float xAfter(int line, int index) => index + 1 == m_lines[line].m_end ? m_lines[line].m_width : m_charX[index + 1];

		private void relayout(string text, GUIStyle style, int line, int start)
		{
			int next = start;

			bool hardBreak;
			do
			{
				growLines(line + 1);

				next = layoutLine(text, style, line, next, out hardBreak);

				line++;
			}
			while (next < text.Length || hardBreak);
			m_lineCount = line;
		}

		private int layoutLine(string text, GUIStyle style, int line, int start, out bool hardBreak)
		{
			hardBreak = false;

			int length = text.Length;

			int end = length;

			int next = length;

			m_charX[start] = 0f;

			int lastSpace = -1;
			for (int index = start; index < length; index++)
			{
				char c = text[index];
				if (c == MenuTextLayout.kNewLine)
				{
					end = index;

					next = index + 1;

					hardBreak = true;
					break;
				}

				float x = measureWidth(text, style, start, index + 1 - start);
				m_charX[index + 1] = x;

				if (c == MenuTextLayout.kSpace)
				{
					lastSpace = index;
					continue;
				}

				if (x <= m_wrapWidth || index == start)
					continue;

				end = lastSpace >= start ? lastSpace + 1 : index;

				next = end;
				break;
			}

			m_lines[line].m_start = start;
			m_lines[line].m_end = end;
			m_lines[line].m_width = m_charX[end];
			m_lines[line].m_text = text[start..end];
			return next;
		}

		private float measureWidth(string text, GUIStyle style, int start, int count) => MenuTextLayout.measure(text, start, count, style, m_spaceWidth) - m_emptyWidth;

		private void measureStyle(GUIStyle style)
		{
			RectOffset pad = style.padding;
			padLeft = pad.left;
			padTop = pad.top;
			padRight = pad.right;
			padBottom = pad.bottom;

			Theme.kScratchContent.text = string.Empty;
			m_emptyWidth = style.CalcSize(Theme.kScratchContent).x;
			m_spaceWidth = MenuTextLayout.spaceWidth(style);

			Theme.kScratchContent.text = MenuTextLayout.kOneLine;

			float oneLine = style.CalcSize(Theme.kScratchContent).y;

			Theme.kScratchContent.text = MenuTextLayout.kTwoLines;

			float twoLines = style.CalcSize(Theme.kScratchContent).y;
			lineHeight = FastMath.max(twoLines - oneLine, MenuTextLayout.kMinHeight);
			m_measuredStyle = style;
		}

		private void growChars(int count)
		{
			if (m_charX.Length >= count)
				return;

			Array.Resize(ref m_charX, FastMath.max(count, m_charX.Length * MenuTextLayout.kGrowthFactor));
		}

		private void growLines(int count)
		{
			if (m_lines.Length >= count)
				return;

			Array.Resize(ref m_lines, FastMath.max(count, m_lines.Length * MenuTextLayout.kGrowthFactor));
		}
	}
}
