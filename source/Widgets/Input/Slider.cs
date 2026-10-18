using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using static Trellis.MenuHost;

namespace Trellis
{
	internal static partial class Widgets
	{
		private const float kStepScale = 10f;
		private const string kSliderControl = "SliderEdit";
		private const string kResetText = "Reset";
		internal const float kIntStep = 1f;
		private const float kPercentPerUnit = 100f;
		private const string kMultiplierFormat = "F2";
		private const int kMaxCached = 512;
		private const float kStepDivisions = 200f;
		private const float kEmptyStep = 1f;
		private const float kStepEpsilon = 0.0001f;

		private static readonly Dictionary<int, string> kPercentTexts = new Dictionary<int, string>();
		private static readonly Dictionary<int, string> kIntTexts = new Dictionary<int, string>();
		private static readonly EvictingCache<(float value, string format, string unit), string> kNumberTexts = new EvictingCache<(float value, string format, string unit), string>(Widgets.kMaxCached);
		private static readonly EvictingCache<(int value, string unit), string> kCountTexts = new EvictingCache<(int value, string unit), string>(Widgets.kMaxCached);
		private static readonly EvictingCache<(string label, string display), string> kSliderCaptions = new EvictingCache<(string label, string display), string>(Widgets.kMaxCached);
		private static readonly Dictionary<string, string> kEditCaptions = new Dictionary<string, string>();
		private static readonly (float below, float step)[] kAutoSteps = new (float below, float step)[] { (0.001f, 0.001f), (0.01f, 0.01f), (0.1f, 0.05f), (1f, 0.1f), (10f, 1f) };
		private static readonly string[] kDecimalFormats = new[] { "F0", "F1", "F2", "F3", "F4" };

		private static string m_activeSlider;
		private static string m_sliderEditId;
		private static string m_sliderEditText;
		private static DoubleClick m_labelClick;

		private static class SliderSafety
		{
			internal const float kMaxMagnitude = 1000000f;
			internal const float kPositiveFloor = 0.0001f;
			internal const string kRangeWarning = "That value is outside the safe range for this setting.\n\n" + "Past the slider limits a setting can behave unpredictably, break the feature, " + "or crash the game. It stays active for this session only and is trimmed back " + "into range when you save your config.";
		}

		private static class SliderLayout
		{
			internal const float kThumbSize = 16f;
			internal const float kTrackH = 4f;
			internal const float kRowH = 20f;
			internal const float kLabelH = 20f;
			internal const float kEditH = 22f;
			internal const float kEditGap = 4f;
			internal const float kLabelGap = SliderLayout.kLabelAdvance - SliderLayout.kLabelH;
			internal const float kResetRowH = 24f;
			internal const float kResetW = 58f;
			internal const float kResetGap = 6f;
			private const float kLabelAdvance = 22f;
		}

		private static class SliderUnit
		{
			internal const string kMultiplier = "x";
			internal const string kPixels = "px";
			internal const string kMeters = "m";
			internal const string kPercent = "%";
			internal const string kNoLimit = "No Limit";
			internal const string kWholeFormat = "F0";
		}

		// parts must occlude not composite under a fade
		internal static void drawSliderVisual(Rect area, float ratio, bool hovered, bool dragging, int animKey)
		{
			float shownRatio = MenuAnimation.displayedValue(animKey, FastMath.clamp01(ratio), dragging, RectMath.w(area));

			float hoverT = MenuAnimation.hoverProgress(animKey, hovered && !dragging);

			float halfThumb = SliderLayout.kThumbSize * 0.5f;

			float rangeLeft = RectMath.x(area) + halfThumb;

			float rangeW = RectMath.w(area) - SliderLayout.kThumbSize;

			float trackY = RectMath.y(area) + (RectMath.h(area) - SliderLayout.kTrackH) * 0.5f;

			float thumbCx = rangeLeft + shownRatio * rangeW;

			Rect thumbRect = Gui.snapRect(RectMath.create(thumbCx - halfThumb, RectMath.y(area) + (RectMath.h(area) - SliderLayout.kThumbSize) * 0.5f, SliderLayout.kThumbSize, SliderLayout.kThumbSize));

			float thumbLeft = RectMath.x(thumbRect);

			float thumbRight = RectMath.xMax(thumbRect);

			Color prev = Gui.color;

			float fillW = thumbLeft - RectMath.x(area);
			if (fillW > 0f)
			{
				Gui.color = ColorMath.modulate(prev, Theme.m_accent);
				Gui.drawStroke(RectMath.create(RectMath.x(area), trackY, fillW, SliderLayout.kTrackH), Gui.whiteTexture);
				Gui.color = prev;
			}

			float restW = RectMath.xMax(area) - thumbRight;
			if (restW > 0f)
				Gui.drawStroke(RectMath.create(thumbRight, trackY, restW, SliderLayout.kTrackH), Theme.sliderTrack);

			Theme.drawThumb(thumbRect, hoverT, dragging);
		}

