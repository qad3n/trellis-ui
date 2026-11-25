using System.Collections.Generic;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace Trellis
{
	internal static class TextInputFocus
	{
		private const int kMissLimit = 2;
		private const int kNoControl = 0;

		private static string m_activeName;
		private static int m_seenFrame = -1;
		private static int m_missFrame = -1;
		private static int m_missedFrames;
		private static bool m_resyncTraced;
		private static Il2CppSystem.Type m_editorType;
		// two lists swap so the last full pass stays readable while the current one fills
		private static List<string> m_order = new List<string>();
		private static List<string> m_lastOrder = new List<string>();
		private static int m_orderPass = -1;

		internal static string activeName => TextInputFocus.m_activeName;
		private static Il2CppSystem.Type editorType => TextInputFocus.m_editorType ??= Il2CppType.Of<TextEditor>();

		internal static void focusOwned(Rect rect, string controlName)
		{
			if (!TextInputFocus.claim(rect, controlName))
				return;

			Event current = Event.current;
			if (!(current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape))
				return;

			UiLog.debug($"[TextInputFocus.focusOwned] escape released '{controlName}'");

			TextInputFocus.clear();

			current.Use();
		}

		internal static void endFrame()
		{
			int frame = Gui.frameCount;
			if (TextInputFocus.m_activeName == null || TextInputFocus.m_seenFrame == frame || TextInputFocus.m_missFrame == frame)
				return;

			TextInputFocus.m_missFrame = frame;

			TextInputFocus.m_missedFrames++;
			if (TextInputFocus.m_missedFrames < TextInputFocus.kMissLimit)
				return;

			UiLog.debug($"[TextInputFocus.endFrame] focused control '{TextInputFocus.m_activeName}' vanished, releasing focus");

			TextInputFocus.clear();
		}

		internal static void set(string controlName)
		{
			UiLog.trace($"[TextInputFocus.set] focus set to '{controlName}'");

			TextInputFocus.m_activeName = controlName;
			TextInputFocus.m_seenFrame = Gui.frameCount;
			TextInputFocus.m_missedFrames = 0;
			TextInputFocus.m_resyncTraced = false;

			MenuTextEditor.onFocusChanged();
		}

		// fields enlist in draw order so tab walks the last full pass and wraps
		internal static bool traverse(int direction)
		{
			List<string> order = TextInputFocus.m_lastOrder;

			int count = order.Count;
			if (count <= 1)
				return false;

			int index = order.IndexOf(TextInputFocus.m_activeName);
			if (index < 0)
				return false;

			int next = ((index + direction) % count + count) % count;
			TextInputFocus.set(order[next]);
			return true;
		}

		internal static void clearIf(string controlName)
		{
			if (TextInputFocus.m_activeName != controlName)
				return;

			TextInputFocus.clear();
		}

		internal static void clear()
		{
			TextInputFocus.m_activeName = null;
			TextInputFocus.m_seenFrame = -1;
			TextInputFocus.m_missedFrames = 0;
			TextInputFocus.m_resyncTraced = false;

			MenuTextEditor.onFocusChanged();

			GUIUtility.keyboardControl = TextInputFocus.kNoControl;
		}

		// unity DoTextField overwrites text but not cursorIndex so a shrunk value throws on the next edit
		internal static void syncEditor(string controlName, string text)
		{
			if (GUI.GetNameOfFocusedControl() != controlName)
				return;

			TextEditor editor = TextInputFocus.focusedEditor();
			if (editor == null)
				return;

			string safe = text ?? string.Empty;
			if (TextInputFocus.setText(editor, safe) && !TextInputFocus.m_resyncTraced)
			{
				TextInputFocus.m_resyncTraced = true;

				UiLog.debug($"[TextInputFocus.syncEditor] '{controlName}' editor resynced to text changed outside it");
			}

			TextInputFocus.clampIndices(editor, safe.Length);
		}

		internal static void clampFocused()
		{
			TextEditor editor = TextInputFocus.focusedEditor();
			if (editor == null)
				return;

			TextInputFocus.clampIndices(editor, (editor.text ?? string.Empty).Length);
		}

		internal static void moveToEnd(string text)
		{
			TextEditor editor = TextInputFocus.focusedEditor();
			if (editor == null)
				return;

			string current = text ?? string.Empty;
			TextInputFocus.setText(editor, current);

			editor.cursorIndex = current.Length;
			editor.selectIndex = current.Length;
		}

		// event based claims miss real clicks depending on draw order
		private static bool claim(Rect rect, string controlName)
		{
			if (TextInputFocus.m_activeName == controlName)
			{
				TextInputFocus.m_seenFrame = Gui.frameCount;
				TextInputFocus.m_missedFrames = 0;
			}

			if (Widgets.disabledScoped && TextInputFocus.m_activeName == controlName)
			{
				UiLog.trace($"[TextInputFocus.claim] '{controlName}' drawn disabled, releasing focus");

				TextInputFocus.clear();
				return false;
			}
			if (!Gui.enabled)
				return false;

			TextInputFocus.enlist(controlName);
			if (MenuHost.rawMouseDown0)
			{
				bool inside = RectMath.contains(rect, MenuHost.scrollMouse);
				if (inside && TextInputFocus.m_activeName != controlName)
					TextInputFocus.set(controlName);
				else if (!inside && TextInputFocus.m_activeName == controlName)
				{
					UiLog.trace($"[TextInputFocus.claim] click away from '{controlName}', releasing focus");

					TextInputFocus.clear();
					return false;
				}
			}
			return TextInputFocus.m_activeName == controlName;
		}

		private static void enlist(string controlName)
		{
			if (TextInputFocus.m_orderPass != Gui.passToken)
			{
				TextInputFocus.m_orderPass = Gui.passToken;

				(TextInputFocus.m_order, TextInputFocus.m_lastOrder) = (TextInputFocus.m_lastOrder, TextInputFocus.m_order);
				TextInputFocus.m_order.Clear();
			}
			TextInputFocus.m_order.Add(controlName);
		}

		private static bool setText(TextEditor editor, string text)
		{
			if (editor.text == text)
				return false;

			editor.text = text;
			if (editor.style != null)
				editor.UpdateTextHandle();
			return true;
		}

		// unity GetStateObject returns a plain wrapper so a managed cast to TextEditor always fails
		private static TextEditor focusedEditor()
		{
			int controlId = GUIUtility.keyboardControl;
			if (controlId == TextInputFocus.kNoControl)
				return null;

			return Il2CppCast.As<TextEditor>(GUIUtility.GetStateObject(TextInputFocus.editorType, controlId));
		}

		private static void clampIndices(TextEditor editor, int length)
		{
			int cursor = editor.cursorIndex;

			int clampedCursor = FastMath.clamp(cursor, 0, length);
			if (clampedCursor != cursor)
				editor.cursorIndex = clampedCursor;

			int select = editor.selectIndex;

			int clampedSelect = FastMath.clamp(select, 0, length);
			if (clampedSelect == select)
				return;

			editor.selectIndex = clampedSelect;
		}
	}
}
