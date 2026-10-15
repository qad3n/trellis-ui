using System;
using System.Collections.Generic;
using UnityEngine;

namespace Trellis
{
	internal static partial class Widgets
	{
		private const string kEllipsisSuffix = "...";

		private static readonly EvictingCache<(GUIStyle style, string text, float width, float height), LabelFit> kFitCache = new EvictingCache<(GUIStyle style, string text, float width, float height), LabelFit>(RowPrimitiveLayout.kMeasureMax);

		private static readonly EvictingCache<(GUIStyle style, string text, float width), float> kHeightCache = new EvictingCache<(GUIStyle style, string text, float width), float>(RowPrimitiveLayout.kMeasureMax);
		private static readonly Dictionary<(string prefix, int index), string> kIndexedIds = new Dictionary<(string prefix, int index), string>();
		private static readonly Dictionary<(int block, string prefix, string label, int occurrence), string> kWidgetIds = new Dictionary<(int block, string prefix, string label, int occurrence), string>();
		private static readonly Dictionary<(int block, string prefix, string label), int> kOccurrences = new Dictionary<(int block, string prefix, string label), int>();
		private static readonly EvictingCache<(GUIStyle style, string text), (float width, float height)> kNaturalSizes = new EvictingCache<(GUIStyle style, string text), (float width, float height)>(RowPrimitiveLayout.kMeasureMax);
		private static readonly Dictionary<GUIStyle, StyleMetrics> kStyleMetrics = new Dictionary<GUIStyle, StyleMetrics>();

		private static readonly EvictingCache<(GUIStyle style, string text, float width, int fontSize, bool keepMarkup), string> kEllipsisCache = new EvictingCache<(GUIStyle style, string text, float width, int fontSize, bool keepMarkup), string>(RowPrimitiveLayout.kMeasureMax);
		private static readonly Dictionary<string, string> kBoldMarkups = new Dictionary<string, string>();
		private static readonly Dictionary<string, string> kSubgroupMarkups = new Dictionary<string, string>();
		private static readonly Dictionary<string, string> kSmallMarkups = new Dictionary<string, string>();
		private static readonly EvictingCache<(string text, string hex), string> kToneMarkups = new EvictingCache<(string text, string hex), string>(RowPrimitiveLayout.kMarkupMax);
		private static readonly EvictingCache<string, string> kDisplayNames = new EvictingCache<string, string>(RowPrimitiveLayout.kMarkupMax);
		private static readonly EvictingCache<(int count, string noun, string inner, string trailing), string> kCountLines = new EvictingCache<(int count, string noun, string inner, string trailing), string>(RowPrimitiveLayout.kMarkupMax);

		private static int m_fitVersion = -1;
		private static int m_heightVersion = -1;

		internal static class InfoRowLayout
		{
			internal const float kRowHeight = 22f;
			internal const float kRowAdvance = 24f;
			// verdana line box is 1.215 em so 15px needs 19
			internal const float kSubHeaderHeight = 19f;
			internal const float kSubHeaderAdvance = 20f;
			internal const float kLabelRatio = 0.44f;
			internal const float kValueGap = 8f;
			internal const float kStackedAdvance = 20f;
			internal const float kStackedIndent = 12f;
		}

		internal static class CountLine
		{
			internal const float kHeight = 20f;
			internal const float kAdvance = 28f;
		}

		private static class RowPrimitiveLayout
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

		internal static float measuredHeight(GUIStyle style, string text, float width) => Widgets.measuredHeight(style, text, Gui.content(text), width);

		internal static float measuredHeight(GUIStyle style, string key, GUIContent content, float width)
		{
			if (Widgets.m_heightVersion != Theme.layoutVersion)
			{
				Widgets.kHeightCache.clear();

				Widgets.m_heightVersion = Theme.layoutVersion;
			}

			float snapped = Widgets.snapWidth(width);

			(GUIStyle, string, float) cacheKey = (style, key, snapped);
			if (Widgets.kHeightCache.tryGetValue(cacheKey, out float cached))
				return Gui.ceilPixel(cached);

			float height = style.CalcHeight(content, snapped);
			Widgets.kHeightCache.set(cacheKey, height);
			return Gui.ceilPixel(height);
		}

