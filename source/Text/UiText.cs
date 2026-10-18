using System;
using System.Collections.Generic;
using UnityEngine;

namespace Trellis
{
	// measuring, drawing and formatting text, with every result cached per theme layout
	internal static class UiText
	{
		private const string kEllipsisSuffix = "...";

		private static readonly EvictingCache<(GUIStyle style, string text, float width, float height), LabelFit> kFitCache = new EvictingCache<(GUIStyle style, string text, float width, float height), LabelFit>(Limits.kMeasureMax);
		private static readonly EvictingCache<(GUIStyle style, string text, float width), float> kHeightCache = new EvictingCache<(GUIStyle style, string text, float width), float>(Limits.kMeasureMax);
		private static readonly EvictingCache<(GUIStyle style, string text), (float width, float height)> kNaturalSizes = new EvictingCache<(GUIStyle style, string text), (float width, float height)>(Limits.kMeasureMax);
		private static readonly EvictingCache<(GUIStyle style, string text, float width, int fontSize, bool keepMarkup), string> kEllipsisCache = new EvictingCache<(GUIStyle style, string text, float width, int fontSize, bool keepMarkup), string>(Limits.kMeasureMax);
		private static readonly Dictionary<GUIStyle, StyleMetrics> kStyleMetrics = new Dictionary<GUIStyle, StyleMetrics>();
		private static readonly Dictionary<GUIStyle, GUIStyle> kWrapStyles = new Dictionary<GUIStyle, GUIStyle>();
		private static readonly Dictionary<string, string> kBoldMarkups = new Dictionary<string, string>();
		private static readonly Dictionary<string, string> kSmallMarkups = new Dictionary<string, string>();
		private static readonly Dictionary<string, string> kSubgroupMarkups = new Dictionary<string, string>();
		private static readonly EvictingCache<(string text, string hex), string> kToneMarkups = new EvictingCache<(string text, string hex), string>(Limits.kMarkupMax);
		private static readonly EvictingCache<string, string> kDisplayNames = new EvictingCache<string, string>(Limits.kMarkupMax);

		private static int m_fitVersion = -1;
		private static int m_heightVersion = -1;
		private static int m_wrapVersion = -1;

		private static class Limits
		{
			internal const int kMinFitSize = 9;
			internal const int kMarkupMax = 1 << 9;
			internal const int kMeasureMax = 1 << 12;
			internal const float kMeasureStep = 4f;
		}

		private enum FitMode
		{
			Single,
			Wrapped,
			Truncated
		}

		private readonly struct LabelFit
		{
			internal readonly int m_fontSize;
			internal readonly FitMode m_mode;

			internal LabelFit(int fontSize, FitMode mode)
			{
				m_fontSize = fontSize;
				m_mode = mode;
			}
		}

		private readonly struct StyleMetrics
		{
			internal readonly int m_fontSize;
			internal readonly float m_paddingWidth;
			internal readonly float m_clipPadding;
			internal readonly bool m_wordWrap;

			internal StyleMetrics(int fontSize, float paddingWidth, float clipPaddingHeight, bool wordWrap)
			{
				m_fontSize = fontSize;
				m_paddingWidth = paddingWidth;
				m_clipPadding = clipPaddingHeight;
				m_wordWrap = wordWrap;
			}
		}

		internal static float measuredWidth(GUIStyle style, string text)
		{
			UiText.expireCaches();
			return UiText.naturalWidth(style, text);
		}

		internal static float measuredHeight(GUIStyle style, string text, float width) => UiText.measuredHeight(style, text, Gui.content(text), width);

		internal static float measuredHeight(GUIStyle style, string key, GUIContent content, float width)
		{
			if (UiText.m_heightVersion != Theme.layoutVersion)
			{
				UiText.kHeightCache.clear();

				UiText.m_heightVersion = Theme.layoutVersion;
			}

			float snapped = UiText.snapWidth(width);

			(GUIStyle, string, float) cacheKey = (style, key, snapped);
			if (UiText.kHeightCache.tryGetValue(cacheKey, out float cached))
				return Gui.ceilPixel(cached);

			float height = style.CalcHeight(content, snapped);
			UiText.kHeightCache.set(cacheKey, height);
			return Gui.ceilPixel(height);
		}

