using UnityEngine;
using static Trellis.MenuHost;

namespace Trellis
{
	internal static partial class Widgets
	{
		// a key that moves with the rects mints a new entry every frame
		private static int m_scrollIds;

		internal struct ScrollState
		{
			internal Vector2 m_position;
			internal Vector2 m_target;
			internal bool m_dragging;
			internal float m_grabOffset;
			internal MotionValue m_scrollMotion;
			internal float m_shownY;
			internal float m_thumbH;
			internal float m_thumbOffset;
			internal float m_trackH;
			internal MotionValue m_thumbMotion;
			internal int m_animId;

			internal void reset()
			{
				m_position = VectorMath.kZero2;
				m_target = VectorMath.kZero2;
				m_dragging = false;
				m_grabOffset = 0f;
				m_scrollMotion = MotionValue.at(0f);
				m_shownY = 0f;
				m_thumbH = 0f;
				m_thumbOffset = 0f;
				m_trackH = 0f;
				m_thumbMotion = MotionValue.at(0f);
			}

			internal void snapY(float y)
			{
				m_position.y = y;
				m_target.y = y;
			}
		}

		private static class ScrollbarLayout
		{
			internal const float kMinThumbH = 20f;
			internal const float kHitPadX = 6f;
			internal const float kTrackEpsilon = 0.1f;
			internal const int kHoverSalt = 0x5C8B;
			internal const int kPresenceSalt = 0x3A17;
			internal const float kHoverEpsilon = 0.001f;
			internal const float kPresenceEpsilon = 0.004f;
			internal const float kOpaqueAlpha = 0.999f;
		}

		internal static void easeScroll(ref ScrollState state, float scrollMax)
		{
			state.m_target.y = FastMath.clamp(state.m_target.y, 0f, scrollMax);

			bool moved = state.m_position.y != state.m_shownY;

			state.m_position.y = FastMath.clamp(state.m_position.y, 0f, scrollMax);
			if (!AnimationConfig.scrollEnabled || state.m_dragging)
			{
				state.m_position.y = state.m_target.y;
				state.m_shownY = state.m_target.y;
				state.m_scrollMotion.snap(state.m_target.y);
				return;
			}

			if (moved || state.m_position.y != state.m_shownY)
				state.m_scrollMotion.snap(state.m_position.y);

			state.m_scrollMotion.reel(state.m_target.y, MotionProfile.hand(1f, true));

			float shown = FastMath.clamp(state.m_scrollMotion.sample(), 0f, scrollMax);
			state.m_position.y = shown;
			state.m_shownY = shown;
		}

		internal static void readScrollAt(Rect viewRect, ref ScrollState state, float scrollMax, float scrollSpeed, Vector2 mousePosition)
		{
			Widgets.applyWheel(viewRect, ref state, scrollMax, scrollSpeed, mousePosition);
			Widgets.easeScroll(ref state, scrollMax);
		}

		// first scrollable area to take the wheel consumes it so inner areas have to run before outer ones
		internal static void applyWheel(Rect viewRect, ref ScrollState state, float scrollMax, float scrollSpeed, Vector2 mousePosition)
		{
			if (!RectMath.contains(viewRect, mousePosition))
				return;

			float delta = MenuHost.scrollDelta;
			if (delta != 0f && scrollMax > 0f)
			{
				state.m_target.y -= delta * scrollSpeed;

				MenuHost.consumeScroll();
			}

			if (Event.current.type != EventType.ScrollWheel)
				return;

			Event.current.Use();
		}

		internal static void syncDrag(ref ScrollState state, float resultY)
		{
			if (FastMath.approximately(resultY, state.m_position.y))
				return;

			state.snapY(resultY);
		}

		internal static bool drawScrollbar(float x, float y, float barW, float trackH, float scrollMax, ref ScrollState state)
		{
			bool interacted = Widgets.drawScrollbarCore(x, y, barW, trackH, scrollMax, ref state, MenuHost.scrollMouse, MenuHost.rawMouseDown0, 1f);
			Widgets.easeScroll(ref state, scrollMax);
			return interacted;
		}

		internal static void reserveScrollbar(float x, float y, float barW, float trackH, float scrollMax) => Widgets.reserveScrollbarAt(x, y, barW, trackH, scrollMax, MenuHost.scrollMouse);
		internal static bool drawScrollbarAt(float x, float y, float barW, float trackH, float scrollMax, ref ScrollState state, Vector2 mousePosition, bool mouseDown, float alpha = 1f) => Widgets.drawScrollbarCore(x, y, barW, trackH, scrollMax, ref state, mousePosition, mouseDown, alpha);
		internal static bool scrollbarVisible(ref ScrollState state, float scrollMax) => Widgets.scrollbarPresence(ref state, scrollMax) > ScrollbarLayout.kPresenceEpsilon;
		private static void readScroll(Rect viewRect, ref ScrollState state, float scrollMax, float scrollSpeed) => Widgets.readScrollAt(viewRect, ref state, scrollMax, scrollSpeed, MenuHost.scrollMouse);

		private static void reserveScrollbarAt(float x, float y, float barW, float trackH, float scrollMax, Vector2 mousePosition)
		{
			if (scrollMax <= 0f)
				return;
			if (!MenuHost.rawMouseDown0)
				return;

			Rect hit = RectMath.create(x - ScrollbarLayout.kHitPadX, y, barW + ScrollbarLayout.kHitPadX * 2f, trackH);
			if (!RectMath.contains(hit, mousePosition))
				return;

			MenuHost.consumeMouseDown0();
		}

