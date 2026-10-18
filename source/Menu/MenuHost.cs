using System;
using System.Collections.Generic;
using UnityEngine;
using static Trellis.Widgets;

namespace Trellis
{
	internal static partial class MenuHost
	{
		private const int kDefaultWinH = 750;
		private const int kDefaultWinW = 850;
		private const int kDefaultWinX = 50;
		private const int kDefaultWinY = 50;
		private const float kTitleBarH = 32f;
		private const float kAccentThickness = 2f;
		private const float kEdgeSize = WindowChrome.kEdgeSize;
		private const float kMinTitle = 80f;
		private const float kTabW = 88f;
		private const float kTabH = 32f;
		private const float kTabGap = 2f;
		private const float kTabsTop = 4f;
		private const float kAccentInsetY = 4f;
		private const float kAccentW = 3f;
		private const float kMinW = 850f;
		private const float kMinH = 750f;
		internal const float kPad = 10f;
		internal const float kScrollThickness = 8f;
		internal const float kContentMargin = Layout.kBlockGap;
		private const float kScrollReserve = MenuHost.kScrollThickness + MenuHost.kContentMargin;
		private const float kFadeHeight = 32f;
		internal const float kScrollSpeed = 40f;
		private const float kDimFactor = 0.72f;
		internal const float kBaseWidth = Gui.kDesignWidth;
		internal const float kSwatchW = 30f;
		internal const float kLabelW = 125f;
		internal const float kDropdownMaxW = 120f;
		internal const float kDropdownItemH = 24f;
		internal const float kFieldH = 24f;
		private const float kNotifySeconds = 5f;
		private const string kSwitchKey = "tab";
		private const int kIndicatorKey = 0x51DE;
		private const int kTintKey = 0xD9A6;

		private static readonly List<PopupRow> kPopups = new List<PopupRow>();

		private static TabRow[] m_tabs = Array.Empty<TabRow>();
		private static ScrollState[] m_tabScroll = Array.Empty<ScrollState>();
		private static float[] m_contentHeights = Array.Empty<float>();
		private static string m_title = string.Empty;
		private static string m_meta = string.Empty;
		private static Action m_onFailure;
		private static Func<KeyCode, bool> m_toggleKey;

		[Config(0x69D5DA4A741DC27D)]
		internal static float winW = MenuHost.kDefaultWinW;

		[Config(0x69D5DB4A741DC430)]
		internal static float winH = MenuHost.kDefaultWinH;

		internal static bool m_visible;
		private static MotionValue m_windowLight;
		private static float m_winX = MenuHost.kDefaultWinX;
		private static float m_winY = MenuHost.kDefaultWinY;
		private static WindowDragState m_drag;
		private static float m_cachedScale;
		private static float m_scaleWidth = -1f;
		private static float m_scaleDpi = -1f;
		private static Matrix4x4 m_matrix = VectorMath.kIdentityMatrix;
		private static int m_activeTab;
		private static float m_failureNotify = -MenuHost.kNotifySeconds;

		internal static float dpiScale { get => Gui.DpiScale; set => Gui.DpiScale = value; }

		private static float displayFade => Fade.frame(ref MenuHost.m_windowLight, MenuHost.m_visible, MotionMath.extent(MenuHost.winW, MenuHost.winH), AnimationConfig.windowEnabled);
		internal static bool windowShown => MenuHost.m_visible || !Fade.dark(MenuHost.m_windowLight);
		internal static float designW => MenuHost.kBaseWidth / FastMath.max(MenuHost.dpiScale, MenuHost.kMinDpi);
		internal static float designH => Gui.screenHeight / Gui.designScale();
		internal static float baseHeight => Gui.screenHeight / Gui.baseScale();

		internal static Vector2 mousePosition { get; set; }
		internal static Vector2 scrollMouse { get; set; }

		private static bool layeredPopup => MenuHost.m_popupActive || Widgets.menuDropdownLayered || Widgets.m_multiSelectId != null || ColorPicker.popupOpen;

		internal static bool popupShown => MenuHost.m_popupActive || Widgets.menuDropdownOpen || Widgets.m_multiSelectId != null || ColorPicker.popupOpen || MenuHost.hostOpen();

		private static Rect windowRect => RectMath.create(MenuHost.m_winX, MenuHost.m_winY, MenuHost.winW, MenuHost.winH);

