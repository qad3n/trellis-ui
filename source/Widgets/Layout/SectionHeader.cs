using System.Collections.Generic;
using UnityEngine;
using static Trellis.MenuHost;

namespace Trellis
{
	internal static partial class Widgets
	{
		private static readonly Dictionary<string, string> kUpperHeaders = new Dictionary<string, string>();

		internal static class SectionHeaderLayout
		{
			internal const float kH = 22f;
			internal const float kPadX = 10f;
			internal const float kBorderH = 2f;
			internal const float kBottomGap = 8f;
			internal const float kRuleGap = 10f;
			internal const float kRuleH = 1f;
			internal const float kTrailingShare = 0.55f;
		}

		private static class DisclosureLayout
		{
			internal const float kH = 20f;
			internal const float kCaretInset = 1f;
			internal const float kCaretBox = 10f;
			internal const float kCaretGap = 6f;
			internal const float kTrailingGap = 8f;
			internal const float kBottomGap = 6f;
			internal const float kInsetPadX = 8f;
			internal const float kHoverEpsilon = 0.001f;
		}

		private static float disclosureHeight => DisclosureLayout.kH + DisclosureLayout.kBottomGap;

		// the trailing text names what the block edits
		internal static void drawSectionHeader(ref float y, float w, string text, string tooltip = null, string trailing = null)
		{
			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, RowPrefix.kHeader, null);
			Widgets.drawSectionHeaderAt(RectMath.create(MenuHost.m_blockOffset.x, MenuHost.m_blockOffset.y + y, w, SectionHeaderLayout.kH), text, tooltip, trailing);

			y += SectionHeaderLayout.kH + SectionHeaderLayout.kBottomGap;
			Widgets.endRow(ref y, ref row);
		}

		internal static void drawSectionHeaderAt(Rect rect, string text, string tooltip = null, string trailing = null)
		{
			bool hovered = MenuHost.hoverTooltip(rect, tooltip);

			float trailingW = Widgets.headerTrailingWidth(rect, trailing);
			if (ThemeConfig.TacticalHeaders)
				Widgets.drawTacticalHeader(RectMath.create(RectMath.x(rect), RectMath.y(rect), RectMath.w(rect) - trailingW, RectMath.h(rect)), text, hovered);
			else
			{
				Gui.drawTextureSnapped(rect, Theme.accentFaint);

				if (hovered)
					Gui.drawTextureSnapped(rect, Theme.disclosureHover);

				UiText.fittedLabel(RectMath.create(RectMath.x(rect) + SectionHeaderLayout.kPadX, RectMath.y(rect), RectMath.w(rect) - SectionHeaderLayout.kPadX - trailingW, RectMath.h(rect)), text, Theme.sectionHeader, TextAnchor.MiddleLeft);

				Gui.drawStroke(Gui.bottomEdge(rect, SectionHeaderLayout.kBorderH), Theme.titleAccent);
			}

			if (trailingW > 0f)
				UiText.ellipsizedLabel(RectMath.create(RectMath.xMax(rect) - trailingW, RectMath.y(rect), trailingW - SectionHeaderLayout.kPadX, RectMath.h(rect)), trailing, Theme.valueLabel, TextAnchor.MiddleRight);
		}

		internal static MenuAnimation.SectionScope beginDisclosure(ref float y, ref float w, string animId, string text, ref bool expanded, string bodyKey, bool locked = false, string trailing = null)
		{
			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, RowPrefix.kDisclosure, animId);

			Widgets.beginDisabled(locked);

			Rect headerRect = RectMath.create(MenuHost.m_blockOffset.x, MenuHost.m_blockOffset.y + y, w, DisclosureLayout.kH);
			if (Widgets.drawCollapsibleHeader(headerRect, animId, text, expanded, trailing))
				expanded = !expanded;

			Widgets.endDisabled();

			y += Widgets.disclosureHeight;
			Widgets.endRow(ref y, ref row);