		internal static string indexedId(string prefix, int index)
		{
			(string, int) key = (prefix, index);
			if (Widgets.kIndexedIds.TryGetValue(key, out string id))
				return id;

			id = $"{prefix}{index}";
			Widgets.kIndexedIds[key] = id;
			return id;
		}

		internal static void resetOccurrences() => Widgets.kOccurrences.Clear();

		internal static float measuredWidth(GUIStyle style, string text)
		{
			Widgets.expireCaches();
			return Widgets.naturalWidth(style, text);
		}

		internal static void fittedLabel(Rect rect, string text, GUIStyle style, TextAnchor align)
		{
			if (!Gui.repainting)
				return;
			if (RectMath.w(rect) <= 0f || string.IsNullOrEmpty(text))
				return;

			LabelFit fit = Widgets.fitLabel(style, text, RectMath.w(rect), RectMath.h(rect));

			StyleMetrics metrics = Widgets.metrics(style);

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
					Gui.label(rect, Widgets.ellipsize(style, text, RectMath.w(rect), fit.m_fontSize), style);

					Widgets.showTruncated(rect, text);
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

			string shown = Widgets.ellipsize(style, text, RectMath.w(rect), keepMarkup);
			Widgets.alignedLabel(rect, shown, style, align);

			bool cut = !object.ReferenceEquals(shown, text);
			if (cut && Gui.repainting)
				Widgets.showTruncated(rect, text);
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
			Widgets.expireCaches();

			if (string.IsNullOrEmpty(text) || width <= 0f || Widgets.naturalWidth(style, text) <= width)
				return text;

			return Widgets.ellipsize(style, text, width, Widgets.fontSize(style), keepMarkup);
		}

		internal static string small(string text) => Widgets.markup(Widgets.kSmallMarkups, text ?? string.Empty, "<size=10>", "</size>", false);
		internal static string bold(string text) => Widgets.markup(Widgets.kBoldMarkups, text, "<b>", "</b>", false);
		internal static string caution(string text) => Widgets.toned(text, ThemeSlot.Warning);
		internal static string hazard(string text) => Widgets.toned(text, ThemeSlot.Danger);

		internal static string tinted(string text, Color color)
		{
			(string, string) key = (text, ColorHex.of(color));
			if (Widgets.kToneMarkups.tryGetValue(key, out string cached))
				return cached;

			string markup = RichText.wrapColor(text, color);
			Widgets.kToneMarkups.set(key, markup);
			return markup;
		}

		internal static string displayName(string raw)
		{
			string key = raw ?? string.Empty;
			if (Widgets.kDisplayNames.tryGetValue(key, out string cached))
				return cached;

			string sanitized = RichText.displayName(key);
			Widgets.kDisplayNames.set(key, sanitized);
			return sanitized;
		}

		internal static void keyValueRow(Rect rect, string label, string value, float labelRatio, float valueGap, GUIStyle labelStyle, GUIStyle valueStyle)
		{
			if (!Gui.repainting)
				return;

			float labelW = RectMath.w(rect) * labelRatio;

			float valueX = RectMath.x(rect) + labelW + valueGap;

			float valueW = RectMath.xMax(rect) - valueX;

			Widgets.fittedLabel(RectMath.create(RectMath.x(rect), RectMath.y(rect), labelW, RectMath.h(rect)), Widgets.bold(label), labelStyle, TextAnchor.MiddleLeft);
			Widgets.fittedLabel(RectMath.create(valueX, RectMath.y(rect), valueW, RectMath.h(rect)), value, valueStyle, TextAnchor.MiddleRight);
		}

		internal static float infoRowHeight(float w, string value) => Widgets.fitsInline(w, value) ? InfoRowLayout.kRowAdvance : InfoRowLayout.kStackedAdvance + Widgets.stackedHeight(w, value);

