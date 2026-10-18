using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Trellis
{
	// unity TextEditor keys state to draw order control ids which is where the Substring throw came from
	internal static class MenuTextEditor
	{
		// prefix measurement is quadratic so pastes need a ceiling
		internal const int kMaxLength = 4096;
		private const float kSelectionAlpha = 0.35f;
		private const float kHintAlpha = 0.45f;
		private const char kMinPrintable = ' ';
		private const char kDelChar = '\x7f';
		private const char kNewLineChar = '\n';
		private const char kWordSeparator = ' ';
		private const string kNewLine = "\n";
		private const int kGrowthFactor = 2;

		private static readonly MenuTextLayout kAreaLayout = new MenuTextLayout();
		private static readonly Dictionary<GUIStyle, GUIStyle> kStyles = new Dictionary<GUIStyle, GUIStyle>();

		private static string m_stateName;
		private static int m_cursor;
		private static int m_select;
		private static float m_scrollX;
		private static float m_scrollY;
		private static bool m_dragSelecting;
		private static float m_editTime;
		private static int m_pendingCaret = -1;
		private static GUIStyle m_measuredStyle;
		private static string m_measuredText;
		private static float[] m_prefixWidths = new float[Limits.kDefaultMax + 1];
		private static int m_prefixCount = 1;
		private static float m_padLeft;
		private static float m_padRight;
		private static float m_spaceWidth;
		private static int m_stylesVersion = -1;
		private static string m_maskCache = string.Empty;
		private static char m_maskChar;

		private static class Caret
		{
			internal const float kW = 1f;
			internal const float kInsetY = 3f;
			internal const float kBlinkSeconds = 0.53f;
		}

		private static class Limits
		{
			internal const int kDefaultMax = 256;
			internal const int kAreaMax = MenuTextEditor.kMaxLength;
		}

		internal static int caretIndex => MenuTextEditor.m_cursor;
		internal static bool selected => MenuTextEditor.m_cursor != MenuTextEditor.m_select;
		internal static int selectionStart => FastMath.min(MenuTextEditor.m_cursor, MenuTextEditor.m_select);
		internal static int selectionEnd => FastMath.max(MenuTextEditor.m_cursor, MenuTextEditor.m_select);

		internal static string draw(Rect rect, string text, GUIStyle style, string controlName, bool password, char maskChar, int maxLength, bool multiline, string hint)
		{
			int cap = maxLength >= 0 ? FastMath.min(maxLength, MenuTextEditor.kMaxLength) : multiline ? Limits.kAreaMax : Limits.kDefaultMax;
			if (text.Length > cap)
				text = text[..cap];

			bool focused = TextInputFocus.activeName == controlName;
			if (MenuTextEditor.rightClicked(rect, text.Length))
			{
				text = string.Empty;

				if (focused)
					MenuTextEditor.clearState();
			}

			string display = password ? MenuTextEditor.maskedDisplay(text.Length, maskChar) : text;
			if (multiline)
				MenuTextEditor.kAreaLayout.build(display, RectMath.w(rect), style);

			if (focused)
			{
				MenuTextEditor.adoptState(controlName, text.Length);

				if (!multiline)
					MenuTextEditor.measureWidths(display, style);

				MenuTextEditor.readMouse(rect, multiline);

				string edited = MenuTextEditor.applyEvent(text, cap, password, multiline);
				if (!object.ReferenceEquals(edited, text))
				{
					text = edited;

					display = password ? MenuTextEditor.maskedDisplay(text.Length, maskChar) : text;
					MenuTextEditor.updateGeometry(display, rect, style, multiline);
				}
				MenuTextEditor.clampState(text.Length);
			}
			MenuTextEditor.render(rect, display, style, focused, multiline, hint);
			return text;
		}

		internal static void onFocusChanged()
		{
			MenuTextEditor.m_stateName = null;
			MenuTextEditor.m_dragSelecting = false;
			MenuTextEditor.m_pendingCaret = -1;
		}

		internal static void openAt(int caret) => MenuTextEditor.m_pendingCaret = FastMath.max(0, caret);

		internal static void setCaret(int index)
		{
			MenuTextEditor.m_cursor = FastMath.max(0, index);
			MenuTextEditor.m_select = MenuTextEditor.m_cursor;
			MenuTextEditor.m_editTime = Gui.unscaledTime;
		}

		internal static void moveToEnd(string text) => MenuTextEditor.setCaret((text ?? string.Empty).Length);

		internal static string insertAtCaret(string text, string insertion)
		{
			string safe = text ?? string.Empty;
			MenuTextEditor.clampState(safe.Length);
			return MenuTextEditor.insert(safe, insertion ?? string.Empty, MenuTextEditor.kMaxLength);
		}

		private static bool rightClicked(Rect rect, int length) => length > 0 && Gui.enabled && MenuHost.mouseDown1 && RectMath.contains(rect, MenuHost.scrollMouse);

		private static void clearState()
		{
			MenuTextEditor.m_cursor = 0;
			MenuTextEditor.m_select = 0;
			MenuTextEditor.resetView();
		}

		private static void adoptState(string controlName, int length)
		{
			if (MenuTextEditor.m_stateName == controlName)
				return;

			MenuTextEditor.m_stateName = controlName;
			MenuTextEditor.m_cursor = length;
			MenuTextEditor.m_select = 0;
			if (MenuTextEditor.m_pendingCaret >= 0)
			{
				MenuTextEditor.m_cursor = FastMath.min(MenuTextEditor.m_pendingCaret, length);
				MenuTextEditor.m_select = MenuTextEditor.m_cursor;
				MenuTextEditor.m_pendingCaret = -1;
			}

			MenuTextEditor.resetView();
		}

		private static void resetView()
		{
			MenuTextEditor.m_scrollX = 0f;
			MenuTextEditor.m_scrollY = 0f;
			MenuTextEditor.m_dragSelecting = false;
			MenuTextEditor.m_editTime = Gui.unscaledTime;
		}

		private static void selectAll(string text)
		{
			MenuTextEditor.m_select = 0;
			MenuTextEditor.m_cursor = text.Length;
		}

		private static void clampState(int length)
		{
			MenuTextEditor.m_cursor = FastMath.clamp(MenuTextEditor.m_cursor, 0, length);
			MenuTextEditor.m_select = FastMath.clamp(MenuTextEditor.m_select, 0, length);
		}

		private static void updateGeometry(string display, Rect rect, GUIStyle style, bool multiline)
		{
			if (multiline)
				MenuTextEditor.kAreaLayout.build(display, RectMath.w(rect), style);
			else
				MenuTextEditor.measureWidths(display, style);
		}

		private static void readMouse(Rect rect, bool multiline)
		{
			if (!Gui.enabled)
			{
				MenuTextEditor.m_dragSelecting = false;
				return;
			}

			Vector2 mouse = MenuHost.scrollMouse;
			if (MenuHost.rawMouseDown0 && RectMath.contains(rect, mouse))
			{
				MenuTextEditor.m_cursor = MenuTextEditor.mouseIndex(rect, mouse, multiline);
				MenuTextEditor.m_select = MenuTextEditor.m_cursor;
				MenuTextEditor.m_dragSelecting = true;
				MenuTextEditor.m_editTime = Gui.unscaledTime;
			}
			else if (MenuTextEditor.m_dragSelecting)
			{
				if (MenuHost.mouseHeld0)
					MenuTextEditor.m_cursor = MenuTextEditor.mouseIndex(rect, mouse, multiline);
				else
					MenuTextEditor.m_dragSelecting = false;
			}
		}

		private static int mouseIndex(Rect rect, Vector2 mouse, bool multiline)
		{
			if (multiline)
				return MenuTextEditor.kAreaLayout.indexAt(mouse.x - RectMath.x(rect) - MenuTextEditor.kAreaLayout.padLeft, mouse.y - RectMath.y(rect) - MenuTextEditor.kAreaLayout.padTop + MenuTextEditor.m_scrollY);

			return MenuTextEditor.indexAt(mouse.x - RectMath.x(rect) - MenuTextEditor.m_padLeft + MenuTextEditor.m_scrollX);
		}

		private static string applyEvent(string text, int cap, bool password, bool multiline)
		{
			if (!Gui.enabled)
				return text;

			Event current = Event.current;
			if (current.type == EventType.ExecuteCommand)
				return MenuTextEditor.runCommand(current, text, cap, password, multiline);
			if (current.type != EventType.KeyDown || MenuHost.togglesMenu(current.keyCode))
				return text;

			bool ctrl = current.control || current.command;

			bool shift = current.shift;

			char typed;
			switch (current.keyCode)
			{
				case KeyCode.LeftArrow:
					MenuTextEditor.moveCursor(text, -1, ctrl, shift);

					current.Use();
					return text;

				case KeyCode.RightArrow:
					MenuTextEditor.moveCursor(text, 1, ctrl, shift);

					current.Use();
					return text;

				case KeyCode.UpArrow:
					MenuTextEditor.moveVertical(text, -1, shift, multiline);

					current.Use();
					return text;

				case KeyCode.DownArrow:
					MenuTextEditor.moveVertical(text, 1, shift, multiline);

					current.Use();
					return text;

				case KeyCode.Home:
					MenuTextEditor.moveEdge(text, -1, ctrl, shift, multiline);

					current.Use();
					return text;

				case KeyCode.End:
					MenuTextEditor.moveEdge(text, 1, ctrl, shift, multiline);

					current.Use();
					return text;

				case KeyCode.Backspace:
					current.Use();
					return MenuTextEditor.deleteBackward(text, ctrl);

				case KeyCode.Delete:
					current.Use();
					return MenuTextEditor.deleteForward(text, ctrl);

				case KeyCode.A when ctrl:
					MenuTextEditor.selectAll(text);

					current.Use();
					return text;

				case KeyCode.C when ctrl:
					MenuTextEditor.copy(text, password);

					current.Use();
					return text;

				case KeyCode.X when ctrl:
					current.Use();
					return MenuTextEditor.cut(text, password);

				case KeyCode.V when ctrl:
					current.Use();
					return MenuTextEditor.paste(text, cap, multiline);

				case KeyCode.Return:
				case KeyCode.KeypadEnter:
					if (!multiline)
						return text;

					current.Use();
					return MenuTextEditor.insert(text, MenuTextEditor.kNewLine, cap);

				case KeyCode.Escape:
				case KeyCode.Tab:
					return text;

				default:
					if (ctrl)
						return text;

					typed = current.character;
					if (!MenuTextEditor.printable(typed))
						return text;

					current.Use();
					return MenuTextEditor.insert(text, typed.ToString(), cap);
			}
		}

		private static string runCommand(Event current, string text, int cap, bool password, bool multiline)
		{
			switch (current.commandName)
			{
				case "SelectAll":
					MenuTextEditor.selectAll(text);

					current.Use();
					return text;

				case "Copy":
					MenuTextEditor.copy(text, password);

					current.Use();
					return text;

				case "Cut":
					current.Use();
					return MenuTextEditor.cut(text, password);

				case "Paste":
					current.Use();
					return MenuTextEditor.paste(text, cap, multiline);

				default:
					return text;
			}
		}

		private static void moveCursor(string text, int direction, bool ctrl, bool shift)
		{
			if (!shift && MenuTextEditor.m_cursor != MenuTextEditor.m_select)
			{
				MenuTextEditor.m_cursor = direction < 0 ? FastMath.min(MenuTextEditor.m_cursor, MenuTextEditor.m_select) : FastMath.max(MenuTextEditor.m_cursor, MenuTextEditor.m_select);
				MenuTextEditor.m_select = MenuTextEditor.m_cursor;
				return;
			}

			int next;
			if (ctrl)
				next = direction < 0 ? MenuTextEditor.wordLeft(text, MenuTextEditor.m_cursor) : MenuTextEditor.wordRight(text, MenuTextEditor.m_cursor);
			else
				next = MenuTextEditor.m_cursor + direction;
			MenuTextEditor.m_cursor = FastMath.clamp(next, 0, text.Length);

			if (shift)
				return;

			MenuTextEditor.m_select = MenuTextEditor.m_cursor;
		}

		private static void moveVertical(string text, int direction, bool shift, bool multiline)
		{
			int target = direction < 0 ? 0 : text.Length;
			if (multiline)
			{
				int line = MenuTextEditor.kAreaLayout.lineOf(MenuTextEditor.m_cursor) + direction;
				if (line >= 0 && line < MenuTextEditor.kAreaLayout.lineCount)
					target = MenuTextEditor.kAreaLayout.indexInLine(line, MenuTextEditor.kAreaLayout.caretX(MenuTextEditor.m_cursor));
			}
			MenuTextEditor.m_cursor = target;

			if (shift)
				return;

			MenuTextEditor.m_select = MenuTextEditor.m_cursor;
		}

		private static void moveEdge(string text, int direction, bool ctrl, bool shift, bool multiline)
		{
			int target = direction < 0 ? 0 : text.Length;
			if (multiline && !ctrl)
			{
				int line = MenuTextEditor.kAreaLayout.lineOf(MenuTextEditor.m_cursor);

				target = direction < 0 ? MenuTextEditor.kAreaLayout.lineStart(line) : MenuTextEditor.kAreaLayout.visibleEnd(line);
			}
			MenuTextEditor.m_cursor = target;

			if (shift)
				return;

			MenuTextEditor.m_select = MenuTextEditor.m_cursor;
		}

		private static string deleteBackward(string text, bool ctrl)
		{
			if (MenuTextEditor.m_cursor != MenuTextEditor.m_select)
				return MenuTextEditor.deleteSelection(text);
			if (MenuTextEditor.m_cursor == 0)
				return text;

			int start = ctrl ? MenuTextEditor.wordLeft(text, MenuTextEditor.m_cursor) : MenuTextEditor.m_cursor - 1;

			string result = text.Remove(start, MenuTextEditor.m_cursor - start);

			MenuTextEditor.m_cursor = start;
			MenuTextEditor.m_select = start;
			MenuTextEditor.m_editTime = Gui.unscaledTime;
			return result;
		}

		private static string deleteForward(string text, bool ctrl)
		{
			if (MenuTextEditor.m_cursor != MenuTextEditor.m_select)
				return MenuTextEditor.deleteSelection(text);
			if (MenuTextEditor.m_cursor >= text.Length)
				return text;

			int end = ctrl ? MenuTextEditor.wordRight(text, MenuTextEditor.m_cursor) : MenuTextEditor.m_cursor + 1;

			string result = text.Remove(MenuTextEditor.m_cursor, end - MenuTextEditor.m_cursor);

			MenuTextEditor.m_select = MenuTextEditor.m_cursor;
			MenuTextEditor.m_editTime = Gui.unscaledTime;
			return result;
		}

		private static string deleteSelection(string text)
		{
			int min = FastMath.min(MenuTextEditor.m_cursor, MenuTextEditor.m_select);

			int max = FastMath.max(MenuTextEditor.m_cursor, MenuTextEditor.m_select);

			string result = text.Remove(min, max - min);

			MenuTextEditor.m_cursor = min;
			MenuTextEditor.m_select = min;
			MenuTextEditor.m_editTime = Gui.unscaledTime;
			return result;
		}

		private static string insert(string text, string insertion, int cap)
		{
			if (MenuTextEditor.m_cursor != MenuTextEditor.m_select)
				text = MenuTextEditor.deleteSelection(text);

			int room = cap - text.Length;
			if (room <= 0)
				return text;

			if (insertion.Length > room)
				insertion = insertion[..room];

			string result = text.Insert(MenuTextEditor.m_cursor, insertion);

			MenuTextEditor.m_cursor += insertion.Length;
			MenuTextEditor.m_select = MenuTextEditor.m_cursor;
			MenuTextEditor.m_editTime = Gui.unscaledTime;
			return result;
		}

		private static void copy(string text, bool password)
		{
			if (password || MenuTextEditor.m_cursor == MenuTextEditor.m_select)
				return;

			int min = FastMath.min(MenuTextEditor.m_cursor, MenuTextEditor.m_select);

			int max = FastMath.max(MenuTextEditor.m_cursor, MenuTextEditor.m_select);
			GUIUtility.systemCopyBuffer = text[min..max];
		}

		private static string cut(string text, bool password)
		{
			if (password || MenuTextEditor.m_cursor == MenuTextEditor.m_select)
				return text;

			MenuTextEditor.copy(text, password);
			return MenuTextEditor.deleteSelection(text);
		}

		private static string paste(string text, int cap, bool multiline)
		{
			string clipboard = GUIUtility.systemCopyBuffer ?? string.Empty;
			if (clipboard.Length == 0)
				return text;

			StringBuilder filtered = new StringBuilder(clipboard.Length);
			foreach (char c in clipboard)
			{
				if (!(MenuTextEditor.printable(c) || multiline && c == MenuTextEditor.kNewLineChar))
					continue;

				filtered.Append(c);
			}

			if (filtered.Length == 0)
				return text;

			return MenuTextEditor.insert(text, filtered.ToString(), cap);
		}

		private static bool printable(char c) => c >= MenuTextEditor.kMinPrintable && c != MenuTextEditor.kDelChar;

		private static int wordLeft(string text, int index)
		{
			int i = index;
			while (i > 0 && text[i - 1] == MenuTextEditor.kWordSeparator)
				i--;

			while (i > 0 && text[i - 1] != MenuTextEditor.kWordSeparator)
				i--;
			return i;
		}

		private static int wordRight(string text, int index)
		{
			int i = index;
			while (i < text.Length && text[i] != MenuTextEditor.kWordSeparator)
				i++;

			while (i < text.Length && text[i] == MenuTextEditor.kWordSeparator)
				i++;
			return i;
		}

		private static void render(Rect rect, string display, GUIStyle style, bool focused, bool multiline, string hint)
		{
			if (!Gui.repainting)
				return;

			bool hovered = Gui.enabled && RectMath.contains(rect, MenuHost.scrollMouse);
			Theme.drawField(rect, hovered, focused);

			if (multiline)
			{
				MenuTextEditor.renderLines(rect, style, focused);
				return;
			}

			if (!focused)
			{
				if (display.Length == 0 && hint != null)
					MenuTextEditor.renderHint(rect, hint, style);
				else
					Gui.label(rect, display, MenuTextEditor.plainStyle(style));
				return;
			}

			MenuTextEditor.updateScroll(rect);

			Gui.beginGroup(rect);

			try
			{
				float baseX = MenuTextEditor.m_padLeft - MenuTextEditor.m_scrollX;
				if (MenuTextEditor.m_cursor != MenuTextEditor.m_select)
				{
					float startX = MenuTextEditor.offset(FastMath.min(MenuTextEditor.m_cursor, MenuTextEditor.m_select));

					float endX = MenuTextEditor.offset(FastMath.max(MenuTextEditor.m_cursor, MenuTextEditor.m_select));
					MenuTextEditor.drawAccent(RectMath.create(baseX + startX, Caret.kInsetY, endX - startX, RectMath.h(rect) - Caret.kInsetY * 2f), MenuTextEditor.kSelectionAlpha);
				}

				float textW = FastMath.max(RectMath.w(rect), MenuTextEditor.m_padLeft + MenuTextEditor.offset(MenuTextEditor.m_prefixCount - 1) + MenuTextEditor.m_padRight);
				Gui.label(RectMath.create(-MenuTextEditor.m_scrollX, 0f, MenuTextEditor.m_scrollX + textW, RectMath.h(rect)), display, MenuTextEditor.plainStyle(style));

				if (MenuTextEditor.caretVisible())
					MenuTextEditor.drawCaret(RectMath.create(baseX + MenuTextEditor.offset(MenuTextEditor.m_cursor), Caret.kInsetY, Caret.kW, RectMath.h(rect) - Caret.kInsetY * 2f));
			}
			finally
			{
				// unity BeginGroup writes GUI.color behind the shadow
				Gui.endGroup();

				Gui.reapplyColor();
			}
		}

		private static void renderLines(Rect rect, GUIStyle style, bool focused)
		{
			MenuTextLayout layout = MenuTextEditor.kAreaLayout;

			float lineH = layout.lineHeight;

			float visibleH = FastMath.max(0f, RectMath.h(rect) - layout.padTop - layout.padBottom);
			MenuTextEditor.updateScrollY(layout, visibleH, focused);

			int lastLine = layout.lineCount - 1;

			int first = FastMath.clamp(FastMath.floorToInt(MenuTextEditor.m_scrollY / lineH), 0, lastLine);

			int last = FastMath.clamp(FastMath.ceilToInt((MenuTextEditor.m_scrollY + visibleH) / lineH), first, lastLine);

			float labelH = layout.padTop + lineH + layout.padBottom;

			float rowY = layout.padTop - MenuTextEditor.m_scrollY;

			GUIStyle lineStyle = MenuTextEditor.plainStyle(style);

			Gui.beginGroup(rect);

			try
			{
				if (focused && MenuTextEditor.m_cursor != MenuTextEditor.m_select)
					MenuTextEditor.drawSelection(layout, first, last, rowY);

				for (int line = first; line <= last; line++)
				{
					float labelW = FastMath.max(RectMath.w(rect), layout.padLeft + layout.lineWidth(line) + layout.padRight);
					Gui.label(RectMath.create(0f, rowY + line * lineH - layout.padTop, labelW, labelH), layout.lineText(line), lineStyle);
				}

				if (focused && MenuTextEditor.caretVisible())
				{
					int line = layout.lineOf(MenuTextEditor.m_cursor);
					MenuTextEditor.drawAccent(RectMath.create(layout.padLeft + layout.caretX(MenuTextEditor.m_cursor), rowY + line * lineH, Caret.kW, lineH), 1f);
				}
			}
			finally
			{
				Gui.endGroup();

				Gui.reapplyColor();
			}
		}

		private static void drawSelection(MenuTextLayout layout, int first, int last, float rowY)
		{
			int min = FastMath.min(MenuTextEditor.m_cursor, MenuTextEditor.m_select);

			int max = FastMath.max(MenuTextEditor.m_cursor, MenuTextEditor.m_select);

			int minLine = layout.lineOf(min);

			int maxLine = layout.lineOf(max);

			float lineH = layout.lineHeight;
			for (int line = FastMath.max(first, minLine); line <= FastMath.min(last, maxLine); line++)
			{
				float startX = line == minLine ? layout.caretX(min) : 0f;

				float endX = line == maxLine ? layout.caretX(max) : layout.lineWidth(line);
				if (endX <= startX)
					continue;

				MenuTextEditor.drawAccent(RectMath.create(layout.padLeft + startX, rowY + line * lineH, endX - startX, lineH), MenuTextEditor.kSelectionAlpha);
			}
		}

		private static void renderHint(Rect rect, string hint, GUIStyle style)
		{
			Color saved = Gui.color;
			Gui.color = ColorMath.fade(saved, MenuTextEditor.kHintAlpha);
			Gui.label(rect, hint, MenuTextEditor.plainStyle(style));
			Gui.color = saved;
		}

		private static void drawAccent(Rect rect, float alpha)
		{
			Color saved = Gui.color;
			Gui.color = ColorMath.fade(saved, alpha);
			Gui.drawTextureSnapped(rect, Theme.accentBar);
			Gui.color = saved;
		}

		private static void drawCaret(Rect rect) => Gui.drawStroke(rect, Theme.accentBar);

		private static bool caretVisible()
		{
			float phase = (Gui.unscaledTime - MenuTextEditor.m_editTime) / Caret.kBlinkSeconds;
			return ((int)phase & 1) == 0;
		}

		private static void updateScroll(Rect rect)
		{
			float visibleW = FastMath.max(0f, RectMath.w(rect) - MenuTextEditor.m_padLeft - MenuTextEditor.m_padRight);

			float cursorX = MenuTextEditor.offset(MenuTextEditor.m_cursor);

			float maxScroll = FastMath.max(0f, MenuTextEditor.offset(MenuTextEditor.m_prefixCount - 1) - visibleW);
			MenuTextEditor.m_scrollX = FastMath.clamp(MenuTextEditor.m_scrollX, 0f, maxScroll);
			if (cursorX - MenuTextEditor.m_scrollX > visibleW)
				MenuTextEditor.m_scrollX = cursorX - visibleW;
			if (!(cursorX < MenuTextEditor.m_scrollX))
				return;

			MenuTextEditor.m_scrollX = cursorX;
		}

		private static void updateScrollY(MenuTextLayout layout, float visibleH, bool focused)
		{
			float lineH = layout.lineHeight;

			float maxScroll = FastMath.max(0f, layout.lineCount * lineH - visibleH);
			MenuTextEditor.m_scrollY = FastMath.clamp(MenuTextEditor.m_scrollY, 0f, maxScroll);

			if (!focused)
				return;

			float caretTop = layout.lineOf(MenuTextEditor.m_cursor) * lineH;
			if (caretTop + lineH - MenuTextEditor.m_scrollY > visibleH)
				MenuTextEditor.m_scrollY = caretTop + lineH - visibleH;
			if (!(caretTop < MenuTextEditor.m_scrollY))
				return;

			MenuTextEditor.m_scrollY = caretTop;
		}

		private static float offset(int index)
		{
			if (index <= 0 || MenuTextEditor.m_prefixCount == 0)
				return 0f;

			return MenuTextEditor.m_prefixWidths[FastMath.min(index, MenuTextEditor.m_prefixCount - 1)] - MenuTextEditor.m_prefixWidths[0];
		}

		private static int indexAt(float x)
		{
			for (int i = 0; i < MenuTextEditor.m_prefixCount - 1; i++)
			{
				float mid = (MenuTextEditor.offset(i) + MenuTextEditor.offset(i + 1)) * 0.5f;
				if (x < mid)
					return i;
			}
			return MenuTextEditor.m_prefixCount - 1;
		}

		private static void measureWidths(string display, GUIStyle style)
		{
			if (!object.ReferenceEquals(style, MenuTextEditor.m_measuredStyle))
			{
				RectOffset pad = style.padding;
				MenuTextEditor.m_padLeft = pad.left;
				MenuTextEditor.m_padRight = pad.right;
				MenuTextEditor.m_spaceWidth = MenuTextLayout.spaceWidth(style);
				MenuTextEditor.m_measuredStyle = style;
				MenuTextEditor.m_measuredText = null;
			}
			if (display == MenuTextEditor.m_measuredText)
				return;

			int count = display.Length + 1;
			if (MenuTextEditor.m_prefixWidths.Length < count)
				Array.Resize(ref MenuTextEditor.m_prefixWidths, FastMath.max(count, MenuTextEditor.m_prefixWidths.Length * MenuTextEditor.kGrowthFactor));

			int start;
			if (MenuTextEditor.m_measuredText != null)
				start = MenuTextLayout.prefixLength(MenuTextEditor.m_measuredText, display) + 1;
			else
			{
				Theme.kScratchContent.text = string.Empty;
				MenuTextEditor.m_prefixWidths[0] = style.CalcSize(Theme.kScratchContent).x;

				start = 1;
			}

			for (int i = start; i < count; i++)
				MenuTextEditor.m_prefixWidths[i] = MenuTextLayout.measure(display, 0, i, style, MenuTextEditor.m_spaceWidth);

			MenuTextEditor.m_prefixCount = count;
			MenuTextEditor.m_measuredText = display;
		}

		// a second break would put glyphs past any caret
		private static GUIStyle plainStyle(GUIStyle source)
		{
			if (MenuTextEditor.m_stylesVersion != Theme.layoutVersion)
			{
				MenuTextEditor.kStyles.Clear();

				MenuTextEditor.m_stylesVersion = Theme.layoutVersion;
			}

			if (MenuTextEditor.kStyles.TryGetValue(source, out GUIStyle cached))
				return cached;

			GUIStyle textStyle = Theme.derive(source);
			textStyle.normal.background = null;
			textStyle.hover.background = null;
			textStyle.active.background = null;
			textStyle.focused.background = null;
			textStyle.wordWrap = false;
			MenuTextEditor.kStyles[source] = textStyle;
			return textStyle;
		}

		private static string maskedDisplay(int length, char maskChar)
		{
			if (MenuTextEditor.m_maskCache.Length != length || MenuTextEditor.m_maskChar != maskChar)
			{
				MenuTextEditor.m_maskCache = new string(maskChar, length);
				MenuTextEditor.m_maskChar = maskChar;
			}
			return MenuTextEditor.m_maskCache;
		}
	}
}