		internal static float sliderRatio(Rect area, float mouseX)
		{
			float half = SliderLayout.kThumbSize * 0.5f;
			return FastMath.clamp01(EasingMath.normalize(mouseX, RectMath.x(area) + half, RectMath.xMax(area) - half));
		}

		internal static void closeSliderEdit()
		{
			Widgets.m_activeSlider = null;

			if (Widgets.m_sliderEditId == null)
				return;

			TextInputFocus.clearIf(Widgets.kSliderControl);

			Widgets.m_sliderEditId = null;
			Widgets.m_sliderEditText = null;
		}

		internal static float drawSlider(ref float y, float w, string label, float value, float min, float max, float step = -1f) => Widgets.drawSliderUnits(ref y, w, label, SliderUnit.kMultiplier, value, min, max, step);
		internal static float drawSliderPx(ref float y, float w, string label, float value, float min, float max, float step = -1f) => Widgets.drawSliderUnits(ref y, w, label, SliderUnit.kPixels, value, min, max, step);
		internal static float drawSliderMeters(ref float y, float w, string label, float value, float min, float max, float step = -1f) => Widgets.drawSliderUnits(ref y, w, label, SliderUnit.kMeters, value, min, max, step);
		internal static float drawSliderPerSecond(ref float y, float w, string label, string unit, float value, float min, float max, float step = -1f) => Widgets.drawSliderUnits(ref y, w, label, unit, value, min, max, step);

		internal static float drawSliderUnits(ref float y, float w, string label, string unit, float value, float min, float max, float step = -1f)
		{
			float resolvedStep = step > 0f ? step : Widgets.autoStep(min, max);

			string display = Gui.repainting ? Widgets.numberText(value, Widgets.stepFormat(resolvedStep), unit) : null;
			return Widgets.drawSliderFull(ref y, w, label, display, value, min, max, resolvedStep);
		}

		internal static float drawSliderPercent(ref float y, float w, string label, float value, float min, float max, float step, string tooltip = null)
		{
			string display = Gui.repainting ? Widgets.percentDisplay((int)FastMath.round(value)) : null;
			return Widgets.drawSliderFull(ref y, w, label, display, value, min, max, step, tooltip);
		}

		internal static int drawSliderInt(ref float y, float w, string label, int value, float min, float max, string tooltip = null, float step = Widgets.kIntStep, string unit = null)
		{
			string display = Gui.repainting ? (unit == null ? Widgets.intDisplay(value) : Widgets.countText(value, unit)) : null;
			return FastMath.roundToInt(Widgets.drawSliderFull(ref y, w, label, display, value, min, max, step, tooltip));
		}

		internal static string percentText(float fraction) => Gui.repainting ? Widgets.percentDisplay(FastMath.roundToInt(fraction * Widgets.kPercentPerUnit)) : null;
		internal static string multiplierText(float value) => Gui.repainting ? Widgets.numberText(value, Widgets.kMultiplierFormat, SliderUnit.kMultiplier) : null;
		internal static string intDisplay(int value) => Widgets.numberDisplay(Widgets.kIntTexts, value, string.Empty);

		internal static float drawDistanceSlider(ref float y, float w, string label, float value, float min, float max, float step = 50f)
		{
			string display = null;
			if (Gui.repainting)
				display = value >= max ? SliderUnit.kNoLimit : Widgets.numberText(value, SliderUnit.kWholeFormat, SliderUnit.kMeters);
			return Widgets.drawSliderFull(ref y, w, label, display, value, min, max, step);
		}

		internal static string countText(int value, string unit)
		{
			(int, string) key = (value, unit);
			if (Widgets.kCountTexts.tryGetValue(key, out string cached))
				return cached;

			string text = $"{value.ToString(CultureInfo.InvariantCulture)}{unit}";
			Widgets.kCountTexts.set(key, text);
			return text;
		}

		internal static string numberText(float value, string format, string unit)
		{
			value = Widgets.quantize(value, format);

			(float, string, string) key = (value, format, unit);
			if (Widgets.kNumberTexts.tryGetValue(key, out string cached))
				return cached;

			string text = $"{value.ToString(format, CultureInfo.InvariantCulture)}{unit}";
			Widgets.kNumberTexts.set(key, text);
			return text;
		}

