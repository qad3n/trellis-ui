using System;
using System.Collections.Generic;
using UnityEngine;

namespace Trellis
{
	internal static partial class Widgets
	{
		internal const int kMarkupCacheMax = 1 << 9;

		private static readonly Dictionary<(string prefix, int index), string> kIndexedIds = new Dictionary<(string prefix, int index), string>();
		private static readonly Dictionary<(int block, string prefix, string label, int occurrence), string> kWidgetIds = new Dictionary<(int block, string prefix, string label, int occurrence), string>();
		private static readonly Dictionary<(int block, string prefix, string label), int> kOccurrences = new Dictionary<(int block, string prefix, string label), int>();
		private static readonly EvictingCache<(int count, string noun, string inner, string trailing), string> kCountLines = new EvictingCache<(int count, string noun, string inner, string trailing), string>(Widgets.kMarkupCacheMax);

		internal static class InfoRowLayout
		{
			internal const float kRowHeight = 22f;
			internal const float kRowAdvance = 24f;
			// verdana line box is 1.215 em so 15px needs 19
			internal const float kSubHeaderHeight = 19f;
			internal const float kSubHeaderAdvance = 24f;
			internal const float kSubHeaderTopGap = 10f;
			internal const float kSubHeaderTickW = 3f;
			internal const float kSubHeaderTickH = 12f;
			internal const float kSubHeaderTickGap = 7f;
			internal const float kSubHeaderRuleGap = 10f;
			internal const float kSubHeaderRuleH = 1f;
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

		internal static float infoRowHeight(float w, string value) => Widgets.fitsInline(w, value) ? InfoRowLayout.kRowAdvance : InfoRowLayout.kStackedAdvance + Widgets.stackedHeight(w, value);

		internal static void drawInfoRowAt(ref float y, float x, float w, string label, string value)
		{
			if (Widgets.fitsInline(w, value))
			{
				UiText.keyValueRow(RectMath.create(x, y, w, InfoRowLayout.kRowHeight), label, value, InfoRowLayout.kLabelRatio, InfoRowLayout.kValueGap, Theme.label, Theme.valueLabel);

				y += InfoRowLayout.kRowAdvance;
				return;
			}
			UiText.fittedLabel(RectMath.create(x, y, w, InfoRowLayout.kRowHeight), UiText.bold(label), Theme.label, TextAnchor.MiddleLeft);

			y += InfoRowLayout.kStackedAdvance;

			float valueH = Widgets.stackedHeight(w, value);
			UiText.alignedLabel(RectMath.create(x + InfoRowLayout.kStackedIndent, y, Widgets.stackedWidth(w), valueH), value, UiText.wrapStyle(Theme.valueLabel), TextAnchor.MiddleLeft);

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

			// the gap splits a subsection from the rows above but a block that opens on one needs none
			if (y > 0f)
				y += InfoRowLayout.kSubHeaderTopGap;

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

		internal static void drawCountLine(ref float y, float x, float w, int count, string noun, string innerSuffix = "", string trailing = "", TextAnchor anchor = TextAnchor.MiddleLeft)
		{
			UiText.fittedLabel(RectMath.create(x, y, w, CountLine.kHeight), Widgets.countLineText(count, noun, innerSuffix, trailing), Theme.label, anchor);

			y += CountLine.kAdvance;
		}

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

			float tickY = RectMath.y(rect) + LayoutMath.centerOffset(RectMath.h(rect), InfoRowLayout.kSubHeaderTickH);
			Gui.drawStroke(RectMath.create(RectMath.x(rect), tickY, InfoRowLayout.kSubHeaderTickW, InfoRowLayout.kSubHeaderTickH), Theme.accentBar);

			string label = UiText.subgroup(text);

			float labelX = RectMath.x(rect) + InfoRowLayout.kSubHeaderTickW + InfoRowLayout.kSubHeaderTickGap;

			float labelW = FastMath.max(0f, FastMath.min(UiText.measuredWidth(Theme.label, label), RectMath.xMax(rect) - labelX));
			UiText.fittedLabel(RectMath.create(labelX, RectMath.y(rect), labelW, RectMath.h(rect)), label, Theme.label, TextAnchor.MiddleLeft);

			float ruleX = labelX + labelW + InfoRowLayout.kSubHeaderRuleGap;

			float ruleW = RectMath.xMax(rect) - ruleX;
			if (ruleW > 0f)
				Gui.drawStroke(RectMath.create(ruleX, RectMath.y(rect) + (RectMath.h(rect) - InfoRowLayout.kSubHeaderRuleH) * 0.5f, ruleW, InfoRowLayout.kSubHeaderRuleH), Theme.divider);
		}

		private static bool fitsInline(float w, string value)
		{
			if (string.IsNullOrEmpty(value))
				return true;

			return value.IndexOf('\n') < 0 && UiText.measuredWidth(Theme.valueLabel, value) <= w * (1f - InfoRowLayout.kLabelRatio) - InfoRowLayout.kValueGap;
		}

		private static float stackedWidth(float w) => FastMath.max(0f, w - InfoRowLayout.kStackedIndent);
		private static float stackedHeight(float w, string value) => FastMath.max(InfoRowLayout.kRowHeight, UiText.measuredHeight(UiText.wrapStyle(Theme.valueLabel), value, Widgets.stackedWidth(w)));

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