		// row order is the sidebar order so a tab index is a row index
		internal static void setTabs(TabRow[] tabs)
		{
			MenuHost.m_tabs = tabs;
			MenuHost.m_tabScroll = new ScrollState[tabs.Length];
			MenuHost.m_contentHeights = new float[tabs.Length];
			MenuHost.m_activeTab = 0;
		}

		internal static void setTitle(string title, string meta)
		{
			MenuHost.m_title = title ?? string.Empty;
			MenuHost.m_meta = meta ?? string.Empty;
		}

		internal static void addPopup(PopupRow popup) => MenuHost.kPopups.Add(popup);

		internal static void setFailure(Action notify) => MenuHost.m_onFailure = notify;

		// text fields let the menu key through so typing never eats the close
		internal static void setToggle(Func<KeyCode, bool> togglesMenu) => MenuHost.m_toggleKey = togglesMenu;

		internal static bool togglesMenu(KeyCode key) => MenuHost.m_toggleKey != null && MenuHost.m_toggleKey(key);

		internal static bool toggle()
		{
			TextInputFocus.clear();

			MenuHost.m_visible = !MenuHost.m_visible;
			if (MenuHost.m_visible)
			{
				MenuHost.snapPopups();
				MenuHost.onMenuOpened();
			}
			else
				MenuHost.onMenuClosed();

			return MenuHost.m_visible;
		}

		// true while the open window holds the game's input
		internal static bool update(bool captureInput)
		{
			if (!MenuHost.windowShown)
			{
				if (MenuHost.popupShown)
					MenuHost.snapPopups();

				MenuHost.restoreInput();
				return false;
			}

			Cursor.visible = true;
			Cursor.lockState = CursorLockMode.None;

			MenuHost.restoreCursor();

			bool capturing = MenuHost.m_visible && captureInput;
			if (capturing)
				MenuHost.blockInput();
			else
				MenuHost.restoreInput();

			MenuHost.primeMouse(Live.frameCount);

			Vector2 mouse = MenuHost.scaledMouse();
			if (Input.GetMouseButtonDown(0) && !ColorPicker.popupContains(mouse))
			{
				int edges = MenuHost.detectEdges(mouse);

				Rect window = MenuHost.windowRect;
				if (edges != 0)
					MenuHost.m_drag.beginResize(edges, mouse, window);
				else if (RectMath.contains(RectMath.create(MenuHost.m_winX + MenuHost.kEdgeSize, MenuHost.m_winY, MenuHost.winW - MenuHost.kEdgeSize * 2f, MenuHost.kTitleBarH), mouse))
					MenuHost.m_drag.beginMove(mouse, window);
			}

			if (Input.GetMouseButtonUp(0))
				MenuHost.m_drag.end();

			if (MenuHost.m_drag.m_dragging)
			{
				Vector2 movePosition = MenuHost.m_drag.movePosition(mouse);
				MenuHost.m_winX = movePosition.x;
				MenuHost.m_winY = movePosition.y;
			}
			else if (MenuHost.m_drag.m_resizeEdges != 0)
			{
				Rect resized = MenuHost.m_drag.resize(mouse, MenuHost.kMinW, MenuHost.kMinH);
				MenuHost.m_winX = RectMath.x(resized);
				MenuHost.m_winY = RectMath.y(resized);
				MenuHost.winW = RectMath.w(resized);
				MenuHost.winH = RectMath.h(resized);
			}

			MenuHost.clampWindow();
			return capturing;
		}

		internal static void onGUI()
		{
			if (!MenuHost.windowShown)
			{
				MenuHost.restoreInput();
				return;
			}

			EventType eventType = Event.current.type;
			if (eventType == EventType.MouseMove)
				return;
			if (MenuHost.m_drag.active && eventType != EventType.Layout && eventType != EventType.Repaint)
				return;

			Theme.init();
			Theme.applyCursor();

			MenuAnimation.beginFrame();

			float screenW = Gui.screenWidth;
			if (screenW != MenuHost.m_scaleWidth || MenuHost.dpiScale != MenuHost.m_scaleDpi)
			{
				MenuHost.m_cachedScale = screenW / MenuHost.kBaseWidth * MenuHost.dpiScale;
				MenuHost.m_matrix = MatrixMath.scale(VectorMath.v3(MenuHost.m_cachedScale, MenuHost.m_cachedScale, 1f));
				MenuHost.m_scaleWidth = screenW;
				MenuHost.m_scaleDpi = MenuHost.dpiScale;
			}

			float scale = MenuHost.m_cachedScale;
			MenuHost.mousePosition = MenuHost.designMouse(scale);

			Matrix4x4 prevMatrix = Gui.matrix;

			Gui.matrix = MenuHost.m_matrix;

			MenuHost.snapWindow();

			Color prevColor = Gui.color;

			Gui.color = ColorMath.withAlpha(ColorMath.kWhite, MenuHost.displayFade * Theme.menuAlpha);

			try
			{
				MenuHost.draw();
			}
			catch (Exception ex)
			{
				MenuHost.noteFailure(ex);
			}
			finally
			{
				Gui.color = prevColor;
				Gui.matrix = prevMatrix;

				TextInputFocus.endFrame();

				MenuAnimation.endFrame();
			}
		}