			MenuAnimation.SectionScope body = MenuAnimation.beginSection(ref y, bodyKey, expanded && !locked);
			if (body.m_render)
			{
				Widgets.beginInset(y, ref w, body);
				y += Widgets.kFieldGap;
			}
			return body;
		}

		// the indent comes off before the section pops its clip so the restored offset stays exact
		internal static void endDisclosure(ref float y, ref float w, MenuAnimation.SectionScope body)
		{
			if (body.m_render)
			{
				Widgets.endInset(ref w);
				y += Widgets.kFieldGap;
			}

			MenuAnimation.endSection(ref y, body);
		}

		// a widgetId outside a block pass advances every pass and never eases
		internal static bool drawCollapsibleHeader(Rect rect, string animId, string text, bool expanded, string trailing = null)
		{
			Rect row = RectMath.create(RectMath.x(rect), RectMath.y(rect), RectMath.w(rect), DisclosureLayout.kH);

			bool hovered = Gui.enabled && RectMath.contains(row, MenuHost.scrollMouse);

			float open = MenuAnimation.disclosureProgress(animId, expanded);

			float hoverT = MenuAnimation.disclosureHover(animId, hovered);
			if (Gui.repainting)
				Widgets.drawDisclosureRow(row, text, trailing, open, hoverT);
			return hovered && MenuHost.mouseDown0;
		}

		// the label brightens while open or hovered so a folded header stays quiet
		private static void drawDisclosureRow(Rect row, string text, string trailing, float open, float hoverT)
		{
			if (hoverT > DisclosureLayout.kHoverEpsilon)
				Widgets.drawFaded(row, Theme.accentFaint, hoverT);

			float lit = FastMath.max(open, hoverT);

			Rect caretRect = RectMath.create(RectMath.x(row) + DisclosureLayout.kCaretInset, RectMath.y(row) + LayoutMath.centerOffset(RectMath.h(row), DisclosureLayout.kCaretBox), DisclosureLayout.kCaretBox, DisclosureLayout.kCaretBox);
			Widgets.drawCaret(caretRect, open, lit);

			float trailingW = string.IsNullOrEmpty(trailing) ? 0f : FastMath.min(UiText.measuredWidth(Theme.valueLabel, trailing), RectMath.w(row) * SectionHeaderLayout.kTrailingShare) + DisclosureLayout.kTrailingGap;

			float labelX = RectMath.xMax(caretRect) + DisclosureLayout.kCaretGap;

			float labelW = FastMath.max(0f, FastMath.min(UiText.measuredWidth(Theme.valueLabel, text), RectMath.xMax(row) - trailingW - labelX));

			Rect labelRect = RectMath.create(labelX, RectMath.y(row), labelW, RectMath.h(row));
			UiText.fittedLabel(labelRect, text, Theme.valueLabel, TextAnchor.MiddleLeft);

			if (lit > DisclosureLayout.kHoverEpsilon)
			{
				Color prev = Gui.color;
				Gui.color = ColorMath.fade(prev, lit);

				UiText.fittedLabel(labelRect, text, Theme.label, TextAnchor.MiddleLeft);

				Gui.color = prev;
			}

			if (trailingW > 0f)
				UiText.ellipsizedLabel(RectMath.create(RectMath.xMax(row) - trailingW + DisclosureLayout.kTrailingGap, RectMath.y(row), trailingW - DisclosureLayout.kTrailingGap, RectMath.h(row)), trailing, Theme.valueLabel, TextAnchor.MiddleRight);
		}

		// the trailing field gap sits inside the section so it eases with the body but outside the inset
		private static void beginInset(float y, ref float w, in MenuAnimation.SectionScope body)
		{
			float insetH = MenuAnimation.fullHeight(body) - Widgets.kFieldGap;
			if (insetH > 0f)
				Theme.drawInsetBg(RectMath.create(MenuHost.m_blockOffset.x, MenuHost.m_blockOffset.y + y, w, insetH));

			Widgets.indentBody(ref w, DisclosureLayout.kInsetPadX);
			w -= DisclosureLayout.kInsetPadX;
		}

		private static void endInset(ref float w)
		{
			w += DisclosureLayout.kInsetPadX;
			Widgets.indentBody(ref w, -DisclosureLayout.kInsetPadX);
		}

		private static void indentBody(ref float w, float indent)
		{
			MenuHost.m_blockOffset.x += indent;
			w -= indent;
		}

		// the title and rule line stop short of this so the trailing text never overlaps them
		private static float headerTrailingWidth(Rect rect, string trailing)
		{
			if (string.IsNullOrEmpty(trailing))
				return 0f;

			return FastMath.min(UiText.measuredWidth(Theme.valueLabel, trailing), RectMath.w(rect) * SectionHeaderLayout.kTrailingShare) + SectionHeaderLayout.kPadX + SectionHeaderLayout.kRuleGap;
		}

		private static void drawTacticalHeader(Rect rect, string text, bool hovered)
		{
			if (!Gui.repainting)
				return;

			string upper = UiText.cachedMarkup(Widgets.kUpperHeaders, text, string.Empty, string.Empty, true);

			GUIStyle style = hovered ? Theme.tacticalHover : Theme.tacticalHeader;

			float labelW = FastMath.min(UiText.measuredWidth(style, upper), RectMath.w(rect));
			UiText.fittedLabel(RectMath.create(RectMath.x(rect), RectMath.y(rect), labelW, RectMath.h(rect)), upper, style, TextAnchor.MiddleLeft);

			float ruleX = RectMath.x(rect) + labelW + SectionHeaderLayout.kRuleGap;

			float ruleW = RectMath.xMax(rect) - ruleX;
			if (!(ruleW > 0f))
				return;

			float ruleY = RectMath.y(rect) + (RectMath.h(rect) - SectionHeaderLayout.kRuleH) * 0.5f;
			Gui.drawStroke(RectMath.create(ruleX, ruleY, ruleW, SectionHeaderLayout.kRuleH), Theme.divider);
		}

		private static void drawCaret(Rect rect, float open, float hoverT)
		{
			Texture2D caret = Theme.disclosureCaret(open, false);
			if (Live.isUnityAlive(caret))
				Gui.drawTextureSnapped(rect, caret);

			if (hoverT <= DisclosureLayout.kHoverEpsilon)
				return;

			Texture2D hoverCaret = Theme.disclosureCaret(open, true);
			if (!Live.isUnityAlive(hoverCaret))
				return;

			Widgets.drawFaded(rect, hoverCaret, hoverT);
		}
	}
}