		internal static void drawInfoRowAt(ref float y, float x, float w, string label, string value)
		{
			if (Widgets.fitsInline(w, value))
			{
				Widgets.keyValueRow(RectMath.create(x, y, w, InfoRowLayout.kRowHeight), label, value, InfoRowLayout.kLabelRatio, InfoRowLayout.kValueGap, Theme.label, Theme.valueLabel);

				y += InfoRowLayout.kRowAdvance;
				return;
			}
			Widgets.fittedLabel(RectMath.create(x, y, w, InfoRowLayout.kRowHeight), Widgets.bold(label), Theme.label, TextAnchor.MiddleLeft);

			y += InfoRowLayout.kStackedAdvance;

			float valueH = Widgets.stackedHeight(w, value);
			Widgets.alignedLabel(RectMath.create(x + InfoRowLayout.kStackedIndent, y, Widgets.stackedWidth(w), valueH), value, Widgets.wrapStyle(Theme.valueLabel), TextAnchor.MiddleLeft);

			y += valueH;
		}

		internal static void drawInfoRow(ref float y, float w, string label, string value, string tooltip = null)
		{
			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, RowPrefix.kInfo, label);

			float screenY = MenuHost.m_blockOffset.y + y;

			float start = screenY;

			Widgets.drawInfoRowAt(ref screenY, MenuHost.m_blockOffset.x, w, label, value);
			if (tooltip != null && Gui.enabled)
				MenuHost.hoverTooltip(RectMath.create(MenuHost.m_blockOffset.x, start, w, screenY - start), tooltip);

			y += screenY - start;
			Widgets.endRow(ref y, ref row);
		}

		internal static void drawSubHeader(ref float y, float w, string text, string tooltip = null)
		{
			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, RowPrefix.kSubHeader, null);

			float screenY = MenuHost.m_blockOffset.y + y;
			Widgets.drawSubHeaderAt(ref screenY, MenuHost.m_blockOffset.x, w, text, tooltip);