		internal static void closePopups()
		{
			TextInputFocus.clear();

			Widgets.snapCloseDropdown();
			Widgets.snapCloseMultiSelect();
			Widgets.closeKeybind();
			Widgets.closeSliderEdit();

			ColorPicker.snapClose();

			foreach (PopupRow popup in MenuHost.kPopups)
				popup.m_close?.Invoke();
		}

		internal static void cleanup()
		{
			MenuHost.m_visible = false;
			MenuHost.m_windowLight = MotionValue.at(0f);

			MenuHost.m_drag.end();

			TextInputFocus.clear();

			MenuHost.snapPopups();

			MenuAnimation.reset();

			MenuWindowBackground.cleanup();

			MenuHost.restoreInput();
		}

		// a widget that throws mid draw leaves scroll views and groups half wound
		private static void noteFailure(Exception ex)
		{
			UiLog.logException("MenuHost.draw", ex);

			TextInputFocus.clear();

			MenuHost.snapPopups();

			float now = Gui.unscaledTime;
			if (now - MenuHost.m_failureNotify < MenuHost.kNotifySeconds)
				return;

			MenuHost.m_failureNotify = now;

			MenuHost.m_onFailure?.Invoke();
		}

		private static void snapPopups()
		{
			MenuHost.closePopups();
			MenuHost.snapPopup();
		}

		private static bool hostOpen()
		{
			foreach (PopupRow popup in MenuHost.kPopups)
			{
				if (popup.m_open != null && popup.m_open())
					return true;
			}
			return false;
		}

