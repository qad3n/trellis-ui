using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Trellis
{
	internal static class ColorPicker
	{
		private const float kDragEpsilon = 0.0001f;
		private const string kSpeedLabel = "<b>Speed</b>";
		private const float kAlphaW = Layout.kSvSize + Layout.kHueGap + Layout.kHueWidth + Layout.kPreviewGap + Layout.kPreviewSize;
		private const float kPopupW = Layout.kPopupPad * 2 + Layout.kSvSize + Layout.kHueGap + Layout.kHueWidth + Layout.kPreviewGap + Layout.kPreviewSize;
		private const float kBaseH = Layout.kPopupPad * 2 + Layout.kSvSize + Layout.kAlphaGap + Layout.kAlphaH + Layout.kHexGap + Layout.kHexRowH + Layout.kRainbowGap + Layout.kRainbowRowH;
		private const float kSpeedExtra = Layout.kSpeedGap + Layout.kSpeedRowH;

		private static readonly Color kClearBlack = ColorMath.rgba(0f, 0f, 0f, 0f);
		private static readonly Color kClearWhite = ColorMath.rgba(1f, 1f, 1f, 0f);
		private static readonly Dictionary<string, PickerState> kStates = new Dictionary<string, PickerState>();
		private static readonly Dictionary<string, bool> kRainbowEnabled = new Dictionary<string, bool>();
		private static readonly Dictionary<string, float> kRainbowSpeeds = new Dictionary<string, float>();
		private static readonly Dictionary<string, Color> kResolveCache = new Dictionary<string, Color>();
		private static readonly Regex kEntryRegex = new Regex(@"(?<id>[^=;:]+)=(?<flag>[01]):(?<speed>[^;]+)", RegexOptions.Compiled);

		private static int m_cacheFrame = -1;

		[Config]
		private static string rainbowState = string.Empty;

		private static Texture2D m_hueTexture;
		private static Texture2D m_checkerTexture;
		private static Texture2D m_satOverlay;
		private static Texture2D m_valueOverlay;
		private static Texture2D m_alphaOverlay;
		private static string m_activeId;
		private static string m_activeAnim;
		private static string m_fadingId;
		private static Rect m_anchorRect;
		private static Vector2 m_blockOffset;
		private static bool m_popupEntered;
		private static bool m_justOpened;
		private static bool m_dragSv;
		private static bool m_dragHue;
		private static bool m_dragAlpha;
		private static bool m_dragSpeed;
		private static Rect m_lastRect;

		private static class Layout
		{
			internal const float kPopupPad = 12f;
			internal const float kSvSize = 120f;
			internal const float kHueWidth = 14f;
			internal const float kHueGap = 8f;
			internal const float kAlphaGap = 8f;
			internal const float kAlphaH = 14f;
			internal const float kPreviewSize = 40f;
			internal const float kPreviewGap = 14f;
			internal const float kHexRowH = 20f;
			internal const float kHexGap = 8f;
			internal const float kRainbowRowH = 22f;
			internal const float kRainbowGap = 8f;
			internal const float kCheckSize = 16f;
			internal const float kCheckGap = 8f;
			internal const float kSpeedRowH = 18f;
			internal const float kSpeedGap = 6f;
			internal const float kSpeedLabelW = 56f;
			internal const float kSpeedValueW = 48f;
			internal const float kSwatchSize = 22f;
			// toggle rows draw their content one pixel down
			internal const float kSwatchNudgeY = 1f;
			internal const float kLabelW = 110f;
			// same pitch as a toggle row so stacked swatches keep the gap under a toggle swatch
			internal const float kRowH = 30f;
			internal const float kHashW = 14f;
			internal const float kSwatchGap = 8f;
			internal const float kInlineMinX = 120f;
			internal const float kHitExtra = 84f;
			internal const float kAnchorGap = 6f;
			internal const float kTrackGap = 4f;
		}

		private static class CursorShape
		{
			internal const float kCrossCore = 1f;
			internal const float kCoreArm = 4f;
			internal const float kHaloArm = 5f;
			internal const float kBarCore = 2f;
			internal const float kBarInset = 2f;
		}

		private static class Rainbow
		{
			internal const float kMinSpeed = 0.1f;
			internal const float kMaxSpeed = 5f;
			internal const float kDefaultSpeed = 1f;
		}

		private static class TextureSizes
		{
			internal const int kHue = 128;
			internal const int kGradientSteps = 128;
			internal const int kChecker = 64;
			internal const int kCheckerCell = 4;
			internal const int kStripWidth = 1;
			internal const int kStripHeight = 1;
		}

		private static class CheckerColors
		{
			internal const int kParity = 2;

			internal static readonly Color kLight = ColorMath.fromBytes(102, 102, 102, 255);
			internal static readonly Color kDark = ColorMath.fromBytes(64, 64, 64, 255);
		}

		private static class HueSweep
		{
			internal const float kSaturation = 1f;
			internal const float kValue = 1f;
		}

		private static class HexField
		{
			internal const int kRgbLength = 6;
			internal const int kRgbaLength = 8;
		}

		private sealed class PickerState
		{
			internal readonly string m_hexControl;
			internal readonly string m_rainbowCheck;
			internal readonly string m_speedSlider;
			internal float m_hue;
			internal float m_sat;
			internal float m_value;
			internal float m_alpha = 1f;
			internal Color m_lastColor;
			internal string m_hexInput = string.Empty;
			internal string m_cachedHex = string.Empty;
			internal Color m_hexColor;

			internal PickerState(string id)
			{
				m_hexControl = $"cp_hex_{id}";
				m_rainbowCheck = $"cp_rainbow:{id}";
				m_speedSlider = $"cpspd:{id}";
			}
		}

		private readonly struct PickerRects
		{
			internal readonly Rect m_sv;
			internal readonly Rect m_hue;
			internal readonly Rect m_alpha;
			internal readonly Rect m_preview;

			internal PickerRects(Rect popupRect)
			{
				m_sv = RectMath.create(RectMath.x(popupRect) + Layout.kPopupPad, RectMath.y(popupRect) + Layout.kPopupPad, Layout.kSvSize, Layout.kSvSize);
				m_hue = RectMath.create(RectMath.xMax(m_sv) + Layout.kHueGap, RectMath.y(m_sv), Layout.kHueWidth, Layout.kSvSize);
				m_alpha = RectMath.create(RectMath.x(m_sv), RectMath.yMax(m_sv) + Layout.kAlphaGap, ColorPicker.kAlphaW, Layout.kAlphaH);
				m_preview = RectMath.create(RectMath.xMax(m_hue) + Layout.kPreviewGap, RectMath.y(m_sv), Layout.kPreviewSize, Layout.kSvSize);
			}
		}

		internal static bool popupOpen => ColorPicker.m_activeId != null;
		internal static string activeAnim => ColorPicker.m_activeAnim;
		// inline swatches cant rely on buildTextures having run
		private static Texture2D checkerTexture => ColorPicker.m_checkerTexture ??= ColorPicker.buildChecker();
		internal static bool popupContains(Vector2 point) => ColorPicker.m_activeId != null && RectMath.contains(ColorPicker.m_lastRect, point);

		// bulk writers must clear this or the cycling hue overrides them
		internal static void clearRainbow(string id)
		{
			if (id == null)
				return;

			ColorPicker.kRainbowEnabled.Remove(id);

			ColorPicker.kRainbowSpeeds.Remove(id);
		}

		// a colour that moved to a new picker id keeps its saved rainbow
		internal static void renameRainbow(string from, string to) => ColorPicker.rainbowState = ColorPicker.kEntryRegex.Replace(ColorPicker.rainbowState ?? string.Empty, match => match.Groups["id"].Value == from ? to + match.Value.Substring(from.Length) : match.Value);

		internal static string captureRainbow(string[] ids)
		{
			StringBuilder text = new StringBuilder();
			foreach (string id in ids)
			{
				if (!ColorPicker.rainbowOn(id))
					continue;

				ColorPicker.appendEntry(text, id, true, ColorPicker.savedSpeed(id));
			}
			return text.ToString();
		}

		internal static void restoreRainbow(string[] ids, string state)
		{
			foreach (string id in ids)
				ColorPicker.clearRainbow(id);

			ColorPicker.parseState(state, ids);
		}

		internal static Color resolve(string id, Color stored, bool active) => active ? ColorPicker.resolve(id, stored) : stored;

		internal static Color resolve(string id, Color stored)
		{
			if (!ColorPicker.rainbowOn(id))
				return stored;

			ColorPicker.updateCache();

			if (ColorPicker.kResolveCache.TryGetValue(id, out Color cached) && cached.a == stored.a)
				return cached;

			float speed = ColorPicker.savedSpeed(id);

			float hue = EasingMath.rainbowHue(Gui.unscaledTime, speed);

			Color rainbow = ColorMath.hsvToColor(hue, HueSweep.kSaturation, HueSweep.kValue, stored.a);
			ColorPicker.kResolveCache[id] = rainbow;
			return rainbow;
		}

		internal static Color resolveQuantized(string id, Color stored, float hueStep)
		{
			if (!ColorPicker.rainbowOn(id))
				return stored;

			float speed = ColorPicker.savedSpeed(id);

			float hue = EasingMath.rainbowHue(Gui.unscaledTime, speed);

			hue = FastMath.floor(hue / hueStep) * hueStep;
			return ColorMath.hsvToColor(hue, HueSweep.kSaturation, HueSweep.kValue, stored.a);
		}

		internal static void onConfigLoaded()
		{
			ColorPicker.kRainbowEnabled.Clear();

			ColorPicker.kRainbowSpeeds.Clear();

			ColorPicker.parseState(ColorPicker.rainbowState, null);
		}

		internal static void onBeforeConfigSave()
		{
			StringBuilder text = new StringBuilder();
			foreach (KeyValuePair<string, bool> pair in ColorPicker.kRainbowEnabled)
			{
				ColorPicker.appendEntry(text, pair.Key, pair.Value, ColorPicker.clampedSpeed(pair.Key));
			}

			ColorPicker.rainbowState = text.ToString();
		}

		internal static void drawRow(string id, ref float y, float w, string label, ref Color color)
		{
			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, Widgets.RowPrefix.kColor, label);

			ColorPicker.syncState(id, ref color);

			float offsetX = MenuHost.m_blockOffset.x;

			float offsetY = MenuHost.m_blockOffset.y;

			float swatchX = w - MenuHost.kSwatchW;

			float labelW = FastMath.max(Layout.kLabelW, swatchX - Layout.kSwatchGap);
			float swatchY = y + Layout.kSwatchNudgeY;
			UiText.fittedLabel(RectMath.create(offsetX, offsetY + swatchY, labelW, Layout.kSwatchSize), UiText.bold(label), Theme.label, TextAnchor.MiddleLeft);

			Rect drawRect = RectMath.create(offsetX + swatchX, offsetY + swatchY, Layout.kSwatchSize, Layout.kSwatchSize);
			ColorPicker.drawSwatch(drawRect, ColorPicker.resolve(id, color));

			if (Gui.enabled && RectMath.contains(drawRect, MenuHost.scrollMouse) && MenuHost.mouseDown0)
				ColorPicker.openPopup(id, RectMath.create(swatchX, swatchY, Layout.kSwatchSize, Layout.kSwatchSize));

			y += Layout.kRowH;
			Widgets.endRow(ref y, ref row);
		}

		private static void syncState(string id, ref Color color)
		{
			PickerState state = ColorPicker.stateFor(id, color);
			ColorPicker.readColor(state, color);

			color = ColorMath.hsvToColor(state.m_hue, state.m_sat, state.m_value, state.m_alpha);
			state.m_lastColor = color;
		}

		internal static void drawInline(string id, float x, float y, float w, ref Color color, float maxHitW = 0f)
		{
			ColorPicker.syncState(id, ref color);

			if (x < Layout.kInlineMinX)
				x = Layout.kInlineMinX;

			float offsetX = MenuHost.m_blockOffset.x;

			float offsetY = MenuHost.m_blockOffset.y;

			Rect drawRect = RectMath.create(offsetX + x, offsetY + y + Layout.kSwatchNudgeY, Layout.kSwatchSize, Layout.kSwatchSize);
			ColorPicker.drawSwatch(drawRect, ColorPicker.resolve(id, color));

			float defaultHit = FastMath.min(Layout.kSwatchSize + Layout.kHitExtra, w - x);

			float hitW = maxHitW > 0f ? FastMath.min(defaultHit, maxHitW) : defaultHit;

			Rect hitRect = RectMath.create(RectMath.x(drawRect), RectMath.y(drawRect), hitW, Layout.kSwatchSize);
			if (!(Gui.enabled && RectMath.contains(hitRect, MenuHost.scrollMouse) && MenuHost.mouseDown0))
				return;

			ColorPicker.openPopup(id, RectMath.create(x, y + Layout.kSwatchNudgeY, Layout.kSwatchSize, Layout.kSwatchSize));
		}

		internal static void readPopup(Rect viewRect, Vector2 scrollOffset)
		{
			if (ColorPicker.m_activeId == null)
				return;
			if (ColorPicker.m_activeAnim != null && MenuAnimation.isClosingOrClosed(ColorPicker.m_activeAnim))
				return;

			if (!ColorPicker.kStates.TryGetValue(ColorPicker.m_activeId, out PickerState state))
			{
				ColorPicker.closePopup();
				return;
			}

			ColorPicker.buildTextures();

			Rect popupRect = ColorPicker.popupRect(viewRect, scrollOffset, ColorPicker.popupHeight(ColorPicker.m_activeId));
			ColorPicker.m_lastRect = popupRect;
			ColorPicker.readInput(state, popupRect, new PickerRects(popupRect), ColorPicker.checkRect(popupRect), ColorPicker.trackRect(popupRect));
		}

		internal static void drawPopup(Rect viewRect, Vector2 scrollOffset)
		{
			string animId = ColorPicker.m_activeAnim;
			if (animId == null)
				return;

			if (MenuAnimation.consumeClosed(animId))
			{
				ColorPicker.m_activeAnim = null;
				ColorPicker.m_fadingId = null;
				return;
			}

			bool interactive = ColorPicker.m_activeId != null && !MenuAnimation.isClosingOrClosed(animId);
			if (!interactive && !MenuAnimation.shouldRender(animId))
			{
				ColorPicker.m_activeAnim = null;
				ColorPicker.m_fadingId = null;
				return;
			}

			string renderId = ColorPicker.renderId();
			if (renderId == null)
				return;
			if (!ColorPicker.kStates.TryGetValue(renderId, out PickerState state))
				return;

			ColorPicker.buildTextures();

			Rect popupRect = ColorPicker.popupRect(viewRect, scrollOffset, ColorPicker.popupHeight(renderId));

			PickerRects rects = new PickerRects(popupRect);
			using (MenuAnimation.beginSheet(animId, MotionMath.extent(RectMath.w(popupRect), RectMath.h(popupRect))))
			{
				Theme.drawWindowBg(popupRect);

				Color prevGui = Gui.color;

				float guiAlpha = prevGui.a;

				Color pureHue = ColorMath.hsvToColor(state.m_hue, HueSweep.kSaturation, HueSweep.kValue, 1f);

				Color opaqueCurrent = ColorMath.hsvToColor(state.m_hue, state.m_sat, state.m_value, 1f);

				Color current = ColorMath.withAlpha(opaqueCurrent, state.m_alpha);

				ColorPicker.drawSv(rects.m_sv, pureHue, guiAlpha);
				ColorPicker.drawSvCursor(rects.m_sv, state, guiAlpha);

				Gui.color = ColorMath.withAlpha(ColorMath.kWhite, guiAlpha);
				Gui.drawTextureSnapped(rects.m_hue, ColorPicker.m_hueTexture);

				ColorPicker.drawBorder(rects.m_hue, ColorMath.withAlpha(Theme.Surfaces.m_overlay0, guiAlpha));
				ColorPicker.drawHueCursor(rects.m_hue, state, guiAlpha);
				ColorPicker.drawAlpha(rects.m_alpha, opaqueCurrent, guiAlpha);
				ColorPicker.drawAlphaCursor(rects.m_alpha, state, guiAlpha);
				ColorPicker.drawPreview(rects.m_preview, ColorPicker.resolve(renderId, current), guiAlpha);

				Gui.color = prevGui;

				ColorPicker.drawHex(popupRect, state, current);
				ColorPicker.drawRainbow(popupRect, state, renderId);

				if (ColorPicker.rainbowOn(renderId))
					ColorPicker.drawSpeed(popupRect, state, renderId);

				state.m_lastColor = current;
			}
		}

		internal static void snapClose()
		{
			if (ColorPicker.m_activeAnim != null)
				MenuAnimation.snapClose(ColorPicker.m_activeAnim);

			ColorPicker.releaseInput();
			ColorPicker.m_activeId = null;
			ColorPicker.m_activeAnim = null;
			ColorPicker.m_fadingId = null;
		}

		internal static void cleanup()
		{
			ColorPicker.closePopup();
			ColorPicker.m_activeAnim = null;
			ColorPicker.m_fadingId = null;
			ColorPicker.invalidateTextures();

			ColorPicker.kStates.Clear();
		}

		internal static void invalidateTextures()
		{
			Textures.destroy(ref ColorPicker.m_hueTexture);
			Textures.destroy(ref ColorPicker.m_checkerTexture);
			Textures.destroy(ref ColorPicker.m_satOverlay);
			Textures.destroy(ref ColorPicker.m_valueOverlay);
			Textures.destroy(ref ColorPicker.m_alphaOverlay);
		}

		private static string animId(string id) => $"cp:{id}";
		private static string renderId() => ColorPicker.m_activeId ?? ColorPicker.m_fadingId;
		private static bool rainbowOn(string id) => id != null && ColorPicker.kRainbowEnabled.TryGetValue(id, out bool enabled) && enabled;

		private static void appendEntry(StringBuilder text, string id, bool enabled, float speed)
		{
			if (text.Length > 0)
				text.Append(';');

			text.Append(id).Append('=').Append(enabled ? '1' : '0').Append(':').Append(speed.ToString("F3", CultureInfo.InvariantCulture));
		}

		private static void parseState(string state, string[] scope)
		{
			if (string.IsNullOrEmpty(state))
				return;

			foreach (Match match in ColorPicker.kEntryRegex.Matches(state))
			{
				string id = match.Groups["id"].Value;
				if (scope != null && Array.IndexOf(scope, id) < 0)
					continue;

				ColorPicker.kRainbowEnabled[id] = match.Groups["flag"].Value == "1";

				if (!float.TryParse(match.Groups["speed"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float speed))
					continue;

				ColorPicker.kRainbowSpeeds[id] = FastMath.clamp(speed, Rainbow.kMinSpeed, Rainbow.kMaxSpeed);
			}
		}

		private static float clampedSpeed(string id)
		{
			float speed = ColorPicker.savedSpeed(id);

			float clamped = FastMath.clamp(speed, Rainbow.kMinSpeed, Rainbow.kMaxSpeed);
			if (clamped != speed)
				UiLog.info($"Config: rainbow speed for '{id}' is set past its safe range, saved {clamped.ToString("F3", CultureInfo.InvariantCulture)} instead of {speed.ToString("F3", CultureInfo.InvariantCulture)}");
			return clamped;
		}

		private static float savedSpeed(string id)
		{
			if (id == null)
				return Rainbow.kDefaultSpeed;

			return ColorPicker.kRainbowSpeeds.TryGetValue(id, out float speed) ? speed : Rainbow.kDefaultSpeed;
		}

		private static void updateCache()
		{
			int frame = Gui.frameCount;
			if (frame == ColorPicker.m_cacheFrame)
				return;

			ColorPicker.kResolveCache.Clear();

			ColorPicker.m_cacheFrame = frame;
		}

		private static PickerState stateFor(string id, Color color)
		{
			if (ColorPicker.kStates.TryGetValue(id, out PickerState existing))
				return existing;

			PickerState created = new PickerState(id);
			ColorMath.rgbToHsv(color, out created.m_hue, out created.m_sat, out created.m_value);
			created.m_alpha = color.a;
			created.m_lastColor = color;
			ColorPicker.kStates[id] = created;
			return created;
		}

		private static void readColor(PickerState state, Color color)
		{
			if (ColorMath.approxEqual(color, state.m_lastColor))
				return;

			ColorMath.rgbToHsv(color, out state.m_hue, out state.m_sat, out state.m_value);
			state.m_alpha = color.a;
			state.m_lastColor = color;
		}

		private static void openPopup(string id, Rect anchorRect)
		{
			ColorPicker.m_activeId = id;
			ColorPicker.m_activeAnim = ColorPicker.animId(id);
			ColorPicker.m_fadingId = null;
			ColorPicker.m_anchorRect = anchorRect;
			ColorPicker.m_blockOffset = MenuHost.m_blockOffset;
			ColorPicker.m_popupEntered = false;
			ColorPicker.m_justOpened = true;
			ColorPicker.resetDrags();

			MenuAnimation.noteOpened(ColorPicker.m_activeAnim);
		}

		private static void closePopup()
		{
			if (ColorPicker.m_activeAnim != null)
				MenuAnimation.close(ColorPicker.m_activeAnim);

			if (ColorPicker.m_activeId != null)
				ColorPicker.m_fadingId = ColorPicker.m_activeId;

			ColorPicker.releaseInput();
			ColorPicker.m_activeId = null;
		}

		private static void releaseInput()
		{
			if (ColorPicker.m_activeId != null && ColorPicker.kStates.TryGetValue(ColorPicker.m_activeId, out PickerState state))
			{
				TextInputFocus.clearIf(state.m_hexControl);

				if (Widgets.sliderEditing(state.m_speedSlider))
					Widgets.closeSliderEdit();
			}

			ColorPicker.m_popupEntered = false;
			ColorPicker.m_justOpened = false;
			ColorPicker.resetDrags();
		}

		private static void resetDrags()
		{
			ColorPicker.m_dragSv = false;
			ColorPicker.m_dragHue = false;
			ColorPicker.m_dragAlpha = false;
			ColorPicker.m_dragSpeed = false;
		}

		private static float popupHeight(string id) => ColorPicker.rainbowOn(id) ? ColorPicker.kBaseH + ColorPicker.kSpeedExtra : ColorPicker.kBaseH;

		private static Rect popupRect(Rect viewRect, Vector2 scrollOffset, float popupH)
		{
			float anchorX = RectMath.x(ColorPicker.m_anchorRect) + ColorPicker.m_blockOffset.x - scrollOffset.x + RectMath.x(viewRect);

			float anchorY = RectMath.y(ColorPicker.m_anchorRect) + ColorPicker.m_blockOffset.y - scrollOffset.y + RectMath.y(viewRect);

			float screenH = MenuHost.designH;

			float popupX = anchorX + RectMath.w(ColorPicker.m_anchorRect) + Layout.kAnchorGap;

			float screenW = MenuHost.kBaseWidth;
			if (popupX + ColorPicker.kPopupW > screenW)
				popupX = anchorX - ColorPicker.kPopupW - Layout.kAnchorGap;

			float popupY = anchorY;
			if (popupY + popupH > screenH)
				popupY = screenH - popupH;

			if (popupY < 0f)
				popupY = 0f;

			if (popupX < 0f)
				popupX = 0f;
			return RectMath.create(popupX, popupY, ColorPicker.kPopupW, popupH);
		}

		private static Rect hexRect(Rect popupRect)
		{
			float y = RectMath.y(popupRect) + Layout.kPopupPad + Layout.kSvSize + Layout.kAlphaGap + Layout.kAlphaH + Layout.kHexGap;
			return RectMath.create(RectMath.x(popupRect) + Layout.kPopupPad, y, ColorPicker.kPopupW - Layout.kPopupPad * 2f, Layout.kHexRowH);
		}

		private static Rect rainbowRect(Rect popupRect)
		{
			Rect hexRect = ColorPicker.hexRect(popupRect);

			float y = RectMath.yMax(hexRect) + Layout.kRainbowGap;
			return RectMath.create(RectMath.x(popupRect) + Layout.kPopupPad, y, ColorPicker.kPopupW - Layout.kPopupPad * 2f, Layout.kRainbowRowH);
		}

		private static Rect checkRect(Rect popupRect)
		{
			Rect rainbowRect = ColorPicker.rainbowRect(popupRect);

			float y = RectMath.y(rainbowRect) + (Layout.kRainbowRowH - Layout.kCheckSize) * 0.5f;
			return RectMath.create(RectMath.x(rainbowRect), y, Layout.kCheckSize, Layout.kCheckSize);
		}

		private static Rect speedRect(Rect popupRect)
		{
			Rect rainbowRect = ColorPicker.rainbowRect(popupRect);

			float y = RectMath.yMax(rainbowRect) + Layout.kSpeedGap;
			return RectMath.create(RectMath.x(popupRect) + Layout.kPopupPad, y, ColorPicker.kPopupW - Layout.kPopupPad * 2f, Layout.kSpeedRowH);
		}

		private static Rect trackRect(Rect popupRect)
		{
			Rect row = ColorPicker.speedRect(popupRect);

			float trackX = RectMath.x(row) + Layout.kSpeedLabelW;

			float trackW = RectMath.xMax(row) - Layout.kSpeedValueW - trackX - Layout.kTrackGap;
			return RectMath.create(trackX, RectMath.y(row), trackW, RectMath.h(row));
		}

		private static void readInput(PickerState state, Rect popupRect, in PickerRects rects, Rect checkRect, Rect trackRect)
		{
			Vector2 mouse = MenuHost.mousePosition;

			bool inside = RectMath.contains(popupRect, mouse);

			bool dragging = ColorPicker.m_dragSv || ColorPicker.m_dragHue || ColorPicker.m_dragAlpha || ColorPicker.m_dragSpeed;
			if (dragging && MenuHost.mouseHeld0)
			{
				if (ColorPicker.m_dragSv)
					ColorPicker.dragSv(state, rects.m_sv, mouse);
				else if (ColorPicker.m_dragHue)
					ColorPicker.dragHue(state, rects.m_hue, mouse);
				else if (ColorPicker.m_dragAlpha)
					ColorPicker.dragAlpha(state, rects.m_alpha, mouse);
				else
					ColorPicker.dragSpeed(trackRect, mouse);
				return;
			}

			if (dragging && !MenuHost.mouseHeld0)
			{
				ColorPicker.resetDrags();
				return;
			}

			if (inside)
			{
				ColorPicker.m_popupEntered = true;

				if (MenuHost.mouseDown0)
				{
					if (RectMath.contains(rects.m_sv, mouse))
					{
						ColorPicker.m_dragSv = true;
						ColorPicker.dragSv(state, rects.m_sv, mouse);
					}
					else if (RectMath.contains(rects.m_hue, mouse))
					{
						ColorPicker.m_dragHue = true;
						ColorPicker.dragHue(state, rects.m_hue, mouse);
					}
					else if (RectMath.contains(rects.m_alpha, mouse))
					{
						ColorPicker.m_dragAlpha = true;
						ColorPicker.dragAlpha(state, rects.m_alpha, mouse);
					}
					else if (RectMath.contains(checkRect, mouse))
						ColorPicker.toggleRainbow(ColorPicker.m_activeId);
					else if (ColorPicker.rainbowOn(ColorPicker.m_activeId) && RectMath.contains(trackRect, mouse) && !Widgets.sliderEditing(state.m_speedSlider))
					{
						ColorPicker.m_dragSpeed = true;
						ColorPicker.dragSpeed(trackRect, mouse);
					}
				}
			}
			else
			{
				if (MenuHost.mouseDown0 && !ColorPicker.m_justOpened)
				{
					ColorPicker.closePopup();

					MenuHost.consumeMouseDown0();
				}
				else if (ColorPicker.m_popupEntered)
					ColorPicker.closePopup();
			}

			if (!Gui.repainting)
				return;

			ColorPicker.m_justOpened = false;
		}

		private static void dragSv(PickerState state, Rect rect, Vector2 mouse)
		{
			float newSat = EasingMath.normalize(mouse.x, RectMath.x(rect), RectMath.xMax(rect));

			float newValue = 1f - EasingMath.normalize(mouse.y, RectMath.y(rect), RectMath.yMax(rect));
			if (FastMath.abs(newSat - state.m_sat) < ColorPicker.kDragEpsilon && FastMath.abs(newValue - state.m_value) < ColorPicker.kDragEpsilon)
				return;

			state.m_sat = newSat;
			state.m_value = newValue;
		}

		private static void dragHue(PickerState state, Rect rect, Vector2 mouse)
		{
			float newHue = 1f - EasingMath.normalize(mouse.y, RectMath.y(rect), RectMath.yMax(rect));
			if (FastMath.abs(newHue - state.m_hue) < ColorPicker.kDragEpsilon)
				return;

			state.m_hue = newHue;
		}

		private static void dragAlpha(PickerState state, Rect rect, Vector2 mouse) => state.m_alpha = EasingMath.normalize(mouse.x, RectMath.x(rect), RectMath.xMax(rect));

		private static void dragSpeed(Rect trackRect, Vector2 mouse)
		{
			float ratio = Widgets.sliderRatio(trackRect, mouse.x);
			ColorPicker.kRainbowSpeeds[ColorPicker.m_activeId] = FastMath.lerp(Rainbow.kMinSpeed, Rainbow.kMaxSpeed, ratio);
		}

		private static void toggleRainbow(string id)
		{
			ColorPicker.kRainbowEnabled[id] = !ColorPicker.rainbowOn(id);

			ColorPicker.kRainbowSpeeds.TryAdd(id, Rainbow.kDefaultSpeed);
		}

		private static void drawSv(Rect svRect, Color pureHue, float guiAlpha)
		{
			Color prev = Gui.color;

			Gui.color = ColorMath.withAlpha(pureHue, guiAlpha);
			Gui.drawTextureSnapped(svRect, Gui.whiteTexture);
			Gui.color = ColorMath.withAlpha(ColorMath.kWhite, guiAlpha);
			Gui.drawTextureSnapped(svRect, ColorPicker.m_satOverlay);
			Gui.drawTextureSnapped(svRect, ColorPicker.m_valueOverlay);

			ColorPicker.drawBorder(svRect, ColorMath.withAlpha(Theme.Surfaces.m_overlay0, guiAlpha));

			Gui.color = prev;
		}

		private static void drawAlpha(Rect alphaRect, Color opaqueCurrent, float guiAlpha)
		{
			Color prev = Gui.color;

			ColorPicker.drawChecker(alphaRect, guiAlpha);
			Gui.color = ColorMath.withAlpha(opaqueCurrent, guiAlpha);
			Gui.drawTextureSnapped(alphaRect, ColorPicker.m_alphaOverlay);

			ColorPicker.drawBorder(alphaRect, ColorMath.withAlpha(Theme.Surfaces.m_overlay0, guiAlpha));

			Gui.color = prev;
		}

		private static void drawPreview(Rect previewRect, Color current, float guiAlpha)
		{
			Color prev = Gui.color;

			ColorPicker.drawChecker(previewRect, guiAlpha);
			Gui.color = ColorMath.withAlpha(current, current.a * guiAlpha);
			Gui.drawTextureSnapped(previewRect, Gui.whiteTexture);
			Gui.color = prev;

			ColorPicker.drawBorder(previewRect, ColorMath.withAlpha(Theme.Surfaces.m_overlay0, guiAlpha));
		}

		private static void drawChecker(Rect rect, float guiAlpha)
		{
			Gui.color = ColorMath.withAlpha(ColorMath.kWhite, guiAlpha);
			Gui.drawTextureSnapped(rect, ColorPicker.m_checkerTexture);
		}

		private static void drawHex(Rect popupRect, PickerState state, Color current)
		{
			Rect hexRect = ColorPicker.hexRect(popupRect);
			UiText.fittedLabel(RectMath.create(RectMath.x(hexRect), RectMath.y(hexRect), Layout.kHashW, Layout.kHexRowH), "#", Theme.label, TextAnchor.MiddleLeft);

			string controlName = state.m_hexControl;

			// native focus lookup is a frame stale and one stale false shrinks the string under the cursor
			bool focused = TextInputFocus.activeName == controlName;

			string currentHex = ColorPicker.hexFor(state, current);
			if (!focused && state.m_hexInput != currentHex)
				state.m_hexInput = currentHex;

			if (focused)
				ColorPicker.filterHex();

			Rect fieldRect = RectMath.create(RectMath.x(hexRect) + Layout.kHashW, RectMath.y(hexRect), RectMath.w(hexRect) - Layout.kHashW, Layout.kHexRowH);

			Vector2 savedMouse = MenuHost.scrollMouse;

			MenuHost.scrollMouse = MenuHost.mousePosition;

			string newInput;
			try
			{
				newInput = Widgets.fixedTextField(fieldRect, state.m_hexInput, Theme.textField, controlName, HexField.kRgbaLength);
			}
			finally
			{
				MenuHost.scrollMouse = savedMouse;
			}
			if (newInput == state.m_hexInput)
				return;

			string sanitized = ColorPicker.sanitizeHex(newInput);
			state.m_hexInput = sanitized;

			// a paste skips the keystroke filter
			if (sanitized.Length != newInput.Length)
				MenuTextEditor.moveToEnd(sanitized);

			ColorPicker.applyHex(state);
		}

		// unity GUI.TextField inserts Event.current.character verbatim so filtering after shrinks it behind the cursor
		private static void filterHex()
		{
			Event current = Event.current;
			if (current.type != EventType.KeyDown)
				return;

			char typed = current.character;
			if (typed == '\0' || char.IsControl(typed))
				return;

			current.character = ColorHex.hexDigit(typed) ? char.ToUpperInvariant(typed) : '\0';
		}

		private static void drawRainbow(Rect popupRect, PickerState state, string renderId)
		{
			bool enabled = ColorPicker.rainbowOn(renderId);

			Rect checkRect = ColorPicker.checkRect(popupRect);

			Rect rainbowRect = ColorPicker.rainbowRect(popupRect);

			bool hovered = RectMath.contains(rainbowRect, MenuHost.mousePosition);
			MenuAnimation.drawCheckbox(checkRect, enabled, hovered, state.m_rainbowCheck);

			float labelX = RectMath.xMax(checkRect) + Layout.kCheckGap;

			float labelW = RectMath.xMax(popupRect) - Layout.kPopupPad - labelX;

			float labelY = RectMath.y(checkRect) + (RectMath.h(checkRect) - Layout.kRainbowRowH) * 0.5f;
			UiText.fittedLabel(RectMath.create(labelX, labelY, labelW, Layout.kRainbowRowH), "<b>Rainbow</b>", Theme.label, TextAnchor.MiddleLeft);
		}

		private static void drawSpeed(Rect popupRect, PickerState state, string renderId)
		{
			Rect row = ColorPicker.speedRect(popupRect);

			Rect track = ColorPicker.trackRect(popupRect);

			float speed = ColorPicker.savedSpeed(renderId);

			string sliderId = state.m_speedSlider;

			Rect labelRect = RectMath.create(RectMath.x(row), RectMath.y(row), Layout.kSpeedLabelW, Layout.kSpeedRowH);

			bool justOpened = Widgets.openSliderEdit(sliderId, labelRect, MenuHost.mousePosition, MenuHost.mouseDown0, speed);
			if (Widgets.sliderEditing(sliderId))
			{
				UiText.fittedLabel(labelRect, ColorPicker.kSpeedLabel, Theme.label, TextAnchor.MiddleLeft);

				Rect editRect = RectMath.create(RectMath.x(track), RectMath.y(row), RectMath.xMax(row) - RectMath.x(track), Layout.kSpeedRowH);

				float edited = Widgets.drawSliderEdit(editRect, MenuHost.mousePosition, MenuHost.mouseDown0, justOpened, speed, Rainbow.kMinSpeed, Rainbow.kMaxSpeed, sliderId, true);
				if (edited != speed)
					ColorPicker.kRainbowSpeeds[renderId] = edited;
				return;
			}

			float ratio = EasingMath.normalize(speed, Rainbow.kMinSpeed, Rainbow.kMaxSpeed);

			UiText.fittedLabel(labelRect, ColorPicker.kSpeedLabel, Theme.label, TextAnchor.MiddleLeft);
			UiText.fittedLabel(RectMath.create(RectMath.xMax(row) - Layout.kSpeedValueW, RectMath.y(row), Layout.kSpeedValueW, Layout.kSpeedRowH), Widgets.numberText(speed, "F2", "x"), Theme.valueLabel, TextAnchor.MiddleRight);

			bool hovered = !ColorPicker.m_dragSpeed && RectMath.contains(track, MenuHost.mousePosition);
			Widgets.drawSliderVisual(track, ratio, hovered, ColorPicker.m_dragSpeed, sliderId.GetHashCode());
		}

		private static void drawSwatch(Rect rect, Color color)
		{
			Rect snapped = Gui.snapRect(rect);

			Color prev = Gui.color;

			if (color.a < 1f)
				Gui.drawTexture(snapped, ColorPicker.checkerTexture);

			Gui.color = ColorMath.modulate(prev, color);
			Gui.drawTexture(snapped, Gui.whiteTexture);
			Gui.color = prev;

			ColorPicker.drawBorder(snapped, ColorMath.modulate(prev, Theme.Surfaces.m_overlay0));
		}

		private static void drawSvCursor(Rect rect, PickerState state, float guiAlpha)
		{
			float cx = RectMath.x(rect) + state.m_sat * RectMath.w(rect);

			float cy = RectMath.y(rect) + (1f - state.m_value) * RectMath.h(rect);

			float core = Gui.snapThickness(CursorShape.kCrossCore);

			float halo = core + Gui.devicePixel * 2f;
			ColorPicker.drawCross(cx, cy, CursorShape.kHaloArm, halo, ColorMath.withAlpha(ColorMath.kBlack, guiAlpha));
			ColorPicker.drawCross(cx, cy, CursorShape.kCoreArm, core, ColorMath.withAlpha(ColorMath.kWhite, guiAlpha));
		}

		private static void drawCross(float cx, float cy, float arm, float thickness, Color color)
		{
			Rect across = Gui.snapStroke(RectMath.create(cx - arm, cy - thickness * 0.5f, arm * 2f, thickness));

			Rect down = Gui.snapStroke(RectMath.create(cx - thickness * 0.5f, cy - arm, thickness, arm * 2f));

			Color prev = Gui.color;

			Gui.color = color;
			Gui.drawStroke(across, Gui.whiteTexture);
			Gui.drawStroke(RectMath.minMax(RectMath.x(down), RectMath.y(down), RectMath.xMax(down), RectMath.y(across)), Gui.whiteTexture);
			Gui.drawStroke(RectMath.minMax(RectMath.x(down), RectMath.yMax(across), RectMath.xMax(down), RectMath.yMax(down)), Gui.whiteTexture);
			Gui.color = prev;
		}

		private static void drawHueCursor(Rect rect, PickerState state, float guiAlpha)
		{
			float cy = FastMath.clamp(RectMath.y(rect) + (1f - state.m_hue) * RectMath.h(rect), RectMath.y(rect) + CursorShape.kBarInset, RectMath.yMax(rect) - CursorShape.kBarInset);

			float core = Gui.snapThickness(CursorShape.kBarCore);

			float halo = core + Gui.devicePixel * 2f;
			ColorPicker.drawBar(RectMath.create(RectMath.x(rect), cy - halo * 0.5f, RectMath.w(rect), halo), ColorMath.withAlpha(ColorMath.kBlack, guiAlpha));
			ColorPicker.drawBar(RectMath.create(RectMath.x(rect), cy - core * 0.5f, RectMath.w(rect), core), ColorMath.withAlpha(ColorMath.kWhite, guiAlpha));
		}

		private static void drawAlphaCursor(Rect rect, PickerState state, float guiAlpha)
		{
			float cx = FastMath.clamp(RectMath.x(rect) + state.m_alpha * RectMath.w(rect), RectMath.x(rect) + CursorShape.kBarInset, RectMath.xMax(rect) - CursorShape.kBarInset);

			float core = Gui.snapThickness(CursorShape.kBarCore);

			float halo = core + Gui.devicePixel * 2f;
			ColorPicker.drawBar(RectMath.create(cx - halo * 0.5f, RectMath.y(rect), halo, RectMath.h(rect)), ColorMath.withAlpha(ColorMath.kBlack, guiAlpha));
			ColorPicker.drawBar(RectMath.create(cx - core * 0.5f, RectMath.y(rect), core, RectMath.h(rect)), ColorMath.withAlpha(ColorMath.kWhite, guiAlpha));
		}

		private static void drawBar(Rect rect, Color color)
		{
			Color prev = Gui.color;

			Gui.color = color;
			Gui.drawStroke(rect, Gui.whiteTexture);
			Gui.color = prev;
		}

		private static void drawBorder(Rect rect, Color color)
		{
			float px = Gui.devicePixel;

			Rect ring = RectMath.inflate(Gui.snapRect(rect), px);
			Gui.drawRoundedTexture(ring, color, VectorMath.v4(px, px, px, px), VectorMath.v4(0f, 0f, 0f, 0f));
		}

		private static void buildTextures()
		{
			if (!Live.isUnityAlive(ColorPicker.m_hueTexture))
				ColorPicker.m_hueTexture = ColorPicker.buildHue();

			if (!Live.isUnityAlive(ColorPicker.m_checkerTexture))
				ColorPicker.m_checkerTexture = ColorPicker.buildChecker();

			Color transparentWhite = ColorPicker.kClearWhite;

			Color transparentBlack = ColorPicker.kClearBlack;

			if (!Live.isUnityAlive(ColorPicker.m_satOverlay))
				ColorPicker.m_satOverlay = ColorPicker.buildHorizontal(ColorMath.kWhite, transparentWhite);

			if (!Live.isUnityAlive(ColorPicker.m_valueOverlay))
				ColorPicker.m_valueOverlay = ColorPicker.buildVertical(transparentBlack, ColorMath.kBlack);

			if (Live.isUnityAlive(ColorPicker.m_alphaOverlay))
				return;

			ColorPicker.m_alphaOverlay = ColorPicker.buildHorizontal(transparentWhite, ColorMath.kWhite);
		}

		private static Texture2D buildHue() => Textures.createPixel(TextureSizes.kStripWidth, TextureSizes.kHue, FilterMode.Bilinear, ColorPicker.huePixel);
		private static Color huePixel(int x, int y) => ColorMath.hsvToColor(EasingMath.indexFraction(y, TextureSizes.kHue), HueSweep.kSaturation, HueSweep.kValue, 1f);
		private static Texture2D buildHorizontal(Color left, Color right) => Textures.createPixel(TextureSizes.kGradientSteps, TextureSizes.kStripHeight, FilterMode.Bilinear, (int x, int y) => ColorMath.lerp(left, right, EasingMath.indexFraction(x, TextureSizes.kGradientSteps)));
		private static Texture2D buildVertical(Color top, Color bottom) => Textures.createPixel(TextureSizes.kStripWidth, TextureSizes.kGradientSteps, FilterMode.Bilinear, (int x, int y) => ColorMath.lerp(bottom, top, EasingMath.indexFraction(y, TextureSizes.kGradientSteps)));
		private static Texture2D buildChecker() => Textures.createPixel(TextureSizes.kChecker, TextureSizes.kChecker, FilterMode.Point, ColorPicker.checkerPixel);
		private static Color checkerPixel(int x, int y) => (x / TextureSizes.kCheckerCell + y / TextureSizes.kCheckerCell) % CheckerColors.kParity == 0 ? CheckerColors.kLight : CheckerColors.kDark;

		private static string hexFor(PickerState state, Color current)
		{
			if (state.m_cachedHex.Length != 0 && ColorMath.approxEqual(state.m_hexColor, current))
				return state.m_cachedHex;

			state.m_cachedHex = ColorHex.of(current);
			state.m_hexColor = current;
			return state.m_cachedHex;
		}

		private static string sanitizeHex(string input)
		{
			char[] digits = new char[HexField.kRgbaLength];

			int length = 0;
			foreach (char c in input)
			{
				if (length >= HexField.kRgbaLength)
					break;
				if (!ColorHex.hexDigit(c))
					continue;

				digits[length++] = char.ToUpperInvariant(c);
			}
			return new string(digits, 0, length);
		}

		private static void applyHex(PickerState state)
		{
			string hex = state.m_hexInput;
			if (hex.Length != HexField.kRgbLength && hex.Length != HexField.kRgbaLength)
				return;
			if (!ColorHex.tryParse(hex, out Color parsed))
				return;

			ColorMath.rgbToHsv(parsed, out state.m_hue, out state.m_sat, out state.m_value);

			if (hex.Length != HexField.kRgbaLength)
				return;

			state.m_alpha = parsed.a;
		}
	}
}