			y += InfoRowLayout.kSubHeaderAdvance;
			Widgets.endRow(ref y, ref row);
		}

		internal static void drawSubHeaderAt(ref float y, float x, float w, string text, string tooltip = null)
		{
			Rect rect = RectMath.create(x, y, w, InfoRowLayout.kSubHeaderHeight);
			MenuHost.hoverTooltip(rect, tooltip);

			Widgets.subgroupHeader(rect, text);

			y += InfoRowLayout.kSubHeaderAdvance;
		}

		internal static void drawCountLine(ref float y, float x, float w, int count, string noun, string innerSuffix = "", string trailing = "")
		{
			Widgets.fittedLabel(RectMath.create(x, y, w, CountLine.kHeight), Widgets.countLineText(count, noun, innerSuffix, trailing), Theme.label, TextAnchor.MiddleLeft);

			y += CountLine.kAdvance;
		}

		private static float snapWidth(float width) => LayoutMath.snapDown(width, RowPrimitiveLayout.kMeasureStep);

		private static int nextOccurrence(string prefix, string label)
		{
			(int, string, string) occurrenceKey = (MenuHost.m_currentBlock, prefix, label);
			Widgets.kOccurrences.TryGetValue(occurrenceKey, out int occurrence);
			occurrence++;
			Widgets.kOccurrences[occurrenceKey] = occurrence;
			return occurrence;
		}

		private static string widgetId(string prefix, string label)
		{
			int occurrence = Widgets.nextOccurrence(prefix, label);

			(int, string, string, int) cacheKey = (MenuHost.m_currentBlock, prefix, label, occurrence);
			if (Widgets.kWidgetIds.TryGetValue(cacheKey, out string id))
				return id;

			id = occurrence == 1 ? $"{prefix}:{MenuHost.m_currentBlock}:{label}" : $"{prefix}:{MenuHost.m_currentBlock}:{label}#{occurrence}";
			Widgets.kWidgetIds[cacheKey] = id;
			return id;
		}

		private static void expireCaches()
		{
			if (Widgets.m_fitVersion == Theme.layoutVersion)
				return;

			Widgets.kFitCache.clear();

			Widgets.kNaturalSizes.clear();

			Widgets.kStyleMetrics.Clear();

			Widgets.m_fitVersion = Theme.layoutVersion;
		}

		private static StyleMetrics metrics(GUIStyle style)
		{
			if (Widgets.kStyleMetrics.TryGetValue(style, out StyleMetrics cached))
				return cached;

			RectOffset padding = style.padding;

			float clipPadding = style.clipping == TextClipping.Clip ? 0f : padding.vertical;

			StyleMetrics metrics = new StyleMetrics(style.fontSize, padding.horizontal, clipPadding, style.wordWrap);
			Widgets.kStyleMetrics[style] = metrics;
			return metrics;
		}

		private static int fontSize(GUIStyle style) => Widgets.metrics(style).m_fontSize;
		private static float naturalWidth(GUIStyle style, string text) => Widgets.naturalSize(style, text).width;

		private static (float width, float height) naturalSize(GUIStyle style, string text)
		{
			(GUIStyle, string) key = (style, text);
			if (Widgets.kNaturalSizes.tryGetValue(key, out (float, float) cached))
				return cached;

			Theme.kScratchContent.text = text;

			Vector2 size = style.CalcSize(Theme.kScratchContent);

			(float, float) measured = (size.x, size.y);
			Widgets.kNaturalSizes.set(key, measured);
			return measured;
		}

		private static LabelFit fitLabel(GUIStyle style, string text, float width, float height)
		{
			Widgets.expireCaches();

			StyleMetrics metrics = Widgets.metrics(style);
			if (width <= 0f || string.IsNullOrEmpty(text))
				return new LabelFit(metrics.m_fontSize, FitMode.Single);

			(float naturalW, float naturalH) = Widgets.naturalSize(style, text);

			float boxH = height + metrics.m_clipPadding;
			if (naturalW <= width && naturalH <= boxH)
				return new LabelFit(metrics.m_fontSize, FitMode.Single);

			float snappedW = Widgets.snapWidth(width);

			float snappedH = FastMath.floor(boxH);

			(GUIStyle, string, float, float) key = (style, text, snappedW, snappedH);
			if (Widgets.kFitCache.tryGetValue(key, out LabelFit cached))
				return cached;

			Theme.kScratchContent.text = text;

			float widthScale = FastMath.max(1f, snappedW - metrics.m_paddingWidth) / FastMath.max(1f, naturalW - metrics.m_paddingWidth);

			float heightScale = snappedH / FastMath.max(1f, naturalH);

			int estimate = FastMath.clamp(FastMath.floorToInt(metrics.m_fontSize * FastMath.min(widthScale, heightScale)), RowPrimitiveLayout.kMinFitSize, metrics.m_fontSize);

			LabelFit fit = Widgets.largestFit(style, metrics, estimate, snappedW, snappedH);

			style.fontSize = metrics.m_fontSize;

			Widgets.kFitCache.set(key, fit);
			return fit;
		}

		private static LabelFit largestFit(GUIStyle style, in StyleMetrics metrics, int estimate, float width, float height)
		{
			int high = estimate;

			int best = -1;

			int low = RowPrimitiveLayout.kMinFitSize;
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

			style.fontSize = RowPrimitiveLayout.kMinFitSize;
			style.wordWrap = true;

			float wrappedH = style.CalcHeight(Theme.kScratchContent, width);

			style.wordWrap = metrics.m_wordWrap;
			return new LabelFit(RowPrimitiveLayout.kMinFitSize, wrappedH <= height ? FitMode.Wrapped : FitMode.Truncated);
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

			float snapped = Widgets.snapWidth(width);

			(GUIStyle, string, float, int, bool) key = (style, text, snapped, fontSize, keepMarkup);
			if (Widgets.kEllipsisCache.tryGetValue(key, out string cached))
				return cached;
			if (Widgets.measureCurrent(style, text) <= snapped)
				return text;

			string result = keepMarkup ? Widgets.cutRich(style, text, snapped) : Widgets.cutPlain(style, text, snapped);
			Widgets.kEllipsisCache.set(key, result);
			return result;
		}

		private static string cutPlain(GUIStyle style, string text, float width)
		{
			string plain = RichText.visibleText(text);

			int keep = Widgets.longestFitting(style, width, plain.Length, (int length) => plain[..length]);
			return keep <= 0 ? Widgets.kEllipsisSuffix : $"{plain[..keep]}{Widgets.kEllipsisSuffix}";
		}

		private static string cutRich(GUIStyle style, string text, float width)
		{
			int visible = RichText.countGlyphs(text);

			int keep = Widgets.longestFitting(style, width, visible, (int length) => RichText.truncateVisible(text, length));
			return keep <= 0 ? Widgets.kEllipsisSuffix : $"{RichText.truncateVisible(text, keep)}{Widgets.kEllipsisSuffix}";
		}

		private static int longestFitting(GUIStyle style, float width, int maxLength, Func<int, string> build)
		{
			int high = maxLength;

			int best = 0;

			int low = 0;
			while (low <= high)
			{
				int mid = (low + high) >> 1;
				if (Widgets.measureCurrent(style, $"{build(mid)}{Widgets.kEllipsisSuffix}") <= width)
				{
					best = mid;

					low = mid + 1;
				}
				else
					high = mid - 1;
			}
			return best;
		}

		private static string markup(Dictionary<string, string> cache, string text, string open, string close, bool upper)
		{
			if (cache.TryGetValue(text, out string markup))
				return markup;

			if (cache.Count >= RowPrimitiveLayout.kMarkupMax)
				cache.Clear();

			markup = $"{open}{(upper ? text.ToUpperInvariant() : text)}{close}";
			cache[text] = markup;
			return markup;
		}

		// the rainbow resolved colour would mint a new widget identity every frame
		private static string toned(string text, ThemeSlot slot) => Widgets.tinted(text, ThemeConfig.SlotColor[(int)slot]);
		private static GUIStyle labelStyle(bool hovered) => hovered ? Theme.labelHover : Theme.label;

		internal static void drawFaded(Rect rect, Texture texture, float alpha)
		{
			Color prev = Gui.color;
			Gui.color = ColorMath.fade(prev, alpha);
			Gui.drawTextureSnapped(rect, texture);
			Gui.color = prev;
		}

		private static bool hoverRow(Rect rowRect, string tooltip)
		{
			bool hovered = Gui.enabled && RectMath.contains(rowRect, MenuHost.scrollMouse);

			if (Gui.enabled)
				MenuHost.hoverTooltip(rowRect, tooltip);
			return hovered;
		}

		private static void subgroupHeader(Rect rect, string text)
		{
			if (!Gui.repainting)
				return;

			Widgets.fittedLabel(rect, Widgets.markup(Widgets.kSubgroupMarkups, text, $"<color={Theme.Hex.kTextSecondary}><b>", "</b></color>", true), Theme.valueLabel, TextAnchor.MiddleLeft);
		}

		private static bool fitsInline(float w, string value)
		{
			Widgets.expireCaches();

			if (string.IsNullOrEmpty(value))
				return true;

			return value.IndexOf('\n') < 0 && Widgets.naturalWidth(Theme.valueLabel, value) <= w * (1f - InfoRowLayout.kLabelRatio) - InfoRowLayout.kValueGap;
		}

		private static float stackedWidth(float w) => FastMath.max(0f, w - InfoRowLayout.kStackedIndent);
		private static float stackedHeight(float w, string value) => FastMath.max(InfoRowLayout.kRowHeight, Widgets.measuredHeight(Widgets.wrapStyle(Theme.valueLabel), value, Widgets.stackedWidth(w)));

		private static string countLineText(int count, string noun, string innerSuffix, string trailing)
		{
			(int, string, string, string) key = (count, noun, innerSuffix, trailing);
			if (Widgets.kCountLines.tryGetValue(key, out string cached))
				return cached;

			string plural = count != 1 ? "s" : string.Empty;

			string text = $"<b>{count}</b> <color={Theme.Hex.kTextSecondary}>{noun}{plural}{innerSuffix}</color>{trailing}";
			Widgets.kCountLines.set(key, text);
			return text;
		}
	}
}