		internal static void fittedLabel(Rect rect, string text, GUIStyle style, TextAnchor align)
		{
			if (!Gui.repainting)
				return;
			if (RectMath.w(rect) <= 0f || string.IsNullOrEmpty(text))
				return;

			LabelFit fit = UiText.fitLabel(style, text, RectMath.w(rect), RectMath.h(rect));

			StyleMetrics metrics = UiText.metrics(style);

			TextAnchor originalAlign = style.alignment;

			// each style write is an interop call
			bool resized = fit.m_fontSize != metrics.m_fontSize;
			if (resized)
				style.fontSize = fit.m_fontSize;
			style.alignment = align;
			switch (fit.m_mode)
			{
				case FitMode.Wrapped:
					style.wordWrap = true;
					Gui.label(rect, text, style);

					style.wordWrap = metrics.m_wordWrap;
					break;

				case FitMode.Truncated:
					Gui.label(rect, UiText.ellipsize(style, text, RectMath.w(rect), fit.m_fontSize), style);

					UiText.showTruncated(rect, text);
					break;

				default:
					Gui.label(rect, text, style);
					break;
			}

			if (resized)
				style.fontSize = metrics.m_fontSize;
			style.alignment = originalAlign;
		}

		internal static bool ellipsizedLabel(Rect rect, string text, GUIStyle style, TextAnchor align, bool keepMarkup = false)
		{
			if (RectMath.w(rect) <= 0f)
				return !string.IsNullOrEmpty(text);

			string shown = UiText.ellipsize(style, text, RectMath.w(rect), keepMarkup);
			UiText.alignedLabel(rect, shown, style, align);

			bool cut = !object.ReferenceEquals(shown, text);
			if (cut && Gui.repainting)
				UiText.showTruncated(rect, text);
			return cut;
		}

		internal static void alignedLabel(Rect rect, string text, GUIStyle style, TextAnchor align)
		{
			if (!Gui.repainting || RectMath.w(rect) <= 0f)
				return;

			TextAnchor prev = style.alignment;

			style.alignment = align;
			Gui.label(rect, text, style);

			style.alignment = prev;
		}

		internal static string ellipsize(GUIStyle style, string text, float width, bool keepMarkup = false)
		{
			UiText.expireCaches();

			if (string.IsNullOrEmpty(text) || width <= 0f || UiText.naturalWidth(style, text) <= width)
				return text;

			return UiText.ellipsize(style, text, width, UiText.fontSize(style), keepMarkup);
		}

		internal static GUIStyle wrapStyle(GUIStyle baseStyle)
		{
			if (UiText.m_wrapVersion != Theme.layoutVersion)
			{
				UiText.kWrapStyles.Clear();

				UiText.m_wrapVersion = Theme.layoutVersion;
			}

			if (UiText.kWrapStyles.TryGetValue(baseStyle, out GUIStyle cached))
				return cached;

			GUIStyle wrapped = Theme.derive(baseStyle);
			wrapped.wordWrap = true;
			wrapped.clipping = TextClipping.Clip;
			wrapped.alignment = TextAnchor.UpperLeft;
			UiText.kWrapStyles[baseStyle] = wrapped;
			return wrapped;
		}

		internal static void wrappedLabel(float x, float y, float width, string text, GUIStyle baseStyle, out float height)
		{
			GUIStyle style = UiText.wrapStyle(baseStyle);
			height = UiText.measuredHeight(style, text, width);
			Gui.label(RectMath.create(x, y, width, height), text, style);
		}

		internal static void keyValueRow(Rect rect, string label, string value, float labelRatio, float valueGap, GUIStyle labelStyle, GUIStyle valueStyle)
		{
			if (!Gui.repainting)
				return;

			float labelW = RectMath.w(rect) * labelRatio;

			float valueX = RectMath.x(rect) + labelW + valueGap;

			float valueW = RectMath.xMax(rect) - valueX;

			UiText.fittedLabel(RectMath.create(RectMath.x(rect), RectMath.y(rect), labelW, RectMath.h(rect)), UiText.bold(label), labelStyle, TextAnchor.MiddleLeft);
			UiText.fittedLabel(RectMath.create(valueX, RectMath.y(rect), valueW, RectMath.h(rect)), value, valueStyle, TextAnchor.MiddleRight);
		}