		private static void draw()
		{
			bool repaint = Gui.repainting;

			Rect winRect = MenuHost.windowRect;
			Theme.drawImageBg(winRect);
			Theme.drawTitleBand(winRect, MenuHost.kTitleBarH, MenuHost.kAccentThickness, Theme.dragTint(MenuAnimation.hoverProgress(MenuHost.kTintKey, MenuHost.m_drag.m_dragging)));

			MenuHost.m_frameTooltip = default;

			if (repaint)
			{
				int highlightEdges = MenuHost.m_drag.m_resizeEdges != 0 ? MenuHost.m_drag.m_resizeEdges : MenuHost.detectEdges(MenuHost.scaledMouse());
				MenuHost.drawEdges(highlightEdges);
			}

			Gui.label(RectMath.create(MenuHost.m_winX + MenuHost.kPad + 2f, MenuHost.m_winY + 6f, MenuHost.winW - MenuHost.kPad * 2f, MenuHost.kTitleBarH), MenuHost.m_title, Theme.title);

			UiText.fittedLabel(RectMath.create(MenuHost.m_winX + MenuHost.kPad, MenuHost.m_winY, MenuHost.winW - MenuHost.kPad * 2f - 2f, MenuHost.kTitleBarH - MenuHost.kAccentThickness), MenuHost.m_meta, Theme.label, TextAnchor.MiddleRight);

			if (MenuHost.m_tabs.Length == 0)
			{
				MenuHost.drawTooltip();
				MenuHost.drawPopup();
				return;
			}

			float tabTop = MenuHost.m_winY + MenuHost.kTitleBarH + MenuHost.kPad;

			float contentH = MenuHost.winH - MenuHost.kTitleBarH - MenuHost.kPad * 1.5f;

			float contentLeft = MenuHost.m_winX + MenuHost.kTabW + MenuHost.kPad * 2f;

			float contentW = MenuHost.winW - MenuHost.kTabW - MenuHost.kPad * 3f;

			float panelLeft = contentLeft - MenuHost.kPad / 2f;

			float panelWidth = contentW + MenuHost.kPad;

			float panelBottom = tabTop + contentH;

			float gridLeft = panelLeft + MenuHost.kContentMargin;

			float gridWidth = panelWidth - MenuHost.kContentMargin * 2f;

			float headerY = tabTop + MenuHost.kContentMargin;

			float headerW = gridWidth;

			float headerH = MenuHost.headerHeight(MenuHost.m_activeTab);

			Rect viewRect = RectMath.create(gridLeft, headerY + headerH, gridWidth, panelBottom - MenuHost.kContentMargin - (headerY + headerH));

			int tabIndex = MenuHost.m_activeTab;
			ColorPicker.readPopup(viewRect, MenuHost.m_tabScroll[tabIndex].m_position);

			Widgets.readDropdown(viewRect, MenuHost.m_tabScroll[tabIndex].m_position);
			Widgets.readMultiSelect(viewRect, MenuHost.m_tabScroll[tabIndex].m_position);

			foreach (PopupRow popup in MenuHost.kPopups)
				popup.m_dismiss?.Invoke(viewRect, MenuHost.m_tabScroll[tabIndex].m_position);

			MenuHost.pollPopup();

			bool layered = MenuHost.layeredPopup;

			bool popupOpen = layered || MenuHost.hostOpen();

			if (layered)
				MenuHost.consumeMouseDown0();

			MenuHost.drawTabs(tabTop, contentH);

			float dimAlpha = FastMath.clamp01(MenuHost.popupDim() * AnimationConfig.DimStrength);

			Color undimmed = Gui.color;

			float dimFactor = FastMath.lerp(1f, MenuHost.kDimFactor, dimAlpha);
			Gui.color = ColorMath.shade(undimmed, dimFactor);

			Rect panelRect = RectMath.create(panelLeft, tabTop, panelWidth, contentH);
			Theme.drawPanelBg(panelRect);

			int outgoingTab = MenuAnimation.outgoingValue(MenuHost.kSwitchKey);

			bool outgoing = outgoingTab >= 0 && outgoingTab != tabIndex && outgoingTab < MenuHost.m_tabs.Length;
			if (outgoing)
				MenuHost.drawOldHeader(gridLeft, headerY, headerW, outgoingTab);

			using (MenuAnimation.beginSwitch(MenuHost.kSwitchKey))
				MenuHost.drawHeader(tabIndex, gridLeft, headerY, headerW);

			float itemW = MenuHost.itemWidth(tabIndex, gridWidth);

			float virtualH = MenuHost.virtualHeight(tabIndex, RectMath.h(viewRect));

			Rect virtualRect = RectMath.create(0, 0, itemW, virtualH);

			float maxScroll = LayoutMath.scrollMax(virtualH, RectMath.h(viewRect));

			// unity scroll view eats an unused wheel event and moves on its own
			if (Event.current.type == EventType.ScrollWheel && (popupOpen || RectMath.contains(viewRect, MenuHost.mousePosition)))
				Event.current.Use();

			Widgets.easeScroll(ref MenuHost.m_tabScroll[tabIndex], maxScroll);

			Vector2 scrollPosition = MenuHost.m_tabScroll[tabIndex].m_position;

			bool mouseIn = RectMath.contains(viewRect, MenuHost.mousePosition);
			MenuHost.scrollMouse = (popupOpen || !mouseIn) ? MenuHost.kOffscreenMouse : VectorMath.v2(MenuHost.mousePosition.x - RectMath.x(viewRect) + scrollPosition.x, MenuHost.mousePosition.y - RectMath.y(viewRect) + scrollPosition.y);

			if (!popupOpen && maxScroll > 0f && MenuHost.rawMouseDown0)
			{
				Rect scrollHit = RectMath.create(RectMath.xMax(viewRect) - MenuHost.kScrollReserve, RectMath.y(viewRect), MenuHost.kScrollReserve, RectMath.h(viewRect));
				if (RectMath.contains(scrollHit, MenuHost.mousePosition))
					MenuHost.consumeMouseDown0();
			}

			if (outgoing)
			{
				float outgoingH = MenuHost.headerHeight(outgoingTab);

				Rect outgoingRect = RectMath.create(gridLeft, headerY + outgoingH, gridWidth, panelBottom - MenuHost.kContentMargin - (headerY + outgoingH));
				MenuHost.drawOldContent(outgoingRect, outgoingTab);
			}

			Vector2 scrollResult = Gui.beginScrollView(viewRect, scrollPosition, virtualRect);

			bool freezeContent = popupOpen && !Gui.repainting;
			if (freezeContent)
				Gui.enabled = false;

			MenuAnimation.setContent(VectorMath.v2(RectMath.x(viewRect) - scrollPosition.x, RectMath.y(viewRect) - scrollPosition.y), RectMath.w(viewRect));
			using (MenuHost.beginViewport(scrollPosition.y, scrollPosition.y + RectMath.h(viewRect), !popupOpen))
			using (MenuAnimation.beginSwitch(MenuHost.kSwitchKey))
				MenuHost.m_contentHeights[tabIndex] = MenuHost.m_tabs[tabIndex].m_draw(itemW, RectMath.h(viewRect));

			if (freezeContent)
				Gui.enabled = true;

			Gui.endScrollView();

			Widgets.syncDrag(ref MenuHost.m_tabScroll[tabIndex], scrollResult.y);

			// page takes the wheel last so any scroll area under the cursor wins it
			if (!popupOpen)
				Widgets.applyWheel(viewRect, ref MenuHost.m_tabScroll[tabIndex], maxScroll, MenuHost.kScrollSpeed, MenuHost.mousePosition);

			float scrollX = RectMath.xMax(viewRect) - MenuHost.kScrollThickness;
			if (Widgets.scrollbarVisible(ref MenuHost.m_tabScroll[tabIndex], maxScroll))
				Widgets.drawScrollbarAt(scrollX, RectMath.y(viewRect), MenuHost.kScrollThickness, RectMath.h(viewRect), maxScroll, ref MenuHost.m_tabScroll[tabIndex], MenuHost.mousePosition, MenuHost.rawMouseDown0 && !popupOpen);

			scrollPosition = MenuHost.m_tabScroll[tabIndex].m_position;
			MenuHost.drawFades(viewRect, itemW, scrollPosition.y, maxScroll);

			Gui.color = undimmed;

			Widgets.drawDropdownPopup(viewRect, scrollPosition);
			Widgets.drawMultiSelectPopup(viewRect, scrollPosition);

			MenuHost.drawHostPopups(viewRect, scrollPosition, false);

			ColorPicker.drawPopup(viewRect, scrollPosition);

			MenuHost.drawHostPopups(viewRect, scrollPosition, true);

			MenuHost.drawTooltip();
			MenuHost.drawPopup();
		}