		internal static bool openSliderEdit(string sliderId, Rect labelRect, Vector2 mouse, bool mouseDown, float value)
		{
			if (Widgets.m_sliderEditId == sliderId || !Gui.enabled || !mouseDown || !RectMath.contains(labelRect, mouse))
				return false;
			if (!Widgets.m_labelClick.register(sliderId.GetHashCode()))
				return false;

			Widgets.m_sliderEditId = sliderId;
			Widgets.m_sliderEditText = value.ToString("G", CultureInfo.InvariantCulture);
			return true;
		}

		internal static bool sliderEditing(string sliderId) => Widgets.m_sliderEditId == sliderId;

		internal static float drawSliderEdit(Rect editRect, Vector2 mouse, bool mouseDown, bool justOpened, float value, float min, float max, string sliderId = null, bool allowUnsafe = false)
		{
			Widgets.m_sliderEditText = Widgets.fixedTextField(editRect, Widgets.m_sliderEditText, Theme.textField, Widgets.kSliderControl);

			bool submit = Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);

			bool cancel = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape;

			bool clickedAway = !justOpened && mouseDown && !RectMath.contains(editRect, mouse);
			if (submit || cancel || clickedAway)
			{
				if (!cancel)
					value = Widgets.commitSliderEdit(Widgets.m_sliderEditText, sliderId, value, min, max, allowUnsafe);

				Widgets.m_sliderEditId = null;
				Widgets.m_sliderEditText = null;

				TextInputFocus.clearIf(Widgets.kSliderControl);

				if (submit || cancel)
					Event.current.Use();
			}
			else if (justOpened)
				TextInputFocus.set(Widgets.kSliderControl);
			return value;
		}

		internal static float drawSliderFull(ref float y, float w, string label, string display, float value, float min, float max, float step, string tooltip = null)
		{
			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, RowPrefix.kSlider, label);

			float offsetX = MenuHost.m_blockOffset.x;

			float offsetY = MenuHost.m_blockOffset.y;

			string sliderId = Widgets.widgetId("sld", label);

			SliderGuard.flush();

			bool overridden = value < min || value > max;
			if (!overridden)
				SliderGuard.release(sliderId);

			float labelH = overridden ? SliderLayout.kResetRowH : SliderLayout.kLabelH;

			float labelW = overridden ? w - SliderLayout.kResetW - SliderLayout.kResetGap : w;

			Rect labelRect = RectMath.create(offsetX, offsetY + y, labelW, labelH);

			bool entering = Widgets.openSliderEdit(sliderId, labelRect, MenuHost.scrollMouse, MenuHost.mouseDown0, value);
			if (Widgets.sliderEditing(sliderId))
			{
				UiText.fittedLabel(RectMath.create(offsetX, offsetY + y, MenuHost.kLabelW, SliderLayout.kEditH), UiText.cachedMarkup(Widgets.kEditCaptions, label, "<b>", "</b>:", false), Theme.label, TextAnchor.MiddleLeft);

				Rect editRect = RectMath.create(offsetX + MenuHost.kLabelW, offsetY + y, w - MenuHost.kLabelW, SliderLayout.kEditH);
				value = Widgets.drawSliderEdit(editRect, MenuHost.scrollMouse, MenuHost.mouseDown0, entering, value, min, max, sliderId, true);

				y += SliderLayout.kEditH + SliderLayout.kEditGap + Widgets.kFieldGap;
				Widgets.endRow(ref y, ref row);
				return value;
			}

			float labelAdvance = labelH + SliderLayout.kLabelGap;

			Rect rowRect = RectMath.create(offsetX, offsetY + y, w, labelAdvance + SliderLayout.kRowH);

			bool rowHovered = Widgets.hoverRow(rowRect, tooltip);

			if (Gui.repainting && display != null)
				UiText.fittedLabel(labelRect, Widgets.captionFor(label, display), Widgets.labelStyle(rowHovered), TextAnchor.MiddleLeft);

			if (overridden)
				value = Widgets.drawReset(sliderId, RectMath.create(offsetX + w - SliderLayout.kResetW, offsetY + y, SliderLayout.kResetW, labelH), value, min, max);

