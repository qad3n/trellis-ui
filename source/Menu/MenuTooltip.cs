using UnityEngine;

namespace Trellis
{
	internal static partial class MenuHost
	{
		private static TooltipFrame m_frameTooltip;
		private static TooltipFrame m_activeTooltip;
		private static MotionValue m_tooltipLight;
		private static Vector2 m_dwellPoint;
		private static float m_dwellStart;
		private static Vector2 m_tooltipPosition;
		private static TooltipFrame m_tooltipMeasured;
		private static int m_tooltipVersion = -1;
		private static Vector2 m_tooltipTextSize;
		private static Vector2 m_tooltipPreviewSize;

		private static class Tooltip
		{
			internal const float kMaxW = 300f;
			internal const float kPadX = 10f;
			internal const float kPadY = 6f;
			internal const float kOffsetXY = 16f;
			internal const float kScreenMargin = 4f;
			internal const float kMeasureSlack = 2f;
			internal const float kSectionGap = 4f;
		}

		// owner is what the dwell keys on so typing into a hovered field updates the preview without restarting it
		private struct TooltipFrame
		{
			internal string m_text;
			internal string m_preview;
			internal string m_owner;
		}

		internal static bool tooltipPending => MenuHost.m_frameTooltip.m_owner != null;

		internal static bool hoverTooltip(Rect rect, string tooltip)
		{
			if (tooltip == null || !RectMath.contains(rect, MenuHost.scrollMouse))
				return false;

			MenuHost.showTooltip(tooltip);
			return true;
		}

		internal static void showTooltip(string tooltip)
		{
			if (tooltip == null)
				return;

			MenuHost.m_frameTooltip.m_text = tooltip;
			MenuHost.m_frameTooltip.m_owner = tooltip;
			MenuHost.m_tooltipPosition = MenuHost.mousePosition;
		}

		// the row description keeps ownership so moving from label onto field only grows the tooltip
		internal static void showPreview(string markup, string owner)
		{
			if (string.IsNullOrEmpty(markup))
				return;

			MenuHost.m_frameTooltip.m_preview = markup;
			if (MenuHost.m_frameTooltip.m_text == null)
				MenuHost.m_frameTooltip.m_owner = owner;
			MenuHost.m_tooltipPosition = MenuHost.mousePosition;
		}

		private static void drawTooltip()
		{
			float now = Gui.unscaledTime;

			bool pending = MenuHost.tooltipPending;
			if (pending)
			{
				if (MenuHost.m_frameTooltip.m_owner != MenuHost.m_activeTooltip.m_owner)
				{
					MenuHost.m_tooltipLight = MotionValue.at(0f);
					MenuHost.restartDwell(now);
				}

				MenuHost.m_activeTooltip = MenuHost.m_frameTooltip;
			}

			if (VectorMath.distance2D(MenuHost.mousePosition.x - MenuHost.m_dwellPoint.x, MenuHost.mousePosition.y - MenuHost.m_dwellPoint.y) > MotionTokens.kFixation.m_radiusPx)
				MenuHost.restartDwell(now);

			bool showing = pending && now - MenuHost.m_dwellStart >= MotionTokens.kFixation.m_seconds;

			float drawAlpha = Fade.cut(ref MenuHost.m_tooltipLight, showing, AnimationConfig.tooltipEnabled);

			if (MenuHost.m_activeTooltip.m_owner == null)
				return;

			if (drawAlpha <= 0f)
			{
				if (!pending)
					MenuHost.m_activeTooltip = default;
				return;
			}

			MenuHost.measureTooltip();

			TooltipFrame active = MenuHost.m_activeTooltip;

			Vector2 textSize = MenuHost.m_tooltipTextSize;

			Vector2 previewSize = MenuHost.m_tooltipPreviewSize;

			float gap = active.m_text != null && active.m_preview != null ? Tooltip.kSectionGap : 0f;

			float tooltipW = FastMath.max(textSize.x, previewSize.x) + Tooltip.kPadX * 2f;

			float tooltipH = textSize.y + gap + previewSize.y + Tooltip.kPadY * 2f;

			float tooltipX = MenuHost.m_tooltipPosition.x + Tooltip.kOffsetXY;

			float tooltipY = MenuHost.m_tooltipPosition.y + Tooltip.kOffsetXY;

			if (tooltipX + tooltipW > MenuHost.kBaseWidth - Tooltip.kScreenMargin)
				tooltipX = MenuHost.m_tooltipPosition.x - tooltipW - Tooltip.kScreenMargin;

			if (tooltipY + tooltipH > MenuHost.designH - Tooltip.kScreenMargin)
				tooltipY = MenuHost.m_tooltipPosition.y - tooltipH - Tooltip.kScreenMargin;

			Color prev = Gui.color;

			Gui.color = ColorMath.withAlpha(ColorMath.kWhite, drawAlpha * MenuHost.displayFade);

			Theme.drawTooltipBg(RectMath.create(tooltipX, tooltipY, tooltipW, tooltipH));

			float textX = tooltipX + Tooltip.kPadX;

			float textY = tooltipY + Tooltip.kPadY;
			MenuHost.drawTooltipSection(textX, textY, textSize, active.m_text, Theme.tooltipLabel);
			MenuHost.drawTooltipSection(textX, textY + textSize.y + gap, previewSize, active.m_preview, Theme.tooltipPreview);
			Gui.color = prev;
		}

		private static void measureTooltip()
		{
			TooltipFrame active = MenuHost.m_activeTooltip;
			if (active.m_text == MenuHost.m_tooltipMeasured.m_text && active.m_preview == MenuHost.m_tooltipMeasured.m_preview && MenuHost.m_tooltipVersion == Theme.layoutVersion)
				return;

			MenuHost.m_tooltipTextSize = MenuHost.measureTooltipSection(active.m_text, Theme.tooltipLabel);
			MenuHost.m_tooltipPreviewSize = MenuHost.measureTooltipSection(active.m_preview, Theme.tooltipPreview);
			MenuHost.m_tooltipMeasured = active;
			MenuHost.m_tooltipVersion = Theme.layoutVersion;
		}

		private static Vector2 measureTooltipSection(string text, GUIStyle style)
		{
			if (text == null)
				return VectorMath.kZero2;

			GUIContent content = Theme.kScratchContent;
			content.text = text;
			style.wordWrap = false;

			Vector2 natural = style.CalcSize(content);

			style.wordWrap = true;

			float textW = FastMath.min(natural.x + Tooltip.kMeasureSlack, Tooltip.kMaxW);
			return VectorMath.v2(textW, style.CalcHeight(content, textW) + Tooltip.kMeasureSlack);
		}

		private static void drawTooltipSection(float x, float y, Vector2 size, string text, GUIStyle style)
		{
			if (text == null)
				return;

			GUIContent content = Theme.kScratchContent;
			content.text = text;
			Gui.label(RectMath.create(x, y, size.x, size.y), content, style);
		}

		private static void restartDwell(float now)
		{
			MenuHost.m_dwellPoint = MenuHost.mousePosition;
			MenuHost.m_dwellStart = now;
		}
	}
}
