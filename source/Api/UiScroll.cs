using System;
using UnityEngine;

namespace Trellis
{
	// scroll state, bars and the lists built on them
	internal static class UiScroll
	{
		internal static float gutter => Widgets.ScrollLayout.kGutter;
		internal static float speed => Widgets.ScrollLayout.kSpeed;

		internal static void ease(ref ScrollState state, float scrollMax) => Widgets.easeScroll(ref state, scrollMax);
		internal static void syncDrag(ref ScrollState state, float resultY) => Widgets.syncDrag(ref state, resultY);
		internal static bool visible(ref ScrollState state, float scrollMax) => Widgets.scrollbarVisible(ref state, scrollMax);
		internal static void reserve(float x, float y, float barW, float trackH, float scrollMax) => Widgets.reserveScrollbar(x, y, barW, trackH, scrollMax);
		internal static void read(Rect viewRect, ref ScrollState state, float scrollMax, float scrollSpeed, Vector2 mousePosition) => Widgets.readScrollAt(viewRect, ref state, scrollMax, scrollSpeed, mousePosition);
		internal static bool bar(float x, float y, float barW, float trackH, float scrollMax, ref ScrollState state) => Widgets.drawScrollbar(x, y, barW, trackH, scrollMax, ref state);
		internal static bool bar(float x, float y, float barW, float trackH, float scrollMax, ref ScrollState state, Vector2 mousePosition, bool mouseDown, float alpha = 1f) => Widgets.drawScrollbarAt(x, y, barW, trackH, scrollMax, ref state, mousePosition, mouseDown, alpha);
		internal static ScrollRegion region(Rect area, ref ScrollState state, float contentH, float barW, float gutter, float scrollSpeed, bool interactive, Action<ScrollRegion> drawContent, bool reserveBar = false) => Widgets.drawScrollRegion(area, ref state, contentH, barW, gutter, scrollSpeed, interactive, drawContent, reserveBar);
		internal static int list(float x, float y, float w, float listH, ref ScrollState scroll, int count, Func<int, float> rowHeight, float barW, float gutter, float scrollSpeed, bool interactive, Action<int, Rect, float> drawRow, bool reserveBar = false) => Widgets.drawSelectableList(x, y, w, listH, ref scroll, count, rowHeight, barW, gutter, scrollSpeed, interactive, drawRow, reserveBar);
		internal static void revealRow(ref ScrollState scroll, int count, int index, bool compact) => Widgets.revealSelectRow(ref scroll, count, index, compact);
	}
}