			y += labelAdvance;
			value = Widgets.drawSliderControl(sliderId, ref y, w, value, min, max, step);
			Widgets.endRow(ref y, ref row);
			return value;
		}

		private static float sliderValue(Rect area, float min, float max, float step, out float ratio)
		{
			float value = EasingMath.snapStep(min + Widgets.sliderRatio(area, MenuHost.scrollMouse.x) * (max - min), min, max, step);
			ratio = FastMath.clamp01(EasingMath.normalize(value, min, max));
			return value;
		}

		private static string percentDisplay(int percent) => Widgets.numberDisplay(Widgets.kPercentTexts, percent, SliderUnit.kPercent);

		private static string numberDisplay(Dictionary<int, string> cache, int value, string suffix)
		{
			if (cache.TryGetValue(value, out string cached))
				return cached;

			if (cache.Count >= Widgets.kMaxCached)
				cache.Clear();

			string display = $"{value.ToString(CultureInfo.InvariantCulture)}{suffix}";
			cache[value] = display;
			return display;
		}

		private static string captionFor(string label, string display)
		{
			(string, string) key = (label, display);
			if (Widgets.kSliderCaptions.tryGetValue(key, out string cached))
				return cached;

			string caption = $"{UiText.bold(label)}: <color={Theme.Hex.kTextSecondary}>{display}</color>";
			Widgets.kSliderCaptions.set(key, caption);
			return caption;
		}

		private static float autoStep(float min, float max)
		{
			float range = max - min;
			if (range <= 0f)
				return Widgets.kEmptyStep;

			float raw = range / Widgets.kStepDivisions;
			foreach ((float below, float step) in Widgets.kAutoSteps)
			{
				if (raw < below)
					return step;
			}
			return FastMath.round(raw);
		}

		private static float quantize(float value, string format)
		{
			int decimals = Array.IndexOf(Widgets.kDecimalFormats, format);
			return decimals < 0 ? value : MathF.Round(value, decimals, MidpointRounding.AwayFromZero);
		}

		private static string stepFormat(float step)
		{
			float scaled = step;
			for (int decimals = 0; decimals < Widgets.kDecimalFormats.Length - 1; decimals++)
			{
				if (FastMath.abs(scaled - FastMath.round(scaled)) < Widgets.kStepEpsilon)
					return Widgets.kDecimalFormats[decimals];

				scaled *= Widgets.kStepScale;
			}
			return Widgets.kDecimalFormats[^1];
		}

		private static float commitSliderEdit(string text, string sliderId, float value, float min, float max, bool allowUnsafe)
		{
			if (!Widgets.tryParseInput(text, min, out float parsed))
				return value;
			if (!allowUnsafe)
				return FastMath.clamp(parsed, min, max);

			if (parsed >= min && parsed <= max)
			{
				SliderGuard.release(sliderId);
				return parsed;
			}

			SliderGuard.noteUnsafe(sliderId, parsed, min, max);

			MenuHost.showInfo(SliderSafety.kRangeWarning);
			return parsed;
		}

		private static bool tryParseInput(string text, float min, out float value)
		{
			value = 0f;

			if (string.IsNullOrWhiteSpace(text))
				return false;
			if (!float.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
				return false;
			if (!float.IsFinite(parsed))
				return false;

			value = Widgets.clampBounds(parsed, min);
			return true;
		}

		private static float clampBounds(float value, float min)
		{
			float bounded = FastMath.clamp(value, -SliderSafety.kMaxMagnitude, SliderSafety.kMaxMagnitude);
			if (min > 0f)
				return FastMath.max(bounded, FastMath.min(SliderSafety.kPositiveFloor, min));
			if (min == 0f)
				return FastMath.max(bounded, 0f);

			return bounded;
		}

		private static float drawReset(string sliderId, Rect rect, float value, float min, float max)
		{
			if (!Widgets.scrollButton(rect, Widgets.kResetText, Theme.button))
				return value;

			if (!SliderGuard.trySafeDefault(sliderId, out float restored))
				restored = value;

			SliderGuard.release(sliderId);
			return FastMath.clamp(restored, min, max);
		}

		private static float drawSliderControl(string sliderId, ref float y, float w, float value, float min, float max, float step)
		{
			float offsetX = MenuHost.m_blockOffset.x;

			float offsetY = MenuHost.m_blockOffset.y;

			float ratio = FastMath.clamp01(EasingMath.normalize(value, min, max));

			bool dragging = Widgets.m_activeSlider == sliderId;

			Rect area = RectMath.create(offsetX, offsetY + y, w, SliderLayout.kRowH);

			bool hovered = Gui.enabled && RectMath.contains(area, MenuHost.scrollMouse);

			if (dragging && MenuHost.scrollMouse.x < 0f)
			{
				Widgets.m_activeSlider = null;

				dragging = false;
			}

			if (dragging)
			{
				if (MenuHost.mouseHeld0)
					value = Widgets.sliderValue(area, min, max, step, out ratio);
				else
				{
					Widgets.m_activeSlider = null;

					dragging = false;
				}
			}
			else if (Gui.enabled && hovered && MenuHost.mouseDown0)
			{
				Widgets.m_activeSlider = sliderId;

				dragging = true;

				value = Widgets.sliderValue(area, min, max, step, out ratio);
			}
			Widgets.drawSliderVisual(area, ratio, hovered, dragging, sliderId.GetHashCode());

			y += SliderLayout.kRowH + Widgets.kFieldGap;
			return value;
		}
	}
}