		internal static string small(string text) => UiText.cachedMarkup(UiText.kSmallMarkups, text ?? string.Empty, "<size=10>", "</size>", false);
		internal static string bold(string text) => UiText.cachedMarkup(UiText.kBoldMarkups, text, "<b>", "</b>", false);
		internal static string caution(string text) => UiText.toned(text, ThemeSlot.Warning);
		internal static string hazard(string text) => UiText.toned(text, ThemeSlot.Danger);
		internal static string subgroup(string text) => UiText.cachedMarkup(UiText.kSubgroupMarkups, text, "<b>", "</b>", true);

		internal static string tinted(string text, Color color)
		{
			(string, string) key = (text, ColorHex.of(color));
			if (UiText.kToneMarkups.tryGetValue(key, out string cached))
				return cached;

			string markup = RichText.wrapColor(text, color);
			UiText.kToneMarkups.set(key, markup);
			return markup;
		}

		internal static string displayName(string raw)
		{
			string key = raw ?? string.Empty;
			if (UiText.kDisplayNames.tryGetValue(key, out string cached))
				return cached;

			string sanitized = RichText.displayName(key);
			UiText.kDisplayNames.set(key, sanitized);
			return sanitized;
		}

		internal static string number(float value, string format, string unit) => Widgets.numberText(value, format, unit);
		internal static string percent(float fraction) => Widgets.percentText(fraction);
		internal static string multiplier(float value) => Widgets.multiplierText(value);
		internal static string count(int value, string unit) => Widgets.countText(value, unit);
		internal static string integer(int value) => Widgets.intDisplay(value);

		private static float snapWidth(float width) => LayoutMath.snapDown(width, Limits.kMeasureStep);

		private static void expireCaches()
		{
			if (UiText.m_fitVersion == Theme.layoutVersion)
				return;

			UiText.kFitCache.clear();

			UiText.kNaturalSizes.clear();

			UiText.kStyleMetrics.Clear();

			UiText.m_fitVersion = Theme.layoutVersion;
		}

		private static StyleMetrics metrics(GUIStyle style)
		{
			if (UiText.kStyleMetrics.TryGetValue(style, out StyleMetrics cached))
				return cached;

			RectOffset padding = style.padding;

			float clipPadding = style.clipping == TextClipping.Clip ? 0f : padding.vertical;

			StyleMetrics metrics = new StyleMetrics(style.fontSize, padding.horizontal, clipPadding, style.wordWrap);
			UiText.kStyleMetrics[style] = metrics;
			return metrics;
		}

		private static int fontSize(GUIStyle style) => UiText.metrics(style).m_fontSize;
		private static float naturalWidth(GUIStyle style, string text) => UiText.naturalSize(style, text).width;

		private static (float width, float height) naturalSize(GUIStyle style, string text)
		{
			(GUIStyle, string) key = (style, text);
			if (UiText.kNaturalSizes.tryGetValue(key, out (float, float) cached))
				return cached;

			Theme.kScratchContent.text = text;

			Vector2 size = style.CalcSize(Theme.kScratchContent);

			(float, float) measured = (size.x, size.y);
			UiText.kNaturalSizes.set(key, measured);
			return measured;
		}

		private static LabelFit fitLabel(GUIStyle style, string text, float width, float height)
		{
			UiText.expireCaches();

			StyleMetrics metrics = UiText.metrics(style);
			if (width <= 0f || string.IsNullOrEmpty(text))
				return new LabelFit(metrics.m_fontSize, FitMode.Single);

			(float naturalW, float naturalH) = UiText.naturalSize(style, text);

			float boxH = height + metrics.m_clipPadding;
			if (naturalW <= width && naturalH <= boxH)
				return new LabelFit(metrics.m_fontSize, FitMode.Single);

			float snappedW = UiText.snapWidth(width);

			float snappedH = FastMath.floor(boxH);

			(GUIStyle, string, float, float) key = (style, text, snappedW, snappedH);
			if (UiText.kFitCache.tryGetValue(key, out LabelFit cached))
				return cached;

			Theme.kScratchContent.text = text;

			float widthScale = FastMath.max(1f, snappedW - metrics.m_paddingWidth) / FastMath.max(1f, naturalW - metrics.m_paddingWidth);

			float heightScale = snappedH / FastMath.max(1f, naturalH);

			int estimate = FastMath.clamp(FastMath.floorToInt(metrics.m_fontSize * FastMath.min(widthScale, heightScale)), Limits.kMinFitSize, metrics.m_fontSize);

			LabelFit fit = UiText.largestFit(style, metrics, estimate, snappedW, snappedH);

			style.fontSize = metrics.m_fontSize;

			UiText.kFitCache.set(key, fit);
			return fit;
		}