		private static void drawHostPopups(Rect viewRect, Vector2 scrollPosition, bool abovePicker)
		{
			foreach (PopupRow popup in MenuHost.kPopups)
			{
				if (popup.m_abovePicker == abovePicker)
					popup.m_draw?.Invoke(viewRect, scrollPosition);
			}
		}

		private static float popupDim()
		{
			float alpha = 0f;
			if (Widgets.m_dropdownAnim != null)
				alpha = FastMath.max(alpha, MenuAnimation.alphaOf(Widgets.m_dropdownAnim));

			if (Widgets.m_multiSelectAnim != null)
				alpha = FastMath.max(alpha, MenuAnimation.alphaOf(Widgets.m_multiSelectAnim));

			string pickerId = ColorPicker.activeAnim;
			if (pickerId != null)
				alpha = FastMath.max(alpha, MenuAnimation.alphaOf(pickerId));

			foreach (PopupRow popup in MenuHost.kPopups)
			{
				if (popup.m_alpha != null)
					alpha = FastMath.max(alpha, popup.m_alpha());
			}
			return alpha;
		}

		private static void drawOldHeader(float x, float y, float w, int outIndex)
		{
			try
			{
				using (MenuAnimation.beginOutgoing(MenuHost.kSwitchKey))
					MenuHost.drawHeader(outIndex, x, y, w);
			}
			catch (Exception ex)
			{
				UiLog.logException("MenuHost.drawOldHeader", ex);
			}
		}

		private static void drawOldContent(Rect viewRect, int outIndex)
		{
			int savedActive = MenuHost.m_activeTab;

			MenuHost.m_activeTab = outIndex;
			try
			{
				float itemW = MenuHost.itemWidth(outIndex, RectMath.w(viewRect));

				float virtualH = MenuHost.virtualHeight(outIndex, RectMath.h(viewRect));

				Rect virtualRect = RectMath.create(0, 0, itemW, virtualH);

				Vector2 outScroll = Gui.beginScrollView(viewRect, MenuHost.m_tabScroll[outIndex].m_position, virtualRect);

				float scrollY = MenuHost.m_tabScroll[outIndex].m_position.y;
				using (MenuHost.beginViewport(scrollY, scrollY + RectMath.h(viewRect), !MenuHost.popupShown))
				using (MenuAnimation.beginOutgoing(MenuHost.kSwitchKey))
					MenuHost.m_tabs[outIndex].m_draw(itemW, RectMath.h(viewRect));

				Gui.endScrollView();

				Widgets.syncDrag(ref MenuHost.m_tabScroll[outIndex], outScroll.y);
			}
			catch (Exception ex)
			{
				UiLog.logException("MenuHost.drawOldContent", ex);
			}
			finally
			{
				MenuHost.m_activeTab = savedActive;
			}
		}

