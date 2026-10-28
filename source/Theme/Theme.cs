using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Trellis
{
	internal static class Theme
	{
		private const int kCodePadLeft = 3;
		private const int kCodePadRight = 3;
		private const int kDropdownPadLeft = 10;
		private const int kDropdownPadRight = 20;
		private const int kItemPadLeft = 10;
		private const int kItemPadRight = 4;
		private const int kPopupPadBottom = 6;
		private const int kPopupPadLeft = 12;
		private const int kPopupPadRight = 12;
		private const int kPopupPadTop = 6;
		private const int kPadBottom = 4;
		private const int kPadLeft = 8;
		private const int kPadRight = 8;
		private const int kPadTop = 4;
		private const int kSectionPadLeft = 10;
		private const int kSubTabPadLeft = 12;
		private const int kSubTabPadRight = 12;
		private const int kTabPadLeft = 14;
		private const int kTabPadRight = 4;
		private const int kFieldPadBottom = 3;
		private const int kFieldPadLeft = 6;
		private const int kFieldPadRight = 6;
		private const int kFieldPadTop = 3;

		internal const string kAccentPicker = "menu_accent";
		internal const string kTintPicker = "theme_tint";
		private const float kPressShade = 0.7f;
		private const string kPrewarmChars = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ" + "[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
		private const int kFadeWidth = 128;
		private const int kFlatSize = 2;

		internal static readonly Color kSelectionTint = ColorMath.rgba(0.55f, 0.55f, 0.55f, 0.28f);
		private static readonly WidgetChrome kButtonChrome = new WidgetChrome();
		private static readonly WidgetChrome kDangerChrome = new WidgetChrome();
		private static readonly WidgetChrome kPopupChrome = new WidgetChrome();
		private static readonly WidgetChrome kTabChrome = new WidgetChrome();
		private static readonly WidgetChrome kActiveTab = new WidgetChrome();
		private static readonly WidgetChrome kSubTab = new WidgetChrome();
		private static readonly WidgetChrome kActiveSubTab = new WidgetChrome();
		private static readonly WidgetChrome kDropdownChrome = new WidgetChrome();
		private static readonly WidgetChrome kDropdownOpen = new WidgetChrome();
		private static readonly WidgetChrome kFieldChrome = new WidgetChrome();
		private static readonly WidgetChrome kKeybindCapture = new WidgetChrome();
		private static readonly WidgetChrome kCheckOff = new WidgetChrome();
		private static readonly WidgetChrome kCheckOn = new WidgetChrome();
		private static readonly WidgetChrome kThumbChrome = new WidgetChrome();
		internal static readonly WidgetChrome kItemChrome = new WidgetChrome();
		private static readonly WidgetChrome[] kStyleChromes = new[] { Theme.kButtonChrome, Theme.kDangerChrome, Theme.kPopupChrome, Theme.kTabChrome, Theme.kActiveTab, Theme.kSubTab, Theme.kActiveSubTab, Theme.kDropdownChrome, Theme.kDropdownOpen, Theme.kFieldChrome, Theme.kKeybindCapture };
		private static readonly List<Texture2D> kTrackedTextures = new List<Texture2D>();
		private static readonly List<Texture2D> kCaretTextures = new List<Texture2D>();
		private static readonly List<Action> kRebuildHooks = new List<Action>();
		private static readonly List<Action> kCleanupHooks = new List<Action>();
		private static readonly List<StyleText> kStyleTexts = new List<StyleText>();
		internal static readonly GUIContent kScratchContent = new GUIContent();

		private static readonly int[] kPrewarmSizes = new[] { FontSizes.kSmall, FontSizes.kCompact, FontSizes.kBody, FontSizes.kPopup, FontSizes.kTitle };

		[Config]
		public static Color AccentColor = ColorHex.parse("00AAFF");

		internal static Color m_accent;
		private static Color m_accentHover;
		private static Color m_accentDim;
		private static Color m_popupBorder;
		private static Color m_sectionTitle;
		private static Color m_accentFaint;
		private static Color m_accentSubtle;
		private static Color m_accentGlow;
		private static Color m_blockBorder;
		private static bool m_initialized;
		private static Texture2D[] m_caretFrames;
		private static Texture2D[] m_caretHoverFrames;
		private static Color m_caretNormal;
		private static Color m_caretHover;
		private static Font m_prewarmedFont;
		private static int m_themeHash;
		private static int m_correctionVersion = -1;
		private static Color m_accentBright;
		private static Color m_hoverText;
		private static Color m_edgeHighlight;
		private static Color m_checkColor;
		private static Color m_sliderFill;
		private static Color m_trackFill;
		private static Color m_scrollFill;
		private static Color m_tabFill;
		private static Color m_tabHover;

		private static bool m_rebuildPending;
		private static float m_rebuildTime;
		private static int m_refreshedFrame = -1;
		private static Color m_cursorText;
		private static Color m_cursorAccent;
		private static bool m_cursorApplied;

		// accent colour stays on Theme since its config id hashes the owner name
		internal static class Surfaces
		{
			internal static readonly Color kTransparent = ColorMath.rgba(0, 0, 0, 0);

			internal static Color m_base;
			internal static Color m_mantle;
			internal static Color m_panel;
			internal static Color m_surface0;
			internal static Color m_surface1;
			internal static Color m_surface2;
			internal static Color m_overlay0;
			internal static Color m_borderColor;
			internal static Color m_subtleBorder;
		}

		internal static class Text
		{
			internal static Color m_primary;
			internal static Color m_secondary;
		}

		// rewritten every rebake so never capture one in a static initializer
		internal static class Status
		{
			internal static Color m_green = ColorHex.parse(BaseHex.kPositive);
			internal static Color m_red = ColorHex.parse(BaseHex.kDanger);
			internal static Color m_gold = ColorHex.parse(BaseHex.kWarning);
			internal static Color m_redHover;
		}

		internal static class Hex
		{
			internal const string kTextSecondary = "#" + BaseHex.kTextSecondary;
			internal const string kPositive = "#" + BaseHex.kPositive;
			internal const string kWarning = "#" + BaseHex.kWarning;
			internal const string kDanger = "#" + BaseHex.kDanger;
		}

		internal sealed class WidgetChrome
		{
			internal GUIStyle m_style;
			internal Color m_normalFill;
			internal Color m_normalBorder;
			internal Color m_hoverFill;
			internal Color m_hoverBorder;
			internal Color m_activeFill;
			internal Color m_activeBorder;

			internal void set(Color normalFill, Color normalBorder, Color hoverFill, Color hoverBorder, Color activeFill, Color activeBorder)
			{
				m_normalFill = normalFill;
				m_normalBorder = normalBorder;
				m_hoverFill = hoverFill;
				m_hoverBorder = hoverBorder;
				m_activeFill = activeFill;
				m_activeBorder = activeBorder;
			}
		}

		// equal channels linearize to one 8 bit code so vulkan cant give them a navy cast
		private static class BaseHex
		{
			internal const string kBase = "171717";
			internal const string kMantle = "0F0F0F";
			internal const string kPanel = "1D1D1D";
			internal const string kSurface0 = "242424";
			internal const string kSurface1 = "2D2D2D";
			internal const string kSurface2 = "383838";
			internal const string kOverlay0 = "505050";
			internal const string kBorderColor = "0C0C0C";
			internal const string kSubtleBorder = "3B3B3B";
			internal const string kTextPrimary = "E0E0E0";
			internal const string kTextSecondary = "A9A9A9";
			internal const string kPositive = "4CAF50";
			internal const string kWarning = "D4A017";
			internal const string kDanger = "E05555";
		}

		private sealed class StyleText
		{
			internal GUIStyle m_style;
			internal Func<Color> m_source;
			internal Color m_applied;
		}

		private static class TabStyle
		{
			internal const float kFillMix = 0.14f;
			internal const float kHoverLift = (TabStyle.kHoverMix - TabStyle.kFillMix) / (1f - TabStyle.kFillMix);
			private const float kHoverMix = 0.2f;
		}

		private static class HoverText
		{
			internal const float kTargetLuminance = 0.55f;
		}

		private static class TintModel
		{
			internal const float kCastSaturation = 0.35f;
			internal const float kTintBlend = 4f;
			internal const float kTextShare = 0.5f;
		}

		private static class FontSizes
		{
			internal const int kSmall = 13;
			internal const int kCompact = 14;
			internal const int kBody = 15;
			internal const int kPopup = 16;
			internal const int kTitle = 17;
		}

		private static class BoxFill
		{
			internal const float kOpaqueInset = 0.49f;
			internal const float kFadedAlpha = 0.9f;
			internal const float kRadiusTrim = 0.25f;
		}

		private static class TopAccent
		{
			internal const float kInsetX = 6f;
			internal const float kThickness = 2f;
		}

		private static class EdgeFade
		{
			internal const int kEndTexels = 31;
			internal const int kWidth = EdgeFade.kEndTexels * 2 + EdgeFade.kMidTexels;
			internal const int kHeight = 64;
			internal const float kEndFraction = (float)EdgeFade.kEndTexels / EdgeFade.kWidth;
			// opaque core so the clipped row edge never shows through the ramp
			internal const float kCoreFraction = 0.12f;
			private const int kMidTexels = 2;
		}

		private static class EdgeLine
		{
			internal const int kCapTexels = 31;
			internal const int kLength = EdgeLine.kCapTexels * 2 + EdgeLine.kMidTexels;
			internal const float kCapFraction = (float)EdgeLine.kCapTexels / EdgeLine.kLength;
			internal const float kCapLength = 44f;
			private const int kMidTexels = 2;
		}

		private static class Caret
		{
			internal const int kRes = 32;
			internal const float kInset = 0.18f;
			internal const float kEdgeSoftness = 1.5f;
			internal const float kSweepDegrees = 90f;
			internal const int kFrames = 19;
		}

		internal static float cornerR { get; private set; }
		internal static float borderWidth { get; private set; } = 1f;
		internal static Texture2D edgeLineH { get; private set; }
		internal static Texture2D edgeLineV { get; private set; }
		internal static Texture2D scrollFadeTop { get; private set; }
		internal static Texture2D scrollFadeBottom { get; private set; }
		internal static Texture2D titleAccent { get; private set; }
		internal static Texture2D accentBar { get; private set; }
		internal static Texture2D accentFaint { get; private set; }
		internal static GUIStyle label { get; private set; }
		internal static GUIStyle labelHover { get; private set; }
		internal static GUIStyle toggleLabel { get; private set; }
		internal static GUIStyle toggleLabelHover { get; private set; }
		internal static GUIStyle toggleLabelWrap { get; private set; }
		internal static GUIStyle toggleLabelWrapHover { get; private set; }
		internal static GUIStyle title { get; private set; }
		internal static GUIStyle statusLabel { get; private set; }
		internal static GUIStyle button { get; private set; }
		internal static GUIStyle dangerButton { get; private set; }
		internal static GUIStyle tab { get; private set; }
		internal static GUIStyle activeTab { get; private set; }
		internal static GUIStyle subTab { get; private set; }
		internal static GUIStyle activeSubTab { get; private set; }
		internal static GUIStyle dropdownButton { get; private set; }
		internal static GUIStyle dropdownOpen { get; private set; }
		internal static GUIStyle keybindCapture { get; private set; }
		internal static GUIStyle dropdownItem { get; private set; }
		internal static Texture2D sliderTrack { get; private set; }
		internal static Texture2D thumbHover { get; private set; }
		internal static GUIStyle sectionHeader { get; private set; }
		internal static GUIStyle tacticalHeader { get; private set; }
		internal static GUIStyle tacticalHover { get; private set; }
		internal static GUIStyle valueLabel { get; private set; }
		internal static GUIStyle badgeLabel { get; private set; }
		internal static Texture2D divider { get; private set; }
		internal static Texture2D disclosureHover { get; private set; }
		internal static Texture2D scrollTrack { get; private set; }
		internal static Texture2D scrollThumb { get; private set; }
		internal static GUIStyle espLabel { get; private set; }
		internal static GUIStyle espShadow { get; private set; }
		internal static GUIStyle textField { get; private set; }
		internal static GUIStyle textArea { get; private set; }
		internal static GUIStyle tooltipLabel { get; private set; }
		internal static GUIStyle tooltipPreview { get; private set; }
		internal static GUIStyle popupLabel { get; private set; }
		internal static GUIStyle popupButton { get; private set; }
		internal static GUIStyle notifHeader { get; private set; }
		internal static GUIStyle notifLabel { get; private set; }
		internal static GUIStyle logLabel { get; private set; }
		internal static GUIStyle logBold { get; private set; }
		internal static GUIStyle codeLabel { get; private set; }
		internal static GUIStyle codeGutter { get; private set; }
		internal static GUIStyle codeField { get; private set; }
		internal static int layoutVersion { get; private set; }

		internal static float menuAlpha => FastMath.clamp(ThemeConfig.MenuOpacity, ThemeConfig.Limits.kMinMenuOpacity, ThemeConfig.Limits.kMaxMenuOpacity);
		internal static Color panelFill => ColorMath.fade(Surfaces.m_panel, Theme.panelAlpha);
		private static float panelAlpha => FastMath.clamp(ThemeConfig.PanelOpacity, ThemeConfig.Limits.kMinOpacity, ThemeConfig.Limits.kMaxOpacity);
		private static float blockAlpha => FastMath.clamp(ThemeConfig.BlockOpacity, ThemeConfig.Limits.kMinOpacity, ThemeConfig.Limits.kMaxOpacity);
		internal static Color blockFill => ColorMath.fade(Surfaces.m_surface0, Theme.blockAlpha);
		internal static Color blockBorder => Theme.m_blockBorder;
		internal static Color scrollBackdrop => GraphicsInfo.guiOver(Theme.panelFill, MenuWindowBackground.backdropColor);

		internal static Color fpsColor(int fps)
		{
			if (fps >= ThemeConstants.kFpsGood)
				return Status.m_green;
			if (fps >= ThemeConstants.kFpsCaution)
				return Status.m_gold;

			return Status.m_red;
		}

		internal static string tag(string text, Color color) => RichText.wrapColor(text, color);

		// runs after every colour rebake so host palettes follow the theme
		internal static void addRebuild(Action hook) => Theme.kRebuildHooks.Add(hook);

		// runs on cleanup since every texture the theme made is destroyed with it
		internal static void addCleanup(Action hook) => Theme.kCleanupHooks.Add(hook);

		internal static Texture2D horizontalFade(Func<Color> source) => Theme.createHorizontalFade(source, Theme.kFadeWidth);

		internal static void applyCursor()
		{
			try
			{
				GUISettings settings = GUI.skin?.settings;
				if (settings == null)
					return;

				settings.cursorColor = Text.m_primary;
				settings.selectionColor = ColorMath.fade(Theme.m_accent, ThemeConstants.kAlphaSelection);
				settings.cursorFlashSpeed = 0f;
			}
			catch (Exception ex)
			{
				UiLog.debug($"[Theme.applyCursor] {ex.Message}");
			}
		}

		internal static void init()
		{
			if (Theme.m_initialized)
			{
				int frame = Gui.frameCount;
				if (frame == Theme.m_refreshedFrame)
					return;

				if (Theme.m_correctionVersion != GraphicsInfo.correctionVersion || Theme.m_themeHash != ThemeConfig.bakeHash())
					Theme.m_rebuildPending = true;

				if (!Theme.m_rebuildPending || !Theme.rebuildDue())
				{
					Theme.refreshFrame(frame);
					return;
				}

				Theme.m_rebuildPending = false;
				Theme.cleanup();
			}

			try
			{
				Theme.initCore();
			}
			catch (Exception ex)
			{
				UiLog.logException("Theme.init", ex);

				// a half built theme leaks its textures into the next attempt
				Theme.cleanup();
				throw;
			}
		}

		internal static BoxMetrics windowBox(Rect rect) => BoxMetrics.measure(rect, Theme.cornerR, Theme.borderWidth);
		internal static void drawWindowBg(Rect rect) => Theme.drawRoundedBox(rect, Surfaces.m_base, Surfaces.m_borderColor, Theme.cornerR, Theme.borderWidth);

		private static bool tryWindowBox(Rect rect, out BoxMetrics box)
		{
			box = default;
			if (!Gui.repainting)
				return false;

			box = Theme.windowBox(rect);
			return !box.empty;
		}

		internal static void drawWindowFill(Rect rect)
		{
			if (!Theme.tryWindowBox(rect, out BoxMetrics box))
				return;

			Theme.drawBoxFill(box, Surfaces.m_base, box.m_border > 0f);
		}

		internal static void drawWindowImage(Rect rect, Texture texture, ScaleMode scaleMode, Color tint)
		{
			if (!Theme.tryWindowBox(rect, out BoxMetrics box))
				return;

			Theme.fillShape(box, box.m_border > 0f, out Rect fillRect, out float fillRadius);
			Gui.drawRoundedTexture(fillRect, texture, scaleMode, ColorMath.modulate(tint, Gui.color), VectorMath.v4(0f, 0f, 0f, 0f), Theme.uniformRadii(fillRadius));
		}

		internal static void drawImageBg(Rect rect)
		{
			if (!MenuWindowBackground.draw(rect))
			{
				Theme.drawWindowBg(rect);
				return;
			}

			Theme.drawOutline(rect, Surfaces.m_borderColor);
		}

		internal static void drawPopupBg(Rect rect) => Theme.drawRoundedBox(rect, Surfaces.m_base, Theme.m_popupBorder, Theme.cornerR, Theme.borderWidth);
		internal static void drawPanelBg(Rect rect) => Theme.drawRoundedBox(rect, ColorMath.fade(Surfaces.m_panel, Theme.panelAlpha), Surfaces.m_borderColor, Theme.cornerR, Theme.borderWidth);
		internal static void drawBlockBg(Rect rect) => Theme.drawRoundedBox(rect, Theme.blockFill, Theme.m_blockBorder, Theme.cornerR, Theme.borderWidth);
		internal static void drawTooltipBg(Rect rect) => Theme.drawRoundedBox(rect, Surfaces.m_surface1, Surfaces.m_overlay0, Theme.cornerR, Theme.borderWidth);
		internal static void drawSidebarBg(Rect rect) => Theme.drawRoundedBox(rect, ColorMath.fade(Surfaces.m_mantle, Theme.panelAlpha), Surfaces.kTransparent, Theme.cornerR, 0f);
		internal static void drawLogBg(Rect rect) => Theme.drawRoundedBox(rect, ColorMath.fade(Surfaces.m_mantle, Theme.blockAlpha), Surfaces.m_subtleBorder, Theme.cornerR, Theme.borderWidth);

		internal static void drawTitleBand(Rect windowRect, float bandH, float accentH, Color overlay)
		{
			if (!Gui.repainting)
				return;

			BoxMetrics box = Theme.windowBox(windowRect);
			if (box.empty)
				return;

			Color fill = ColorMath.fade(Surfaces.m_mantle, Theme.panelAlpha);
			if (overlay.a > 0f)
				fill = ColorMath.over(overlay, fill);

			float bottom = Theme.bandBottom(box, bandH);
			Theme.drawBandFill(box, bottom, fill);

			Gui.drawStroke(Theme.bandRule(box, bottom, box.m_border), Surfaces.m_borderColor);
			Gui.drawStroke(Theme.bandRule(box, bottom, accentH), Theme.titleAccent);
		}

		internal static void drawTitleBand(Rect windowRect, float bandH, float accentH) => Theme.drawTitleBand(windowRect, bandH, accentH, ColorMath.kTransparent);

		internal static void drawPopupBand(Rect popupRect, float bandH, float accentH)
		{
			if (!Gui.repainting)
				return;

			BoxMetrics box = Theme.windowBox(popupRect);
			if (box.empty)
				return;

			float bottom = Theme.bandBottom(box, bandH);
			Theme.drawBandFill(box, bottom, Surfaces.m_mantle);

			Gui.drawStroke(Theme.bandRule(box, bottom, accentH), Theme.m_popupBorder);
		}

		internal static Rect windowInner(Rect windowRect) => RectMath.inset(windowRect, Theme.borderWidth);

		internal static Rect windowBody(Rect windowRect, float bandH)
		{
			Rect inner = Theme.windowInner(windowRect);

			float top = FastMath.max(RectMath.y(inner), RectMath.y(windowRect) + bandH);
			return RectMath.minMax(RectMath.x(inner), top, RectMath.xMax(inner), RectMath.yMax(inner));
		}

		internal static void drawWindowRegion(Rect windowRect, Rect region, Color fill) => Theme.drawWindowRegion(windowRect, region, Gui.whiteTexture, fill);

		internal static void drawWindowRegion(Rect windowRect, Rect region, Texture texture, Color tint)
		{
			if (!Gui.repainting || tint.a <= 0f)
				return;

			BoxMetrics box = Theme.windowBox(windowRect);
			if (box.empty)
				return;

			Theme.drawRegion(box, region, texture, tint);
		}

		internal static Color dragTint(float progress) => ColorMath.fade(Theme.m_accent, Theme.accentAlpha(ThemeConstants.kAlphaFaint) * FastMath.clamp01(progress));
		internal static Color hopTint(float progress) => ColorMath.fade(Theme.m_accent, Theme.accentAlpha(ThemeConstants.kAlphaGlow) * FastMath.clamp01(progress));
		internal static void drawTopAccent(Rect boxRect, Texture texture) => Theme.drawTopAccent(boxRect, texture, TopAccent.kInsetX, TopAccent.kThickness);
		internal static void drawTopAccent(Rect boxRect, Color color) => Theme.drawTopAccent(boxRect, color, TopAccent.kInsetX, TopAccent.kThickness);

		internal static void drawTopAccent(Rect boxRect, Texture texture, float insetX, float thickness)
		{
			if (!Theme.tryWindowBox(boxRect, out BoxMetrics box))
				return;

			Gui.drawStroke(Theme.accentRect(box, insetX, thickness), texture);
		}

		internal static void drawOutline(Rect rect, Color color)
		{
			if (!Gui.repainting || color.a <= 0f)
				return;

			BoxMetrics box = Theme.windowBox(rect);
			if (!(!box.empty && box.m_border > 0f))
				return;

			Theme.drawRoundedFrame(box.m_outer, color, box.m_border, box.m_radius);
		}

		internal static void drawHalo(Rect rect, Color color)
		{
			if (!Gui.repainting || color.a <= 0f)
				return;

			BoxMetrics box = Theme.windowBox(rect);
			if (box.empty || box.m_border <= 0f)
				return;

			float radius = box.m_radius > 0f ? box.m_radius + box.m_border : 0f;
			Theme.drawRoundedFrame(RectMath.inflate(box.m_outer, box.m_border), color, box.m_border, radius);
		}

		internal static void drawRoundedBox(Rect rect, Color fill, Color border, float radius, float borderWidth)
		{
			if (!Gui.repainting)
				return;

			BoxMetrics box = BoxMetrics.measure(rect, radius, borderWidth);
			if (box.empty)
				return;

			bool framed = border.a > 0f && box.m_border > 0f;
			if (fill.a > 0f)
				Theme.drawBoxFill(box, fill, framed);

			if (!framed)
				return;

			Theme.drawRoundedFrame(box.m_outer, border, box.m_border, box.m_radius);
		}

		internal static void drawRoundedTint(Rect rect, Color color)
		{
			if (color.a <= 0f || !Theme.tryWindowBox(rect, out BoxMetrics box))
				return;

			Theme.drawBoxFill(box, color, box.m_border > 0f);
		}

		internal static WidgetChrome chromeOf(GUIStyle style)
		{
			foreach (WidgetChrome styleChrome in Theme.kStyleChromes)
			{
				if (object.ReferenceEquals(styleChrome.m_style, style))
					return styleChrome;
			}
			return null;
		}

		internal static void drawChrome(Rect rect, WidgetChrome chrome, float hoverT, bool active) => Theme.drawChromeState(rect, chrome, hoverT, active, Theme.cornerR);
		internal static void drawThumb(Rect rect, float hoverT, bool dragging) => Theme.drawChromeState(rect, Theme.kThumbChrome, hoverT, dragging, FastMath.min(RectMath.w(rect), RectMath.h(rect)) * 0.5f);

		internal static void drawCheckbox(Rect rect, float onT, float hoverT)
		{
			if (!Gui.repainting)
				return;

			Color offFill = ColorMath.lerpVisible(Theme.kCheckOff.m_normalFill, Theme.kCheckOff.m_hoverFill, hoverT);

			Color offBorder = ColorMath.lerpVisible(Theme.kCheckOff.m_normalBorder, Theme.kCheckOff.m_hoverBorder, hoverT);

			Color onFill = ColorMath.lerpVisible(Theme.kCheckOn.m_normalFill, Theme.kCheckOn.m_hoverFill, hoverT);

			Color onBorder = ColorMath.lerpVisible(Theme.kCheckOn.m_normalBorder, Theme.kCheckOn.m_hoverBorder, hoverT);
			Theme.drawRoundedBox(rect, ColorMath.lerpVisible(offFill, onFill, onT), ColorMath.lerpVisible(offBorder, onBorder, onT), Theme.cornerR, Theme.borderWidth);
		}

		internal static void drawField(Rect rect, bool hovered, bool focused) => Theme.drawChromeState(rect, Theme.kFieldChrome, hovered ? 1f : 0f, focused, Theme.cornerR);

		internal static GUIStyle derive(GUIStyle source)
		{
			for (int i = 0; i < Theme.kStyleTexts.Count; i++)
			{
				if (object.ReferenceEquals(Theme.kStyleTexts[i].m_style, source))
					return Theme.recolor(source, Theme.kStyleTexts[i].m_source);
			}
			return new GUIStyle(source);
		}

		internal static void cleanup()
		{
			foreach (Texture2D texture in Theme.kTrackedTextures)
				Textures.destroy(texture);

			Theme.kTrackedTextures.Clear();

			foreach (Action hook in Theme.kCleanupHooks)
				hook();

			Theme.kStyleTexts.Clear();

			ThemeTint.onCleanup();

			ScrollFade.cleanup();

			Theme.m_cursorApplied = false;
			Theme.m_refreshedFrame = -1;

			ColorHex.clearCache();

			ColorPicker.invalidateTextures();

			Theme.m_initialized = false;
		}

		internal static void drawEdgeFade(Rect rect, Texture2D texture, float endWidth)
		{
			if (!Gui.repainting || !Live.isUnityAlive(texture))
				return;

			Rect snapped = Gui.snapRect(rect);
			if (RectMath.w(snapped) <= 0f || RectMath.h(snapped) <= 0f)
				return;

			Theme.drawThreeSlice(snapped, texture, false, endWidth, EdgeFade.kEndFraction);
		}

		internal static void drawEdgeLine(Rect rect, bool vertical)
		{
			Texture2D texture = vertical ? Theme.edgeLineV : Theme.edgeLineH;
			if (!Gui.repainting || !Live.isUnityAlive(texture))
				return;

			Rect snapped = Gui.snapStroke(rect);
			if (RectMath.w(snapped) <= 0f || RectMath.h(snapped) <= 0f)
				return;

			Theme.drawThreeSlice(snapped, texture, vertical, EdgeLine.kCapLength, EdgeLine.kCapFraction);
		}

		// unity RotateAroundPivot pivots wrong under the menu scale so the sweep is baked
		internal static Texture2D disclosureCaret(float progress, bool hovered)
		{
			Texture2D[] frames = hovered ? Theme.m_caretHoverFrames : Theme.m_caretFrames;
			if (frames == null)
				return null;

			int index = FastMath.clamp(FastMath.roundToInt(progress * (Caret.kFrames - 1)), 0, Caret.kFrames - 1);
			return frames[index];
		}

		private static bool rebuildDue()
		{
			float now = Gui.unscaledTime;
			if (now - Theme.m_rebuildTime < ThemeConstants.kRebuildSeconds)
				return false;

			Theme.m_rebuildTime = now;
			return true;
		}

		private static void refreshFrame(int frame)
		{
			Theme.m_refreshedFrame = frame;
			Theme.buildShape();
			Theme.buildColors();

			ThemeTint.retintChanged();

			Theme.refreshStyleText();
			Theme.refreshCursor();
		}

		private static void refreshStyleText()
		{
			for (int i = 0; i < Theme.kStyleTexts.Count; i++)
			{
				StyleText entry = Theme.kStyleTexts[i];

				Color color = entry.m_source();
				if (ColorMath.approxEqual(entry.m_applied, color))
					continue;

				entry.m_applied = color;
				Theme.applyStyleText(entry.m_style, color);
			}
		}

		private static void applyStyleText(GUIStyle style, Color color)
		{
			Color resolved = GraphicsInfo.shaderColor(color);
			style.normal.textColor = resolved;
			style.hover.textColor = resolved;
			style.active.textColor = resolved;
			style.focused.textColor = resolved;
		}

		private static void refreshCursor()
		{
			if (Theme.m_cursorApplied && ColorMath.approxEqual(Theme.m_cursorText, Text.m_primary) && ColorMath.approxEqual(Theme.m_cursorAccent, Theme.m_accent))
				return;

			Theme.m_cursorText = Text.m_primary;
			Theme.m_cursorAccent = Theme.m_accent;
			Theme.m_cursorApplied = true;
			Theme.applyCursor();
		}

		private static float accentAlpha(float baseAlpha)
		{
			float intensity = FastMath.clamp(ThemeConfig.AccentIntensity, ThemeConfig.Limits.kMinAccent, ThemeConfig.Limits.kMaxAccent);
			return FastMath.clamp01(baseAlpha * intensity);
		}

		private static Color shaded(Color color, HsvDelta delta)
		{
			ColorMath.rgbToHsv(color, out float h, out float s, out float v);
			return delta.apply(h, s, v);
		}

		private static Color stylize(string hex, float accentHue, float accentSat, float tint, float brightness) => Theme.stylize(ColorHex.parse(hex), accentHue, accentSat, tint, brightness);

		private static Color stylize(Color baseColor, float accentHue, float accentSat, float tint, float brightness)
		{
			if (brightness != 1f)
			{
				Color scaled = ColorMath.shade(baseColor, brightness);
				baseColor = ColorMath.rgba(FastMath.clamp01(scaled.r), FastMath.clamp01(scaled.g), FastMath.clamp01(scaled.b), 1f);
			}

			float castSaturation = TintModel.kCastSaturation * FastMath.clamp01(accentSat);
			if (tint <= 0f || castSaturation <= 0f)
				return baseColor;

			float value = FastMath.clamp01(FastMath.max(baseColor.r, FastMath.max(baseColor.g, baseColor.b)));

			Color cast = ColorMath.hsvToColor(accentHue, castSaturation, value, 1f);
			return ColorMath.lerp(baseColor, cast, FastMath.clamp01(tint * TintModel.kTintBlend));
		}

		private static Color borderColor(Color border, Color neighbour, float strength)
		{
			if (strength <= 1f)
				return ColorMath.lerp(neighbour, border, strength);

			return ColorMath.shade(border, 1f / strength);
		}

		private static Color applyContrast(Color surface, float contrast)
		{
			if (contrast == 1f)
				return surface;

			return ColorMath.lerpUnclamped(Surfaces.m_base, surface, contrast);
		}

		private static Color surfaceColor(ThemeSlot slot, string hex, float tintHue, float tintSat, float tint, float brightness, float contrast) => Theme.slot(slot, Theme.applyContrast(Theme.stylize(hex, tintHue, tintSat, tint, brightness), contrast));

		private static Color slot(ThemeSlot slot, Color derived)
		{
			int index = (int)slot;
			if (!ThemeConfig.SlotOverride[index])
			{
				ThemeConfig.SlotColor[index] = derived;
				return derived;
			}
			return ColorPicker.resolve(ThemeSlots.pickerId(slot), ThemeConfig.SlotColor[index]);
		}

		private static void sizeSlots()
		{
			if (ThemeConfig.SlotOverride == null || ThemeConfig.SlotOverride.Length != ThemeSlots.kCount)
				Array.Resize(ref ThemeConfig.SlotOverride, ThemeSlots.kCount);

			if (!(ThemeConfig.SlotColor == null || ThemeConfig.SlotColor.Length != ThemeSlots.kCount))
				return;

			Array.Resize(ref ThemeConfig.SlotColor, ThemeSlots.kCount);
		}

		private static void buildSurfaces(float tintHue, float tintSat)
		{
			float tint = FastMath.clamp(ThemeConfig.SurfaceTint, ThemeConfig.Limits.kMinTint, ThemeConfig.Limits.kMaxTint);

			float contrast = FastMath.clamp(ThemeConfig.SurfaceContrast, ThemeConfig.Limits.kMinContrast, ThemeConfig.Limits.kMaxContrast);

			float brightness = FastMath.clamp(ThemeConfig.Brightness, ThemeConfig.Limits.kMinBrightness, ThemeConfig.Limits.kMaxBrightness);

			float borderStrength = FastMath.clamp(ThemeConfig.BorderStrength, ThemeConfig.Limits.kMinBorderStrength, ThemeConfig.Limits.kMaxBorderStrength);

			float textBrightness = FastMath.clamp(ThemeConfig.TextBrightness, ThemeConfig.Limits.kMinTextBrightness, ThemeConfig.Limits.kMaxTextBrightness);

			Surfaces.m_base = Theme.slot(ThemeSlot.Window, Theme.stylize(BaseHex.kBase, tintHue, tintSat, tint, brightness));
			Surfaces.m_mantle = Theme.surfaceColor(ThemeSlot.TitleBar, BaseHex.kMantle, tintHue, tintSat, tint, brightness, contrast);
			Surfaces.m_panel = Theme.surfaceColor(ThemeSlot.Panel, BaseHex.kPanel, tintHue, tintSat, tint, brightness, contrast);
			Surfaces.m_surface0 = Theme.surfaceColor(ThemeSlot.Container, BaseHex.kSurface0, tintHue, tintSat, tint, brightness, contrast);
			Surfaces.m_surface1 = Theme.surfaceColor(ThemeSlot.Widget, BaseHex.kSurface1, tintHue, tintSat, tint, brightness, contrast);
			Surfaces.m_surface2 = Theme.surfaceColor(ThemeSlot.WidgetHover, BaseHex.kSurface2, tintHue, tintSat, tint, brightness, contrast);
			Surfaces.m_overlay0 = Theme.surfaceColor(ThemeSlot.WidgetPressed, BaseHex.kOverlay0, tintHue, tintSat, tint, brightness, contrast);

			Color windowBorder = Theme.applyContrast(Theme.stylize(BaseHex.kBorderColor, tintHue, tintSat, tint, brightness), contrast);

			Color widgetBorder = Theme.applyContrast(Theme.stylize(BaseHex.kSubtleBorder, tintHue, tintSat, tint, brightness), contrast);

			Surfaces.m_borderColor = Theme.slot(ThemeSlot.WindowBorder, Theme.borderColor(windowBorder, Surfaces.m_base, borderStrength));
			Surfaces.m_subtleBorder = Theme.slot(ThemeSlot.WidgetBorder, Theme.borderColor(widgetBorder, Surfaces.m_surface1, borderStrength));

			Text.m_primary = Theme.slot(ThemeSlot.Text, Theme.stylize(BaseHex.kTextPrimary, tintHue, tintSat, tint * TintModel.kTextShare, textBrightness));
			Text.m_secondary = Theme.slot(ThemeSlot.MutedText, Theme.stylize(BaseHex.kTextSecondary, tintHue, tintSat, tint * TintModel.kTextShare, textBrightness));
		}

		private static void buildStatus()
		{
			Status.m_green = Theme.slot(ThemeSlot.Positive, ColorHex.parse(BaseHex.kPositive));
			Status.m_gold = Theme.slot(ThemeSlot.Warning, ColorHex.parse(BaseHex.kWarning));
			Status.m_red = Theme.slot(ThemeSlot.Danger, ColorHex.parse(BaseHex.kDanger));
			ColorMath.rgbToHsv(Status.m_red, out float redH, out float redS, out float redV);
			Status.m_redHover = ThemeConstants.kStatusHover.apply(redH, redS, redV);
		}

		private static void buildShape()
		{
			Theme.cornerR = FastMath.clamp(ThemeConfig.CornerRadius, ThemeConfig.Limits.kMinRadius, ThemeConfig.Limits.kMaxRadius);
			Theme.borderWidth = FastMath.clamp(ThemeConfig.BorderWidth, ThemeConfig.Limits.kMinBorderWidth, ThemeConfig.Limits.kMaxBorderWidth);
		}

		private static void buildChrome()
		{
			Color clear = Surfaces.kTransparent;

			Theme.kButtonChrome.set(Surfaces.m_surface1, Surfaces.m_surface2, Surfaces.m_surface2, Surfaces.m_overlay0, Surfaces.m_surface0, Surfaces.m_surface1);

			Theme.kDangerChrome.set(Status.m_red, Status.m_red, Status.m_redHover, Status.m_redHover, ColorMath.shade(Status.m_red, Theme.kPressShade), Status.m_red);

			Theme.kPopupChrome.set(Theme.m_accent, Theme.m_accentDim, Theme.m_accentHover, Theme.m_accent, Theme.m_accentDim, Theme.m_accentDim);

			Theme.kTabChrome.set(clear, clear, ColorMath.fade(Theme.m_accent, Theme.accentAlpha(ThemeConstants.kTabHover)), clear, Surfaces.m_surface2, Surfaces.m_overlay0);

			Theme.kActiveTab.set(Theme.m_tabFill, clear, Theme.m_tabHover, clear, ColorMath.fade(Theme.m_accent, Theme.accentAlpha(ThemeConstants.kAlphaDim)), clear);

			Theme.kSubTab.set(Surfaces.m_surface1, Surfaces.m_surface2, Surfaces.m_surface2, Surfaces.m_overlay0, Surfaces.m_overlay0, Surfaces.m_overlay0);

			Theme.kActiveSubTab.set(Theme.m_accentSubtle, clear, ColorMath.fade(Theme.m_accent, Theme.accentAlpha(ThemeConstants.kActiveHover)), clear, ColorMath.fade(Theme.m_accent, Theme.accentAlpha(ThemeConstants.kAlphaDim)), clear);

			Theme.kDropdownChrome.set(Surfaces.m_surface1, Surfaces.m_overlay0, Surfaces.m_surface2, Theme.m_accent, Surfaces.m_surface1, Theme.m_accentDim);

			// not activeTab since bolding reflows the label under the cursor
			Theme.kDropdownOpen.set(Surfaces.m_surface2, Theme.m_accent, Surfaces.m_surface2, Theme.m_accentHover, Surfaces.m_surface1, Theme.m_accentDim);

			// an armed keybind brightens on press since a darker press read as disarmed
			Theme.kKeybindCapture.set(Theme.captureFill(ThemeConstants.CaptureTint.kRest), Theme.m_accentHover, Theme.captureFill(ThemeConstants.CaptureTint.kHover), Theme.m_accentHover, Theme.captureFill(ThemeConstants.CaptureTint.kPress), Theme.m_accentHover);

			Theme.kFieldChrome.set(Surfaces.m_surface1, Surfaces.m_subtleBorder, Surfaces.m_surface2, Surfaces.m_overlay0, Surfaces.m_surface2, ColorMath.lerp(Surfaces.m_subtleBorder, Theme.m_accent, ThemeConstants.kFocusTint));

			Theme.kItemChrome.set(clear, clear, Theme.m_accent, Theme.m_accentDim, Theme.m_accent, Theme.m_accentDim);

			Theme.kCheckOff.set(Surfaces.m_surface1, Surfaces.m_subtleBorder, Surfaces.m_surface2, Surfaces.m_overlay0, Surfaces.m_surface2, Surfaces.m_overlay0);

			Color checkDim = Theme.shaded(Theme.m_checkColor, ThemeConstants.kDim);
			Theme.kCheckOn.set(Theme.m_checkColor, checkDim, Theme.shaded(Theme.m_checkColor, ThemeConstants.kHover), Theme.m_checkColor, Theme.m_checkColor, checkDim);

			// no frame since a 1 px ring on a circle this small reads as dark stairs
			Theme.kThumbChrome.set(Theme.m_sliderFill, clear, Theme.shaded(Theme.m_sliderFill, ThemeConstants.kHover), clear, Theme.m_sliderFill, clear);
		}

		private static void buildColors()
		{
			Theme.sizeSlots();
			Theme.m_accent = ColorPicker.resolve(Theme.kAccentPicker, Theme.AccentColor);
			ColorMath.rgbToHsv(Theme.m_accent, out float ah, out float aS, out float av);

			float tintH = ah;

			float tintS = aS;
			if (ThemeConfig.TintSource == SurfaceTintSource.Custom)
				ColorMath.rgbToHsv(ColorPicker.resolve(Theme.kTintPicker, ThemeConfig.TintColor), out tintH, out tintS, out float valueValue);
			Theme.buildSurfaces(tintH, tintS);
			Theme.buildStatus();
			Theme.m_accentHover = Theme.slot(ThemeSlot.AccentHover, ThemeConstants.kHover.apply(ah, aS, av));
			Theme.m_accentDim = Theme.slot(ThemeSlot.AccentPressed, ThemeConstants.kDim.apply(ah, aS, av));
			Theme.m_popupBorder = Theme.slot(ThemeSlot.PopupBorder, ColorMath.lerp(Theme.m_accent, Surfaces.m_borderColor, ThemeConstants.kBorderMix));
			Theme.m_sectionTitle = Theme.slot(ThemeSlot.HeaderTitle, ThemeConstants.kTitle.apply(ah, aS, av));
			Theme.m_accentFaint = ColorMath.fade(Theme.m_accent, Theme.accentAlpha(ThemeConstants.kAlphaDim));
			Theme.m_accentSubtle = ColorMath.fade(Theme.m_accent, Theme.accentAlpha(ThemeConstants.kAlphaSubtle));
			Theme.m_accentGlow = ColorMath.fade(Theme.m_accent, Theme.accentAlpha(ThemeConstants.kAlphaGlow));
			Theme.m_accentBright = ThemeConstants.kBright.apply(ah, aS, av);
			Theme.m_blockBorder = Theme.slot(ThemeSlot.ContainerBorder, ColorMath.lerp(Surfaces.m_subtleBorder, Theme.m_accent, ThemeConstants.kBorderTint));
			Theme.m_edgeHighlight = Theme.slot(ThemeSlot.EdgeHighlight, Theme.m_accent);
			Theme.m_checkColor = Theme.slot(ThemeSlot.CheckboxOn, Theme.m_accent);
			Theme.m_sliderFill = Theme.slot(ThemeSlot.SliderThumb, Theme.m_accent);
			Theme.m_trackFill = Theme.slot(ThemeSlot.SliderTrack, Surfaces.m_surface1);
			Theme.m_scrollFill = Theme.slot(ThemeSlot.ScrollThumb, Theme.m_accent);
			Theme.m_hoverText = Theme.slot(ThemeSlot.HoverText, ColorMath.hsvAtLuminance(ah, aS, HoverText.kTargetLuminance, 1f));
			Theme.m_tabFill = Theme.slot(ThemeSlot.TabActiveFill, ColorMath.lerp(Surfaces.m_surface1, Theme.m_accent, TabStyle.kFillMix));
			Theme.m_tabHover = ColorMath.lerp(Theme.m_tabFill, Theme.m_accent, TabStyle.kHoverLift);
			Theme.m_caretNormal = Text.m_secondary;
			Theme.m_caretHover = Text.m_primary;
			Theme.buildChrome();
			foreach (Action hook in Theme.kRebuildHooks)
				hook();
		}

		private static void initCore()
		{
			UiLog.debug("[Theme.initCore] start");

			Theme.m_correctionVersion = GraphicsInfo.correctionVersion;
			Theme.m_themeHash = ThemeConfig.bakeHash();
			Theme.buildColors();
			Theme.buildShape();

			Font font = UiFont.load();

			UiLog.debug("[Theme.initCore] stage colors+font done");

			Theme.titleAccent = Theme.createFlat(() => Theme.m_accentGlow);
			Theme.accentBar = Theme.createFlat(() => Theme.m_accentBright);
			Theme.accentFaint = Theme.createFlat(() => Theme.m_accentFaint);

			UiLog.debug("[Theme.initCore] stage box+flat textures done");

			Theme.edgeLineH = Theme.createEdgeLine(() => Theme.m_edgeHighlight, false);
			Theme.edgeLineV = Theme.createEdgeLine(() => Theme.m_edgeHighlight, true);
			Theme.scrollFadeTop = Theme.createEdgeFade(() => Theme.scrollBackdrop, true);
			Theme.scrollFadeBottom = Theme.createEdgeFade(() => Theme.scrollBackdrop, false);
			Theme.sliderTrack = Theme.createFlat(() => Theme.m_trackFill);
			Theme.scrollTrack = Theme.createFlat(() => Surfaces.m_mantle);
			Theme.scrollThumb = Theme.createFlat(Theme.scrollHover);
			Theme.thumbHover = Theme.createFlat(Theme.scrollGlow);
			Theme.bakeCarets();
			Theme.disclosureHover = Theme.createFlat(Theme.faintColor);

			UiLog.debug("[Theme.initCore] stage slider/scroll textures done");

			Theme.divider = Theme.createFlat(Theme.acuteColor);
			Theme.buildStyles(font);
			Theme.applyCursor();

			UiLog.debug("[Theme.initCore] stage styles done");

			Theme.prewarmGlyphs(font);
			Theme.m_initialized = true;
			Theme.m_refreshedFrame = Gui.frameCount;

			Theme.layoutVersion++;

			UiLog.debug("[Theme.initCore] complete");
		}

		private static Color scrollHover() => ColorMath.fade(Theme.m_scrollFill, ThemeConstants.kAlphaHover);
		private static Color scrollGlow() => ColorMath.fade(Theme.m_scrollFill, ThemeConstants.kAlphaGlow);
		private static Color faintColor() => ColorMath.fade(Theme.m_accent, Theme.accentAlpha(ThemeConstants.kAlphaFaint));
		private static Color acuteColor() => ColorMath.fade(Theme.m_accent, Theme.accentAlpha(ThemeConstants.kAlphaAcute));

		private static void buildStyles(Font font)
		{
			Theme.label = Theme.createStyle(font, FontSizes.kBody, () => Text.m_primary);
			Theme.label.richText = true;
			Theme.label.clipping = TextClipping.Clip;
			Theme.labelHover = Theme.recolor(Theme.label, () => Theme.m_hoverText);
			Theme.toggleLabel = Theme.createStyle(font, FontSizes.kBody, () => Text.m_primary);
			Theme.toggleLabel.richText = true;
			Theme.toggleLabel.clipping = TextClipping.Clip;
			Theme.toggleLabel.alignment = TextAnchor.MiddleLeft;
			Theme.toggleLabelHover = Theme.recolor(Theme.toggleLabel, () => Theme.m_hoverText);
			Theme.toggleLabelWrap = Theme.createStyle(font, FontSizes.kBody, () => Text.m_primary);
			Theme.toggleLabelWrap.richText = true;
			Theme.toggleLabelWrap.wordWrap = true;
			Theme.toggleLabelWrap.alignment = TextAnchor.UpperLeft;
			Theme.toggleLabelWrapHover = Theme.recolor(Theme.toggleLabelWrap, () => Theme.m_hoverText);
			Theme.title = Theme.createStyle(font, FontSizes.kTitle, () => Text.m_primary);
			Theme.title.fontStyle = FontStyle.Bold;
			Theme.title.richText = true;
			Theme.title.clipping = TextClipping.Clip;
			Theme.statusLabel = Theme.createStyle(font, FontSizes.kBody, () => Theme.m_accent);
			Theme.statusLabel.richText = true;
			Theme.statusLabel.clipping = TextClipping.Clip;
			Theme.button = Theme.createStyle(font, FontSizes.kBody, () => Text.m_primary);
			Theme.button.alignment = TextAnchor.MiddleCenter;
			Theme.button.padding = new RectOffset(Theme.kPadLeft, Theme.kPadRight, Theme.kPadTop, Theme.kPadBottom);
			Theme.kButtonChrome.m_style = Theme.button;
			Theme.dangerButton = Theme.recolor(Theme.button, () => Text.m_primary);
			Theme.dangerButton.fontStyle = FontStyle.Bold;
			Theme.kDangerChrome.m_style = Theme.dangerButton;
			Theme.tab = Theme.createStyle(font, FontSizes.kCompact, () => Text.m_secondary);
			Theme.tab.alignment = TextAnchor.MiddleLeft;
			Theme.tab.richText = true;
			Theme.tab.padding = new RectOffset(Theme.kTabPadLeft, Theme.kTabPadRight, Theme.kPadTop, Theme.kPadBottom);
			Theme.kTabChrome.m_style = Theme.tab;
			Theme.activeTab = Theme.recolor(Theme.tab, () => Text.m_primary);
			Theme.activeTab.fontStyle = FontStyle.Bold;
			Theme.kActiveTab.m_style = Theme.activeTab;
			Theme.subTab = Theme.createStyle(font, FontSizes.kCompact, () => Text.m_secondary);
			Theme.subTab.alignment = TextAnchor.MiddleCenter;
			Theme.subTab.padding = new RectOffset(Theme.kSubTabPadLeft, Theme.kSubTabPadRight, Theme.kPadTop, Theme.kPadBottom);
			Theme.kSubTab.m_style = Theme.subTab;
			Theme.activeSubTab = Theme.recolor(Theme.subTab, () => Text.m_primary);
			Theme.activeSubTab.fontStyle = FontStyle.Bold;
			Theme.kActiveSubTab.m_style = Theme.activeSubTab;
			Theme.dropdownButton = Theme.createStyle(font, FontSizes.kCompact, () => Text.m_primary);
			Theme.dropdownButton.alignment = TextAnchor.MiddleLeft;
			Theme.dropdownButton.padding = new RectOffset(Theme.kDropdownPadLeft, Theme.kDropdownPadRight, Theme.kPadTop, Theme.kPadBottom);
			Theme.dropdownButton.clipping = TextClipping.Clip;
			Theme.kDropdownChrome.m_style = Theme.dropdownButton;
			Theme.dropdownOpen = Theme.recolor(Theme.dropdownButton, () => Text.m_primary);
			Theme.kDropdownOpen.m_style = Theme.dropdownOpen;
			Theme.keybindCapture = Theme.recolor(Theme.dropdownButton, () => Text.m_primary);
			Theme.kKeybindCapture.m_style = Theme.keybindCapture;
			Theme.dropdownItem = Theme.createStyle(font, FontSizes.kCompact, () => Text.m_primary);
			Theme.dropdownItem.alignment = TextAnchor.MiddleLeft;
			Theme.dropdownItem.padding = new RectOffset(Theme.kItemPadLeft, Theme.kItemPadRight, Theme.kPadTop, Theme.kPadBottom);
			Theme.sectionHeader = Theme.createStyle(font, FontSizes.kBody, () => Theme.m_sectionTitle);
			Theme.sectionHeader.fontStyle = FontStyle.Bold;
			Theme.sectionHeader.richText = true;
			Theme.sectionHeader.clipping = TextClipping.Clip;
			Theme.sectionHeader.alignment = TextAnchor.MiddleLeft;
			Theme.sectionHeader.padding = new RectOffset(Theme.kSectionPadLeft, 0, 0, 0);
			// two styles because GUI.color can only darken text
			Theme.tacticalHeader = Theme.createStyle(font, FontSizes.kCompact, () => Theme.m_sectionTitle);
			Theme.tacticalHeader.fontStyle = FontStyle.Bold;
			Theme.tacticalHeader.richText = true;
			Theme.tacticalHeader.clipping = TextClipping.Clip;
			Theme.tacticalHeader.alignment = TextAnchor.MiddleLeft;
			Theme.tacticalHover = Theme.recolor(Theme.tacticalHeader, () => Theme.m_accentBright);
			Theme.valueLabel = Theme.createStyle(font, FontSizes.kBody, () => Text.m_secondary);
			Theme.valueLabel.richText = true;
			Theme.valueLabel.clipping = TextClipping.Clip;
			Theme.badgeLabel = Theme.createStyle(font, FontSizes.kSmall, () => Text.m_primary);
			Theme.badgeLabel.fontStyle = FontStyle.Bold;
			Theme.badgeLabel.richText = true;
			Theme.badgeLabel.clipping = TextClipping.Clip;
			Theme.badgeLabel.alignment = TextAnchor.MiddleCenter;
			Theme.espLabel = Theme.createStyle(font, FontSizes.kCompact, () => Text.m_primary);
			Theme.espLabel.alignment = TextAnchor.UpperCenter;
			Theme.espLabel.richText = true;
			Theme.espShadow = Theme.recolor(Theme.espLabel, () => ColorMath.kBlack);
			Theme.espShadow.font = ShadowFont.of(font);
			Theme.textField = Theme.createStyle(font, FontSizes.kCompact, () => Text.m_primary);
			Theme.textField.alignment = TextAnchor.MiddleLeft;
			Theme.textField.padding = new RectOffset(Theme.kFieldPadLeft, Theme.kFieldPadRight, Theme.kFieldPadTop, Theme.kFieldPadBottom);
			Theme.textField.clipping = TextClipping.Clip;
			Theme.textField.richText = false;
			Theme.kFieldChrome.m_style = Theme.textField;
			Theme.textArea = Theme.recolor(Theme.textField, () => Text.m_primary);
			Theme.textArea.alignment = TextAnchor.UpperLeft;
			Theme.tooltipLabel = Theme.createStyle(font, FontSizes.kSmall, () => Text.m_primary);
			Theme.tooltipLabel.wordWrap = true;
			Theme.tooltipPreview = Theme.createStyle(font, FontSizes.kCompact, () => Text.m_primary);
			Theme.tooltipPreview.richText = true;
			Theme.tooltipPreview.wordWrap = true;
			Theme.popupLabel = Theme.createStyle(font, FontSizes.kPopup, () => Text.m_primary);
			Theme.popupLabel.alignment = TextAnchor.MiddleCenter;
			Theme.popupLabel.wordWrap = true;
			Theme.popupLabel.richText = true;
			Theme.popupButton = Theme.createStyle(font, FontSizes.kBody, () => Text.m_primary);
			Theme.popupButton.alignment = TextAnchor.MiddleCenter;
			Theme.popupButton.fontStyle = FontStyle.Bold;
			Theme.popupButton.padding = new RectOffset(Theme.kPopupPadLeft, Theme.kPopupPadRight, Theme.kPopupPadTop, Theme.kPopupPadBottom);
			Theme.kPopupChrome.m_style = Theme.popupButton;
			Theme.notifHeader = Theme.createStyle(font, FontSizes.kCompact, () => Text.m_primary);
			Theme.notifHeader.fontStyle = FontStyle.Bold;
			Theme.notifHeader.clipping = TextClipping.Clip;
			Theme.notifHeader.wordWrap = true;
			Theme.notifHeader.richText = false;
			Theme.notifLabel = Theme.createStyle(font, FontSizes.kCompact, () => Text.m_primary);
			Theme.notifLabel.wordWrap = true;
			Theme.notifLabel.clipping = TextClipping.Clip;
			Theme.notifLabel.richText = true;
			Theme.logLabel = Theme.createStyle(font, FontSizes.kSmall, () => Text.m_primary);
			Theme.logLabel.wordWrap = true;
			Theme.logLabel.richText = false;
			Theme.logBold = Theme.recolor(Theme.logLabel, () => Text.m_primary);
			Theme.logBold.fontStyle = FontStyle.Bold;
			Theme.codeLabel = Theme.createStyle(font, FontSizes.kSmall, () => Text.m_primary);
			Theme.codeLabel.wordWrap = false;
			Theme.codeLabel.richText = true;
			Theme.codeLabel.clipping = TextClipping.Clip;
			Theme.codeGutter = Theme.createStyle(font, FontSizes.kSmall, Theme.secondaryEdge);
			Theme.codeGutter.wordWrap = false;
			Theme.codeGutter.richText = false;
			Theme.codeGutter.alignment = TextAnchor.MiddleRight;
			Theme.codeField = Theme.createStyle(font, FontSizes.kSmall, () => Text.m_primary);
			Theme.codeField.alignment = TextAnchor.MiddleLeft;
			Theme.codeField.padding = new RectOffset(Theme.kCodePadLeft, Theme.kCodePadRight, 0, 0);
			Theme.codeField.clipping = TextClipping.Clip;
			Theme.codeField.richText = false;
		}

		private static Color secondaryEdge() => ColorMath.fade(Text.m_secondary, ThemeConstants.kAlphaEdge);

		private static void prewarmGlyphs(Font font)
		{
			if (Live.unityEquals(Theme.m_prewarmedFont, font))
				return;

			Stopwatch timer = Stopwatch.StartNew();

			Theme.m_prewarmedFont = font;

			foreach (int prewarmFontSize in Theme.kPrewarmSizes)
				FontGlyphCache.requestBoth(font, Theme.kPrewarmChars, prewarmFontSize);

			UiLog.debug($"[Theme.initCore] stage glyph prewarm done ({Theme.kPrewarmSizes.Length} sizes, {timer.ElapsedMilliseconds}ms)");
		}

		private static void bakeCarets()
		{
			if (Theme.m_caretFrames != null && Theme.m_caretHoverFrames != null)
				return;

			Theme.m_caretFrames = new Texture2D[Caret.kFrames];
			Theme.m_caretHoverFrames = new Texture2D[Caret.kFrames];

			float step = Caret.kSweepDegrees / (Caret.kFrames - 1);
			for (int i = 0; i < Caret.kFrames; i++)
			{
				float[] coverage = Theme.bakeCoverage(i * step);
				Theme.m_caretFrames[i] = ThemeTint.create(Caret.kRes, Caret.kRes, coverage, () => Theme.m_caretNormal, FilterMode.Bilinear, true);
				Theme.m_caretHoverFrames[i] = ThemeTint.create(Caret.kRes, Caret.kRes, coverage, () => Theme.m_caretHover, FilterMode.Bilinear, true);

				Theme.kCaretTextures.Add(Theme.m_caretFrames[i]);
				Theme.kCaretTextures.Add(Theme.m_caretHoverFrames[i]);
			}
		}

		private static float bandBottom(in BoxMetrics box, float bandH) => RectMath.yMax(Gui.snapRect(RectMath.create(RectMath.x(box.m_outer), RectMath.y(box.m_outer), RectMath.w(box.m_outer), bandH)));
		private static void drawBandFill(in BoxMetrics box, float bottom, Color fill) => Theme.drawRegion(box, RectMath.minMax(RectMath.x(box.m_outer), RectMath.y(box.m_outer), RectMath.xMax(box.m_outer), bottom), fill);
		private static void drawRegion(in BoxMetrics box, Rect region, Color fill) => Theme.drawRegion(box, region, Gui.whiteTexture, fill);

		private static void drawRegion(in BoxMetrics box, Rect region, Texture texture, Color tint)
		{
			if (tint.a <= 0f)
				return;

			Theme.fillShape(box, box.m_border > 0f, out Rect fillRect, out float fillRadius);

			Rect snapped = Gui.snapRect(region);

			Rect inner = box.inner;

			float reach = Gui.devicePixel * 0.5f;

			float x0 = RectMath.x(snapped) <= RectMath.x(inner) + reach ? RectMath.x(fillRect) : RectMath.x(snapped);

			float y0 = RectMath.y(snapped) <= RectMath.y(inner) + reach ? RectMath.y(fillRect) : RectMath.y(snapped);

			float x1 = RectMath.xMax(snapped) >= RectMath.xMax(inner) - reach ? RectMath.xMax(fillRect) : RectMath.xMax(snapped);

			float y1 = RectMath.yMax(snapped) >= RectMath.yMax(inner) - reach ? RectMath.yMax(fillRect) : RectMath.yMax(snapped);
			if (x1 <= x0 || y1 <= y0)
				return;

			float left = x0 - RectMath.x(fillRect);

			float top = y0 - RectMath.y(fillRect);

			float right = RectMath.xMax(fillRect) - x1;

			float bottom = RectMath.yMax(fillRect) - y1;

			float cap = FastMath.min(x1 - x0, y1 - y0) * 0.5f;

			Vector4 radii = VectorMath.v4(LayoutMath.nestedRadius(fillRadius, left, top, cap), LayoutMath.nestedRadius(fillRadius, right, top, cap), LayoutMath.nestedRadius(fillRadius, right, bottom, cap), LayoutMath.nestedRadius(fillRadius, left, bottom, cap));
			Gui.drawRoundedTexture(RectMath.minMax(x0, y0, x1, y1), texture, ScaleMode.StretchToFill, ColorMath.modulate(tint, Gui.color), VectorMath.v4(0f, 0f, 0f, 0f), radii);
		}

		private static Rect bandRule(in BoxMetrics box, float bottom, float thickness)
		{
			float snappedThickness = Gui.snapThickness(thickness);

			float top = bottom - snappedThickness;

			float inset = top >= RectMath.y(box.m_outer) + box.m_radius ? box.m_border : FastMath.max(box.m_border, box.m_radius);
			return RectMath.minMax(RectMath.x(box.m_outer) + inset, top, RectMath.xMax(box.m_outer) - inset, bottom);
		}

		private static void drawTopAccent(Rect boxRect, Color color, float insetX, float thickness)
		{
			if (!Theme.tryWindowBox(boxRect, out BoxMetrics box))
				return;

			Gui.drawStroke(Theme.accentRect(box, insetX, thickness), color);
		}

		private static Rect accentRect(in BoxMetrics box, float insetX, float thickness)
		{
			float inset = FastMath.max(insetX, box.m_radius);

			float snappedThickness = Gui.snapThickness(thickness);
			return RectMath.minMax(RectMath.x(box.m_outer) + inset, RectMath.y(box.m_outer), RectMath.xMax(box.m_outer) - inset, RectMath.y(box.m_outer) + snappedThickness);
		}

		private static void drawChromeState(Rect rect, WidgetChrome chrome, float hoverT, bool active, float radius)
		{
			if (!Gui.repainting)
				return;

			if (active)
			{
				Theme.drawRoundedBox(rect, chrome.m_activeFill, chrome.m_activeBorder, radius, Theme.borderWidth);
				return;
			}

			Color fill = ColorMath.lerpVisible(chrome.m_normalFill, chrome.m_hoverFill, hoverT);

			Color border = ColorMath.lerpVisible(chrome.m_normalBorder, chrome.m_hoverBorder, hoverT);
			Theme.drawRoundedBox(rect, fill, border, radius, Theme.borderWidth);
		}

		private static void drawBoxFill(in BoxMetrics box, Color fill, bool framed)
		{
			Theme.fillShape(box, framed, out Rect fillRect, out float fillRadius);
			Theme.drawRoundedShape(fillRect, fill, VectorMath.v4(0f, 0f, 0f, 0f), Theme.uniformRadii(fillRadius));
		}

		private static void fillShape(in BoxMetrics box, bool framed, out Rect rect, out float radius)
		{
			if (!framed)
			{
				rect = box.m_outer;

				radius = box.m_radius;
				return;
			}

			if (Gui.color.a < BoxFill.kFadedAlpha)
			{
				rect = box.inner;

				radius = box.m_radius > 0f ? FastMath.max(0f, box.innerRadius - BoxFill.kRadiusTrim * Gui.devicePixel) : 0f;
				return;
			}

			if (box.m_radius <= 0f)
			{
				rect = box.m_outer;

				radius = 0f;
				return;
			}

			float inset = BoxFill.kOpaqueInset * Gui.devicePixel;
			rect = RectMath.inset(box.m_outer, inset);

			radius = FastMath.max(0f, box.m_radius - inset);
		}

		private static void drawRoundedFrame(Rect rect, Color color, float borderWidth, float radius) => Theme.drawRoundedShape(rect, color, VectorMath.v4(borderWidth, borderWidth, borderWidth, borderWidth), Theme.uniformRadii(radius));
		// the colour overload of DrawTexture never reads GUI.color
		private static void drawRoundedShape(Rect rect, Color color, Vector4 borderWidths, Vector4 radii) => Gui.drawRoundedTexture(rect, ColorMath.modulate(color, Gui.color), borderWidths, radii);
		private static Vector4 uniformRadii(float radius) => VectorMath.v4(radius, radius, radius, radius);

		private static GUIStyle createStyle(Font font, int size, Func<Color> textColor)
		{
			GUIStyle style = new GUIStyle();
			if (Live.isUnityAlive(font))
				style.font = font;
			style.fontSize = size;
			return Theme.bindText(style, textColor);
		}

		private static Color captureFill(float accentShare) => ColorMath.lerp(Surfaces.m_surface2, Theme.m_accent, accentShare);

		private static GUIStyle recolor(GUIStyle source, Func<Color> textColor)
		{
			GUIStyle style = new GUIStyle(source);
			return Theme.bindText(style, textColor);
		}

		private static GUIStyle bindText(GUIStyle style, Func<Color> textColor)
		{
			Color color = textColor();
			Theme.applyStyleText(style, color);

			Theme.kStyleTexts.Add(new StyleText
			{
				m_style = style,
				m_source = textColor,
				m_applied = color
			});
			return style;
		}

		private static Texture2D createHorizontalFade(Func<Color> source, int size)
		{
			float[] alphaWeight = new float[size];

			int last = size - 1;
			for (int i = 0; i < size; i++)
				alphaWeight[i] = GraphicsInfo.fadeAlpha(EasingMath.cubicFalloff((float)i / last));

			Texture2D texture = ThemeTint.create(size, 1, alphaWeight, source, FilterMode.Bilinear);
			Theme.kTrackedTextures.Add(texture);
			return texture;
		}

		private static Texture2D createEdgeFade(Func<Color> source, bool opaqueTop)
		{
			float[] alphaWeight = new float[EdgeFade.kWidth * EdgeFade.kHeight];

			int lastRow = EdgeFade.kHeight - 1;
			for (int y = 0; y < EdgeFade.kHeight; y++)
			{
				float fraction = (float)y / lastRow;

				float band = GraphicsInfo.fadeAlpha(Theme.edgeWeight(opaqueTop ? 1f - fraction : fraction));
				for (int x = 0; x < EdgeFade.kWidth; x++)
					alphaWeight[y * EdgeFade.kWidth + x] = band * Theme.endTaper(x);
			}

			Texture2D texture = ThemeTint.create(EdgeFade.kWidth, EdgeFade.kHeight, alphaWeight, source, FilterMode.Bilinear);
			Theme.kTrackedTextures.Add(texture);
			return texture;
		}

		internal static float edgeWeight(float distance) => 1f - EasingMath.smoothStep(EasingMath.normalize(distance, EdgeFade.kCoreFraction, 1f));

		private static float endTaper(int x)
		{
			int edge = FastMath.min(x, EdgeFade.kWidth - 1 - x);
			if (edge >= EdgeFade.kEndTexels)
				return 1f;

			return EasingMath.smoothStep((edge + 0.5f) / EdgeFade.kEndTexels);
		}

		private static float lineTaper(int index)
		{
			int fromEnd = FastMath.min(index, EdgeLine.kLength - 1 - index);
			if (fromEnd >= EdgeLine.kCapTexels)
				return 1f;

			return EasingMath.smoothStep((fromEnd + 0.5f) / EdgeLine.kCapTexels);
		}

		private static Texture2D createEdgeLine(Func<Color> source, bool vertical)
		{
			int width = vertical ? 1 : EdgeLine.kLength;

			int height = vertical ? EdgeLine.kLength : 1;

			float[] alphaWeight = new float[EdgeLine.kLength];
			for (int i = 0; i < EdgeLine.kLength; i++)
				alphaWeight[i] = GraphicsInfo.fadeAlpha(Theme.lineTaper(i));

			Texture2D texture = ThemeTint.create(width, height, alphaWeight, source, FilterMode.Bilinear);
			Theme.kTrackedTextures.Add(texture);
			return texture;
		}

		private static void drawThreeSlice(Rect rect, Texture2D texture, bool vertical, float capLength, float capFraction)
		{
			float x0 = RectMath.x(rect);

			float y0 = RectMath.y(rect);

			float x1 = RectMath.xMax(rect);

			float y1 = RectMath.yMax(rect);

			float cap = FastMath.min(capLength, (vertical ? y1 - y0 : x1 - x0) * 0.5f);

			float midFraction = 1f - capFraction * 2f;
			// texture rows run bottom up
			if (vertical)
			{
				Theme.drawSlice(RectMath.minMax(x0, y0, x1, y0 + cap), texture, RectMath.create(0f, 1f - capFraction, 1f, capFraction));
				Theme.drawSlice(RectMath.minMax(x0, y0 + cap, x1, y1 - cap), texture, RectMath.create(0f, capFraction, 1f, midFraction));
				Theme.drawSlice(RectMath.minMax(x0, y1 - cap, x1, y1), texture, RectMath.create(0f, 0f, 1f, capFraction));
				return;
			}

			Theme.drawSlice(RectMath.minMax(x0, y0, x0 + cap, y1), texture, RectMath.create(0f, 0f, capFraction, 1f));
			Theme.drawSlice(RectMath.minMax(x0 + cap, y0, x1 - cap, y1), texture, RectMath.create(capFraction, 0f, midFraction, 1f));
			Theme.drawSlice(RectMath.minMax(x1 - cap, y0, x1, y1), texture, RectMath.create(1f - capFraction, 0f, capFraction, 1f));
		}

		private static void drawSlice(Rect rect, Texture2D texture, Rect uv)
		{
			Rect snapped = Gui.snapRect(rect);
			if (!(RectMath.w(snapped) > 0f && RectMath.h(snapped) > 0f))
				return;

			Gui.drawTextureUv(snapped, texture, uv);
		}

		private static float[] bakeCoverage(float degrees)
		{
			int res = Caret.kRes;

			float inset = res * Caret.kInset;

			float left = inset;

			float right = res - inset;

			float mid = res * 0.5f;

			float radians = degrees * FastMath.kDeg2Rad;

			float cos = FastMath.cos(radians);

			float sin = FastMath.sin(radians);

			float[] coverage = new float[res * res];
			for (int y = 0; y < res; y++)
			{
				for (int x = 0; x < res; x++)
				{
					float dx = x + 0.5f - mid;

					float dy = res - (y + 0.5f) - mid;

					float px = mid + cos * dx + sin * dy;

					float py = mid - sin * dx + cos * dy;

					float along = (px - left) / (right - left);
					if (along < 0f || along > 1f)
						continue;

					float halfSpan = (1f - along) * (mid - inset);

					float spanDistance = FastMath.abs(py - mid);
					coverage[y * res + x] = FastMath.clamp01((halfSpan - spanDistance) / Caret.kEdgeSoftness);
				}
			}
			return coverage;
		}

		private static Texture2D createFlat(Func<Color> source)
		{
			float[] alphaWeight = new[] { 1f, 1f, 1f, 1f };

			Texture2D texture = ThemeTint.create(Theme.kFlatSize, Theme.kFlatSize, alphaWeight, source, FilterMode.Bilinear);
			Theme.kTrackedTextures.Add(texture);
			return texture;
		}
	}
}