		private static int trackKey(ref ScrollState state, int salt)
		{
			if (state.m_animId == 0)
				state.m_animId = ++Widgets.m_scrollIds;
			return unchecked((state.m_animId * Widgets.kKeyMix) ^ salt);
		}

		private static float scrollbarPresence(ref ScrollState state, float scrollMax) => MenuAnimation.presenceProgress(Widgets.trackKey(ref state, ScrollbarLayout.kPresenceSalt), scrollMax > 0f, MotionMath.extent(MenuHost.kScrollThickness, state.m_trackH));

		private static float easeThumb(ref ScrollState state, float trackH, float targetH, bool scrollable)
		{
			if (!scrollable)
			{
				if (state.m_thumbH <= 0f)
					state.m_thumbH = targetH;
				return state.m_thumbH;
			}

			bool instant = !AnimationConfig.scrollEnabled || state.m_dragging || state.m_thumbH <= 0f || FastMath.abs(trackH - state.m_trackH) > ScrollbarLayout.kTrackEpsilon;

			state.m_trackH = trackH;
			if (instant)
			{
				state.m_thumbH = targetH;
				state.m_thumbMotion.snap(targetH);
				return targetH;
			}

			state.m_thumbMotion.reel(targetH, MotionProfile.hand(1f, true));
			state.m_thumbH = state.m_thumbMotion.sample();
			return state.m_thumbH;
		}

		private static bool drawScrollbarCore(float x, float y, float barW, float trackH, float scrollMax, ref ScrollState state, Vector2 mousePosition, bool mouseDown, float alpha)
		{
			bool scrollable = scrollMax > 0f;
			if (!scrollable)
				state.m_dragging = false;

			float presence = Widgets.scrollbarPresence(ref state, scrollMax);
			if (presence <= ScrollbarLayout.kPresenceEpsilon)
				return false;

			alpha *= presence;

			float contentH = scrollMax + trackH;

			float targetH = FastMath.max(ScrollbarLayout.kMinThumbH, trackH * (trackH / contentH));

			float thumbH = Widgets.easeThumb(ref state, trackH, targetH, scrollable);

			float scrollRange = trackH - thumbH;

			float thumbOffset;
			if (scrollable)
			{
				thumbOffset = state.m_position.y / scrollMax * scrollRange;
				state.m_thumbOffset = thumbOffset;
			}
			else
				thumbOffset = FastMath.clamp(state.m_thumbOffset, 0f, FastMath.max(0f, scrollRange));

			float thumbY = y + thumbOffset;

			Rect trackHit = RectMath.create(x - ScrollbarLayout.kHitPadX, y, barW + ScrollbarLayout.kHitPadX * 2f, trackH);

			Rect thumbHit = RectMath.create(x - ScrollbarLayout.kHitPadX, thumbY, barW + ScrollbarLayout.kHitPadX * 2f, thumbH);

			int hoverKey = Widgets.trackKey(ref state, ScrollbarLayout.kHoverSalt);

			float hoverT = MenuAnimation.hoverProgress(hoverKey, scrollable && (state.m_dragging || RectMath.contains(thumbHit, mousePosition)));

			Color prev = Gui.color;
			if (alpha < ScrollbarLayout.kOpaqueAlpha)
				Gui.color = ColorMath.fade(prev, alpha);

			Rect thumbRect = Gui.snapRect(RectMath.create(x, thumbY, barW, thumbH));

			Texture2D trackTexture = Theme.scrollTrack;

			float aboveH = RectMath.y(thumbRect) - y;
			if (aboveH > 0f)
				Gui.drawTextureSnapped(RectMath.create(x, y, barW, aboveH), trackTexture);

			float belowY = RectMath.yMax(thumbRect);

			float belowH = y + trackH - belowY;
			if (belowH > 0f)
				Gui.drawTextureSnapped(RectMath.create(x, belowY, barW, belowH), trackTexture);

			Gui.drawTexture(thumbRect, Theme.scrollThumb);

			if (hoverT > ScrollbarLayout.kHoverEpsilon)
			{
				Color faded = Gui.color;
				Gui.color = ColorMath.fade(faded, hoverT);
				Gui.drawTexture(thumbRect, Theme.thumbHover);
				Gui.color = faded;
			}
			Gui.color = prev;

			if (!scrollable)
				return false;

			bool userInteracted = false;
			if (mouseDown && RectMath.contains(trackHit, mousePosition))
			{
				if (RectMath.contains(thumbHit, mousePosition))
				{
					state.m_dragging = true;
					state.m_grabOffset = mousePosition.y - thumbY;
				}
				else
				{
					float clickRatio = FastMath.clamp01((mousePosition.y - y - thumbH * 0.5f) / scrollRange);
					state.m_target.y = clickRatio * scrollMax;
					state.m_dragging = true;
					state.m_grabOffset = thumbH * 0.5f;
				}

				userInteracted = true;

				MenuHost.consumeMouseDown0();
			}

			if (state.m_dragging)
			{
				if (MenuHost.mouseHeld0)
				{
					float nextY = mousePosition.y - state.m_grabOffset;

					float newRatio = FastMath.clamp01((nextY - y) / scrollRange);

					float newY = newRatio * scrollMax;
					state.m_target.y = newY;
					state.m_position.y = newY;

					userInteracted = true;
				}
				else
					state.m_dragging = false;
			}
			return userInteracted;
		}
	}
}
