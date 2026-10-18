using System;
using UnityEngine;

namespace Trellis
{
	// borrows the menu input state so every widget works unchanged
	internal static class WindowHost
	{
		private static readonly Rect kScreenView = RectMath.create(0f, 0f, 0f, 0f);

		internal static class Layout
		{
			internal const float kTitleBarH = 32f;
			internal const float kTitleAccentH = 2f;
			internal const float kPad = 18f;
			internal const float kScreenMargin = 24f;
			internal const float kButtonH = 34f;
			internal const float kButtonGap = 10f;
			internal const float kSectionGap = 18f;
		}

		internal static bool interactive { get; private set; }

		internal static void onGUI<TWindow>(string animId, bool open, TWindow window, string location, Action draw) where TWindow : unmanaged, Enum
		{
			MenuAnimation.consumeClosed(animId);

			if (!open && !MenuAnimation.shouldRender(animId))
				return;

			MenuAnimation.beginOverlay();

			Matrix4x4 prevMatrix = Gui.beginScaled(out float scale);

			Vector2 mouse = MenuHost.designMouse(scale);

			bool live = open && !MenuAnimation.isClosingOrClosed(animId) && !MenuHost.coversCursor();

			Color prevColor = Gui.color;

			bool prevEnabled = Gui.enabled;

			Vector2 savedScroll = MenuHost.scrollMouse;

			Vector2 savedMouse = MenuHost.mousePosition;

			int prevBlock = MenuHost.m_currentBlock;

			MenuHost.m_currentBlock = MenuHost.key(window);
			MenuHost.scrollMouse = live ? mouse : MenuHost.kOffscreenMouse;
			MenuHost.mousePosition = MenuHost.scrollMouse;

			Widgets.m_overlayPass = true;
			WindowHost.interactive = live;

			Gui.enabled = live;
			try
			{
				UiAt.readDropdown(WindowHost.kScreenView, Vector2.zero);

				if (Ui.overlayDropdownLayered)
					MenuHost.consumeMouseDown0();

				draw();

				Gui.color = prevColor;
				UiAt.dropdownPopup(WindowHost.kScreenView, Vector2.zero);
			}
			catch (Exception ex)
			{
				UiLog.logException(location, ex);

				TextInputFocus.clear();
			}
			finally
			{
				Widgets.m_overlayPass = false;
				WindowHost.interactive = false;

				MenuHost.m_currentBlock = prevBlock;
				MenuHost.scrollMouse = savedScroll;
				MenuHost.mousePosition = savedMouse;

				Gui.enabled = prevEnabled;
				Gui.color = prevColor;
				Gui.restoreGui(prevMatrix);
			}
		}

		internal static Rect centered(float width, float height)
		{
			float spaceW = Gui.spaceWidth;

			float spaceH = Gui.spaceHeight;

			float w = FastMath.min(width, FastMath.max(0f, spaceW - Layout.kScreenMargin * 2f));

			float h = FastMath.min(height, FastMath.max(0f, spaceH - Layout.kScreenMargin * 2f));
			return Gui.snapRect(RectMath.create((spaceW - w) * 0.5f, (spaceH - h) * 0.5f, w, h));
		}

		// a null title leaves the band empty for the caller
		internal static MenuAnimation.SheetScope begin(string animId, Rect window, string title, out Rect content)
		{
			UiAt.scrim(Gui.spaceWidth, Gui.spaceHeight, MenuAnimation.alphaOf(animId));

			Gui.color = ColorMath.kWhite;

			MenuAnimation.SheetScope sheet = MenuAnimation.beginSheet(animId, MotionMath.extent(RectMath.w(window), RectMath.h(window)));

			// same opacity knob as the menu window
			Gui.color = ColorMath.fade(Gui.color, Theme.menuAlpha);

			Theme.drawWindowBg(window);
			Theme.drawTitleBand(window, Layout.kTitleBarH, Layout.kTitleAccentH);

			if (title != null)
				UiText.alignedLabel(RectMath.create(RectMath.x(window), RectMath.y(window), RectMath.w(window), Layout.kTitleBarH), title, Theme.title, TextAnchor.MiddleCenter);

			content = Theme.windowBody(window, Layout.kTitleBarH);
			Theme.drawWindowRegion(window, content, Theme.panelFill);
			return sheet;
		}

		// sizes are vanilla popup sizes in design units that the ui scale scales like the menu
		internal static Rect inner(Rect content) => RectMath.inset(content, Layout.kPad);

		// window width that wraps a body of this width
		internal static float widthFor(float bodyW) => bodyW + Layout.kPad * 2f + Theme.borderWidth * 2f;

		// window height that wraps a body of this height under the title band
		internal static float heightFor(float bodyH) => Layout.kTitleBarH + bodyH + Layout.kPad * 2f + Theme.borderWidth;
	}
}