		private static LabelFit largestFit(GUIStyle style, in StyleMetrics metrics, int estimate, float width, float height)
		{
			int high = estimate;

			int best = -1;

			int low = Limits.kMinFitSize;
			while (low <= high)
			{
				int mid = (low + high) >> 1;
				style.fontSize = mid;

				Vector2 size = style.CalcSize(Theme.kScratchContent);
				if (size.x <= width && size.y <= height)
				{
					best = mid;

					low = mid + 1;
				}
				else
					high = mid - 1;
			}
			if (best >= 0)
				return new LabelFit(best, FitMode.Single);

			style.fontSize = Limits.kMinFitSize;
			style.wordWrap = true;

			float wrappedH = style.CalcHeight(Theme.kScratchContent, width);

			style.wordWrap = metrics.m_wordWrap;
			return new LabelFit(Limits.kMinFitSize, wrappedH <= height ? FitMode.Wrapped : FitMode.Truncated);
		}

		private static void showTruncated(Rect rect, string text)
		{
			if (!(MenuHost.m_visible && !MenuHost.tooltipPending && RectMath.contains(rect, MenuHost.scrollMouse)))
				return;

			MenuHost.showTooltip(RichText.visibleText(text));
		}

		private static float measureCurrent(GUIStyle style, string text)
		{
			Theme.kScratchContent.text = text;
			return style.CalcSize(Theme.kScratchContent).x;
		}

		private static string ellipsize(GUIStyle style, string text, float width, int fontSize, bool keepMarkup = false)
		{
			if (string.IsNullOrEmpty(text))
				return text;

			float snapped = UiText.snapWidth(width);

			(GUIStyle, string, float, int, bool) key = (style, text, snapped, fontSize, keepMarkup);
			if (UiText.kEllipsisCache.tryGetValue(key, out string cached))
				return cached;
			if (UiText.measureCurrent(style, text) <= snapped)
				return text;

			string result = keepMarkup ? UiText.cutRich(style, text, snapped) : UiText.cutPlain(style, text, snapped);
			UiText.kEllipsisCache.set(key, result);
			return result;
		}

		private static string cutPlain(GUIStyle style, string text, float width)
		{
			string plain = RichText.visibleText(text);

			int keep = UiText.longestFitting(style, width, plain.Length, (int length) => plain[..length]);
			return keep <= 0 ? UiText.kEllipsisSuffix : $"{plain[..keep]}{UiText.kEllipsisSuffix}";
		}

		private static string cutRich(GUIStyle style, string text, float width)
		{
			int visible = RichText.countGlyphs(text);

			int keep = UiText.longestFitting(style, width, visible, (int length) => RichText.truncateVisible(text, length));
			return keep <= 0 ? UiText.kEllipsisSuffix : $"{RichText.truncateVisible(text, keep)}{UiText.kEllipsisSuffix}";
		}

		private static int longestFitting(GUIStyle style, float width, int maxLength, Func<int, string> build)
		{
			int high = maxLength;

			int best = 0;

			int low = 0;
			while (low <= high)
			{
				int mid = (low + high) >> 1;
				if (UiText.measureCurrent(style, $"{build(mid)}{UiText.kEllipsisSuffix}") <= width)
				{
					best = mid;

					low = mid + 1;
				}
				else
					high = mid - 1;
			}
			return best;
		}

		// callers keep their own cache so one markup shape never evicts another
		internal static string cachedMarkup(Dictionary<string, string> cache, string text, string open, string close, bool upper)
		{
			if (cache.TryGetValue(text, out string markup))
				return markup;

			if (cache.Count >= Limits.kMarkupMax)
				cache.Clear();

			markup = $"{open}{(upper ? text.ToUpperInvariant() : text)}{close}";
			cache[text] = markup;
			return markup;
		}

		// the rainbow resolved colour would mint a new widget identity every frame
		private static string toned(string text, ThemeSlot slot) => UiText.tinted(text, ThemeConfig.SlotColor[(int)slot]);
	}
}