		private static bool ownsScroll(int tab)
		{
			Func<bool> owns = MenuHost.m_tabs[tab].m_ownsScroll;
			return owns != null && owns();
		}

		private static float itemWidth(int tab, float contentW)
		{
			if (MenuHost.ownsScroll(tab))
				return contentW;

			return contentW - MenuHost.kScrollReserve;
		}

		private static float virtualHeight(int tab, float viewH)
		{
			if (MenuHost.ownsScroll(tab))
				return viewH;

			return FastMath.max(MenuHost.m_contentHeights[tab], viewH);
		}

		private static void drawFades(Rect viewRect, float contentW, float scrollY, float maxScroll)
		{
			if (maxScroll <= 0f)
				return;

			Color prevColor = Gui.color;

			float topStrength = FastMath.clamp01(scrollY / MenuHost.kFadeHeight);
			if (topStrength > 0f)
			{
				Gui.color = ColorMath.fade(prevColor, GraphicsInfo.fadeAlpha(topStrength));

				ScrollFade.draw(MenuHost.windowRect, MenuHost.fadeBand(viewRect, contentW, RectMath.y(viewRect)), true, MenuHost.kContentMargin);
			}

			float bottomStrength = FastMath.clamp01((maxScroll - scrollY) / MenuHost.kFadeHeight);
			if (bottomStrength > 0f)
			{
				Gui.color = ColorMath.fade(prevColor, GraphicsInfo.fadeAlpha(bottomStrength));

				ScrollFade.draw(MenuHost.windowRect, MenuHost.fadeBand(viewRect, contentW, RectMath.yMax(viewRect) - MenuHost.kFadeHeight), false, MenuHost.kContentMargin);
			}
			Gui.color = prevColor;
		}

		private static Rect fadeBand(Rect viewRect, float contentW, float y) => RectMath.create(RectMath.x(viewRect) - MenuHost.kContentMargin, y, contentW + MenuHost.kContentMargin * 2f, MenuHost.kFadeHeight);

		private static void drawHeader(int tab, float x, float y, float w) => MenuHost.m_tabs[tab].m_drawHeader?.Invoke(x, y, w);
		private static float headerHeight(int tab) => MenuHost.m_tabs[tab].m_drawHeader != null ? Widgets.kSubTabBarH : 0f;

		private static void drawTabs(float top, float availableH)
		{
			Rect sidebarRect = RectMath.create(MenuHost.m_winX + MenuHost.kPad / 2f, top, MenuHost.kTabW + MenuHost.kPad, availableH);
			Theme.drawSidebarBg(sidebarRect);

			float groupX = MenuHost.m_winX + MenuHost.kPad;

			float groupY = top;

			Vector2 groupMouse = VectorMath.v2(MenuHost.mousePosition.x - groupX, MenuHost.mousePosition.y - groupY);

			Gui.beginGroup(RectMath.create(groupX, groupY, MenuHost.kTabW, availableH));

			float accentY = MenuHost.kTabsTop;

			float y = MenuHost.kTabsTop;
			for (int i = 0; i < MenuHost.m_tabs.Length; i++)
			{
				GUIStyle style = MenuHost.m_activeTab == i ? Theme.activeTab : Theme.tab;
				if (Widgets.scaledButton(RectMath.create(0, y, MenuHost.kTabW, MenuHost.kTabH), MenuHost.m_tabs[i].m_label, style, groupMouse))
				{
					MenuAnimation.noteSwitch(MenuHost.kSwitchKey, MenuHost.m_activeTab, i);

					MenuHost.m_activeTab = i;
					MenuHost.closePopups();
				}

				if (MenuHost.m_activeTab == i)
					accentY = y;

				y += MenuHost.kTabH + MenuHost.kTabGap;
			}

			Rect accentTarget = RectMath.create(0f, accentY + MenuHost.kAccentInsetY, MenuHost.kAccentW, MenuHost.kTabH - MenuHost.kAccentInsetY * 2f);

			MarkerFrame accent = MenuAnimation.displayedMarker(MenuHost.kIndicatorKey, accentTarget, groupMouse.y, false);
			Widgets.drawMarker(accent, Theme.accentBar);

			Gui.endGroup();
		}
	}
}
