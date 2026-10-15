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
			internal const float kIndentX = 4f;
			internal const float kCaretBox = 10f;
			internal const float kCaretGap = 7f;
			internal const float kBottomGap = 6f;
			internal const float kHoverEpsilon = 0.001f;
		}

		private static float disclosureHeight => DisclosureLayout.kH + DisclosureLayout.kBottomGap;

		// trailing names what the block is editing, right aligned in the header
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

				Widgets.fittedLabel(RectMath.create(RectMath.x(rect) + SectionHeaderLayout.kPadX, RectMath.y(rect), RectMath.w(rect) - SectionHeaderLayout.kPadX - trailingW, RectMath.h(rect)), text, Theme.sectionHeader, TextAnchor.MiddleLeft);

				Gui.drawStroke(Gui.bottomEdge(rect, SectionHeaderLayout.kBorderH), Theme.titleAccent);
			}

			if (trailingW > 0f)
				Widgets.ellipsizedLabel(RectMath.create(RectMath.xMax(rect) - trailingW, RectMath.y(rect), trailingW - SectionHeaderLayout.kPadX, RectMath.h(rect)), trailing, Theme.valueLabel, TextAnchor.MiddleRight);
		}

		internal static MenuAnimation.SectionScope beginDisclosure(ref float y, float w, string animId, string text, ref bool expanded, string bodyKey, bool locked = false)
		{
			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, RowPrefix.kDisclosure, animId);

			Widgets.beginDisabled(locked);

			Rect headerRect = RectMath.create(MenuHost.m_blockOffset.x, MenuHost.m_blockOffset.y + y, w, SectionHeaderLayout.kH);
			if (Widgets.drawCollapsibleHeader(headerRect, animId, text, expanded))
				expanded = !expanded;

			Widgets.endDisabled();

			y += Widgets.disclosureHeight;
			Widgets.endRow(ref y, ref row);
			return MenuAnimation.beginSection(ref y, bodyKey, expanded && !locked);
		}

		// a widgetId outside a block pass advances every pass and never eases
		internal static bool drawCollapsibleHeader(Rect rect, string animId, string text, bool expanded)
		{
			bool interactive = Gui.enabled;

			Rect row = RectMath.create(RectMath.x(rect) + DisclosureLayout.kIndentX, RectMath.y(rect), RectMath.w(rect) - DisclosureLayout.kIndentX, DisclosureLayout.kH);

			bool hovered = interactive && RectMath.contains(row, MenuHost.scrollMouse);

			float open = MenuAnimation.disclosureProgress(animId, expanded);

			float hoverT = MenuAnimation.disclosureHover(animId, hovered);
			if (Gui.repainting)
			{
				Color prev = Gui.color;
				if (hoverT > DisclosureLayout.kHoverEpsilon)
				{
					Gui.color = ColorMath.fade(prev, hoverT);
					Gui.drawTextureSnapped(row, Theme.disclosureHover);
					Gui.color = prev;
				}

				Rect caretRect = RectMath.create(RectMath.x(row) + DisclosureLayout.kCaretGap, RectMath.y(row) + LayoutMath.centerOffset(RectMath.h(row), DisclosureLayout.kCaretBox), DisclosureLayout.kCaretBox, DisclosureLayout.kCaretBox);
				Widgets.drawCaret(caretRect, open, hoverT);

				float labelX = RectMath.xMax(caretRect) + DisclosureLayout.kCaretGap;

				Rect labelRect = RectMath.create(labelX, RectMath.y(row), RectMath.xMax(row) - labelX, RectMath.h(row));
				Widgets.fittedLabel(labelRect, text, Theme.valueLabel, TextAnchor.MiddleLeft);

				if (hoverT > DisclosureLayout.kHoverEpsilon)
				{
					Gui.color = ColorMath.fade(prev, hoverT);

					Widgets.fittedLabel(labelRect, text, Theme.label, TextAnchor.MiddleLeft);

					Gui.color = prev;
				}
			}
			return hovered && MenuHost.mouseDown0;
		}

		// the title and rule line stop short of this so the trailing text never overlaps them
		private static float headerTrailingWidth(Rect rect, string trailing)
		{
			if (string.IsNullOrEmpty(trailing))
				return 0f;

			return FastMath.min(Widgets.measuredWidth(Theme.valueLabel, trailing), RectMath.w(rect) * SectionHeaderLayout.kTrailingShare) + SectionHeaderLayout.kPadX + SectionHeaderLayout.kRuleGap;
		}

		private static void drawTacticalHeader(Rect rect, string text, bool hovered)
		{
			if (!Gui.repainting)
				return;

			string upper = Widgets.markup(Widgets.kUpperHeaders, text, string.Empty, string.Empty, true);

			GUIStyle style = hovered ? Theme.tacticalHover : Theme.tacticalHeader;

			float labelW = FastMath.min(Widgets.measuredWidth(style, upper), RectMath.w(rect));
			Widgets.fittedLabel(RectMath.create(RectMath.x(rect), RectMath.y(rect), labelW, RectMath.h(rect)), upper, style, TextAnchor.MiddleLeft);

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
