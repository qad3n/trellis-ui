using System.Text;

namespace Trellis
{
	// widget identity, shared input state and the clipboard
	internal static partial class Ui
	{
		// mixes a caller's own key into a widget id so repeated rows stay apart
		internal static int keyMix => Widgets.kKeyMix;
		internal static bool capturingKeybind => Widgets.capturingKeybind;
		internal static bool overlayDropdownLayered => Widgets.overlayDropdownLayered;

		internal static string id(string prefix, int index) => Widgets.indexedId(prefix, index);
		internal static MultiSelectItemHint[] hints(string[] tooltips) => Widgets.tooltipHints(tooltips);
		internal static void closeOverlayDropdown() => Widgets.closeOverlayDropdown();

		// claims the layered pass for this frame so a popup closing on this click still draws on top
		internal static void holdDropdownLayer() => Widgets.m_dropdownFrame = Gui.frameCount;
		internal static void setClipboard(string text) => Widgets.setClipboard(text);
		internal static void copySelection(StringBuilder lines) => Widgets.copySelection(lines);
		internal static bool copyPressed(TextSelectionOwner owner) => Widgets.copyPressed(owner);
	}
}
