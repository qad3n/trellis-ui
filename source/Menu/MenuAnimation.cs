using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Trellis
{
	[Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
	internal static class MenuAnimation
	{
		private const float kPressAlpha = 0.25f;
		private const float kMinReveal = 1f;

		private static readonly Dictionary<int, LightEntry> kHoverState = new Dictionary<int, LightEntry>();
		private static readonly Dictionary<int, LightEntry> kToggleState = new Dictionary<int, LightEntry>();
		private static readonly Dictionary<int, LightEntry> kEdgeState = new Dictionary<int, LightEntry>();
		private static readonly Dictionary<int, LightEntry> kPresenceState = new Dictionary<int, LightEntry>();
		private static readonly Dictionary<int, LightEntry> kPressState = new Dictionary<int, LightEntry>();
		private static readonly Dictionary<int, OpenCloseEntry> kOpenClose = new Dictionary<int, OpenCloseEntry>();
		private static readonly Dictionary<int, BlockMoveEntry> kBlockMoves = new Dictionary<int, BlockMoveEntry>();
		private static readonly Dictionary<int, BlockEntryState> kBlockEntries = new Dictionary<int, BlockEntryState>();
		private static readonly Dictionary<int, MarkerEntry> kMarkers = new Dictionary<int, MarkerEntry>();
		private static readonly Dictionary<int, ValueEntry> kDisplayedValues = new Dictionary<int, ValueEntry>();
		private static readonly Dictionary<int, SwitchEntry> kSwitches = new Dictionary<int, SwitchEntry>();
		private static readonly List<int> kGcScratch = new List<int>();
		private static readonly float[] kChildLive = new float[SectionMeasure.kMaxDepth];
		private static readonly float[] kChildSettled = new float[SectionMeasure.kMaxDepth];
		private static readonly Dictionary<int, float> kFullHeights = new Dictionary<int, float>();
		private static readonly Dictionary<(int block, string key), string> kSectionIds = new Dictionary<(int block, string key), string>();

		private static int m_sectionDepth;
		private static int m_activeSwitch;
		private static int m_gcCounter;
		private static Vector2 m_contentOrigin;
		private static float m_contentWidth = 1f;

		internal struct OutgoingScope : IDisposable
		{
			internal bool m_render;
			internal Color m_savedColor;
			internal bool m_savedEnabled;
			internal Vector2 m_savedMouse;

			public void Dispose()
			{
				if (!m_render)
					return;

				Gui.color = m_savedColor;
				Gui.enabled = m_savedEnabled;
				MenuHost.scrollMouse = m_savedMouse;
			}
		}

		internal readonly struct BlockEntryScope : IDisposable
		{
			private readonly Color m_savedColor;
			private readonly Matrix4x4 m_savedMatrix;
			private readonly bool m_restore;

			internal BlockEntryScope(Color color, Matrix4x4 matrix, bool restoreState)
			{
				m_savedColor = color;
				m_savedMatrix = matrix;
				m_restore = restoreState;
			}

			public void Dispose()
			{
				if (!m_restore)
					return;

				Gui.color = m_savedColor;
				Gui.matrix = m_savedMatrix;
			}
		}

		internal readonly struct SheetScope : IDisposable
		{
			private readonly Color m_savedColor;
			private readonly bool m_clipped;

			internal SheetScope(Color color, bool clipped)
			{
				m_savedColor = color;
				m_clipped = clipped;
			}

			public void Dispose()
			{
				if (m_clipped)
					Gui.endClip();

				Gui.color = m_savedColor;
			}
		}

		internal struct SectionScope
		{
			internal bool m_render;
			internal string m_animId;
			internal float m_startY;
			internal float m_layoutProgress;
			internal bool m_targetOpen;
			internal Color m_savedColor;
			internal bool m_clipped;
		}

		internal readonly struct TabScope : IDisposable
		{
			private readonly Color m_savedColor;
			private readonly int m_savedSwitch;

			internal TabScope(Color color, int switchKey)
			{
				m_savedColor = color;
				m_savedSwitch = switchKey;
			}

			public void Dispose()
			{
				Gui.color = m_savedColor;

				MenuAnimation.m_activeSwitch = m_savedSwitch;
			}
		}

		private static class Gc
		{
			internal const float kLifetimeSeconds = 1f;
			internal const int kFrameStride = 30;
		}

		private static class SectionMeasure
		{
			internal const int kMaxDepth = 8;
		}

		private enum OpenCloseKind : byte
		{
			Popup,
			Section,
			FadeIn
		}

		private struct LightEntry
		{
			internal MotionValue m_value;
			internal float m_lastTouched;
		}

		private struct ValueEntry
		{
			internal MotionValue m_value;
			internal float m_lastTouched;
		}

		private struct OpenCloseEntry
		{
			internal MotionValue m_light;
			internal MotionValue m_payout;
			internal bool m_targetOpen;
			internal float m_lastTouched;
			// the size of the sheet once drawn, it sets how long its light takes
			internal float m_extent;
			internal OpenCloseKind m_kind;
		}

		private struct BlockMoveEntry
		{
			internal Vector2 m_target;
			internal MotionValue m_offsetX;
			internal MotionValue m_offsetY;
			internal float m_hopSpan;
			internal bool m_armed;
			internal bool m_markHop;
		}

		private struct BlockEntryState
		{
			internal MotionValue m_light;
			internal MotionValue m_offsetX;
			internal MotionValue m_offsetY;
			internal int m_switchKey;
			internal int m_generation;
			internal float m_lastTouched;
		}

		private struct SwitchEntry
		{
			internal MotionValue m_incoming;
			internal MotionValue m_outgoing;
			internal Vector2 m_trigger;
			internal int m_prevValue;
			internal int m_generation;
			internal int m_startStep;
		}

		private struct MarkerEntry
		{
			internal Rect m_target;
			internal Rect m_origin;
			internal MotionValue m_payout;
			internal Rect m_leaving;
			internal MotionValue m_leavingLight;
			internal float m_lastTouched;
		}

		private static class AnimKeys
		{
			internal const int kMix = 397;
			internal const int kCheckboxSalt = 0x7E57;
			internal const int kCheckboxHoverSalt = 0x5EEA;
			internal const int kDisclosureSalt = 0x0D15;
			internal const int kDisclosureHoverSalt = 0x0DAE;
			internal const int kFadeSalt = 0xFAD1;
		}

		internal static bool enabled => AnimationConfig.Enabled;
		private static float restPx => MotionTokens.kSolver.m_restPx;
		private static float stepSeconds => MotionTokens.kSolver.m_stepSeconds;

		internal static void beginFrame() => MotionClock.sync();

		internal static void beginOverlay()
		{
			Theme.init();
			MenuAnimation.beginFrame();
		}

		internal static float presenceProgress(int id, bool present, float extentPx)
		{
			ref LightEntry entry = ref MenuAnimation.touchLight(MenuAnimation.kPresenceState, id, 0f);
			return Fade.release(ref entry.m_value, present, extentPx, MenuAnimation.enabled);
		}

		internal static float edgeAlpha(int windowId, bool active)
		{
			ref LightEntry entry = ref MenuAnimation.touchLight(MenuAnimation.kEdgeState, windowId, 0f);
			return Fade.cut(ref entry.m_value, active, MenuAnimation.enabled);
		}

		internal static float hoverProgress(int id, bool hovered) => MenuAnimation.sampleLight(MenuAnimation.kHoverState, id, hovered, AnimationConfig.hoverEnabled);

		internal static void endFrame()
		{
			if (!Gui.repainting)
				return;

			MenuAnimation.m_gcCounter++;
			if (MenuAnimation.m_gcCounter < Gc.kFrameStride)
				return;

			MenuAnimation.m_gcCounter = 0;

			float now = Gui.unscaledTime;
			MenuAnimation.gcLights(MenuAnimation.kHoverState, now);
			MenuAnimation.gcLights(MenuAnimation.kToggleState, now);
			MenuAnimation.gcLights(MenuAnimation.kEdgeState, now);
			MenuAnimation.gcLights(MenuAnimation.kPresenceState, now);
			MenuAnimation.gcLights(MenuAnimation.kPressState, now);
			MenuAnimation.gcFades(now);
			MenuAnimation.gcEntries(now);
			MenuAnimation.gcMarkers(now);
			MenuAnimation.gcValues(now);
		}

		internal static void reset()
		{
			MenuAnimation.kHoverState.Clear();

			MenuAnimation.kToggleState.Clear();

			MenuAnimation.kEdgeState.Clear();

			MenuAnimation.kPresenceState.Clear();

			MenuAnimation.kPressState.Clear();

			MenuAnimation.kOpenClose.Clear();

			MenuAnimation.kSwitches.Clear();

			MenuAnimation.kBlockMoves.Clear();

			MenuAnimation.kBlockEntries.Clear();

			MenuAnimation.kMarkers.Clear();

			MenuAnimation.kDisplayedValues.Clear();

			MenuAnimation.kFullHeights.Clear();

			ContentClip.reset();

			MenuAnimation.m_activeSwitch = 0;

			Array.Clear(MenuAnimation.kChildLive, 0, MenuAnimation.kChildLive.Length);
			Array.Clear(MenuAnimation.kChildSettled, 0, MenuAnimation.kChildSettled.Length);

			MenuAnimation.m_sectionDepth = 0;

			MenuRowFlow.reset();
		}

		internal static bool drawButton(Rect rect, string text, GUIStyle style, Vector2 mouse)
		{
			bool hovered = Gui.enabled && RectMath.contains(rect, mouse);

			bool clicked = hovered && MenuHost.mouseDown0;

			bool pressed = hovered && MenuHost.mouseHeld0;

			int id = MenuAnimation.idFor(rect, text);

			bool hoverTarget = hovered && !pressed;

			float hoverT = MenuAnimation.sampleLight(MenuAnimation.kHoverState, id, hoverTarget, AnimationConfig.hoverEnabled);

			float pressT = MenuAnimation.sampleLight(MenuAnimation.kPressState, id, pressed, AnimationConfig.pressEnabled);

			if (!Gui.repainting)
				return clicked;

			Theme.WidgetChrome chrome = Theme.chromeOf(style);
			if (chrome != null)
				Theme.drawChrome(rect, chrome, hoverT, pressed);

			Vector2 savedOffset = style.contentOffset;
			try
			{
				if (pressed)
					style.contentOffset = VectorMath.add(savedOffset, VectorMath.v2(0f, 1f));
				Widgets.fittedLabel(rect, text, style, style.alignment);
			}
			finally
			{
				style.contentOffset = savedOffset;
			}

			if (pressT > 0f)
				Theme.drawRoundedTint(rect, ColorMath.rgba(0f, 0f, 0f, MenuAnimation.kPressAlpha * pressT * Gui.color.a));
			return clicked;
		}

		internal static float disclosureProgress(string animId, bool expanded) => MenuAnimation.sampleLight(MenuAnimation.kToggleState, MenuAnimation.animKey(animId, AnimKeys.kDisclosureSalt), expanded, MenuAnimation.enabled);
		internal static float disclosureHover(string animId, bool hovered) => MenuAnimation.hoverProgress(MenuAnimation.animKey(animId, AnimKeys.kDisclosureHoverSalt), hovered);
		internal static void snapCheckbox(string toggleKey, bool value) => MenuAnimation.snapToggle(MenuAnimation.animKey(toggleKey, AnimKeys.kCheckboxSalt), value);
		internal static void snapDisclosure(string animId, bool expanded) => MenuAnimation.snapToggle(MenuAnimation.animKey(animId, AnimKeys.kDisclosureSalt), expanded);

		internal static void drawCheckbox(Rect rect, bool value, bool hovered, string toggleKey)
		{
			float onT = MenuAnimation.sampleLight(MenuAnimation.kToggleState, MenuAnimation.animKey(toggleKey, AnimKeys.kCheckboxSalt), value, MenuAnimation.enabled);

			float hoverT = MenuAnimation.sampleLight(MenuAnimation.kHoverState, MenuAnimation.animKey(toggleKey, AnimKeys.kCheckboxHoverSalt), hovered, AnimationConfig.hoverEnabled);
			Theme.drawCheckbox(rect, onT, hoverT);
		}

		internal static void noteOpened(string id) => MenuAnimation.openEntry(id.GetHashCode(), OpenCloseKind.Popup);

		internal static void close(string id)
		{
			ref OpenCloseEntry entry = ref MenuAnimation.openClose(id);
			if (Unsafe.IsNullRef(ref entry))
				return;

			entry.m_targetOpen = false;

			entry.m_light.vent(0f, MenuAnimation.sheetLight(ref entry));
			if (entry.m_kind == OpenCloseKind.Section)
				entry.m_payout.vent(0f, MenuAnimation.sectionPayout(id.GetHashCode()));
		}

		internal static void beginMeasure()
		{
			MenuAnimation.m_sectionDepth = 0;
			MenuAnimation.kChildLive[0] = 0f;
			MenuAnimation.kChildSettled[0] = 0f;
		}

		internal static float consumeAdjust()
		{
			float settledAdjust = MenuAnimation.kChildSettled[0] - MenuAnimation.kChildLive[0];

			MenuAnimation.kChildLive[0] = 0f;
			MenuAnimation.kChildSettled[0] = 0f;
			MenuAnimation.m_sectionDepth = 0;
			return settledAdjust;
		}

		internal static void noteChild(float live, float settled)
		{
			int depth = FastMath.min(MenuAnimation.m_sectionDepth, SectionMeasure.kMaxDepth - 1);
			MenuAnimation.kChildLive[depth] += live;
			MenuAnimation.kChildSettled[depth] += settled;
		}

		internal static void snapClose(string id)
		{
			ref OpenCloseEntry entry = ref MenuAnimation.openClose(id);
			if (Unsafe.IsNullRef(ref entry))
				return;

			entry.m_targetOpen = false;
			entry.m_light.snap(0f);
			entry.m_payout.snap(0f);
		}

		internal static bool shouldRender(string id)
		{
			if (!MenuAnimation.tryOpenClose(id, out OpenCloseEntry entry))
				return false;

			return entry.m_targetOpen || !MenuAnimation.dark(ref entry);
		}

		internal static bool isClosingOrClosed(string id)
		{
			if (!MenuAnimation.tryOpenClose(id, out OpenCloseEntry entry))
				return true;

			return !entry.m_targetOpen;
		}

		internal static bool consumeClosed(string id)
		{
			if (id == null)
				return false;

			int key = id.GetHashCode();

			ref OpenCloseEntry entry = ref CollectionsMarshal.GetValueRefOrNullRef(MenuAnimation.kOpenClose, key);
			if (Unsafe.IsNullRef(ref entry))
				return false;
			if (entry.m_targetOpen || !MenuAnimation.dark(ref entry))
				return false;

			MenuAnimation.kOpenClose.Remove(key);
			return true;
		}

		internal static float alphaOf(string id)
		{
			ref OpenCloseEntry entry = ref MenuAnimation.openClose(id);
			if (Unsafe.IsNullRef(ref entry))
				return 0f;

			return entry.m_light.sampleLight();
		}

		internal static SheetScope beginSheet(string id, float extentPx)
		{
			Color prev = Gui.color;

			ref OpenCloseEntry entry = ref MenuAnimation.openClose(id);
			if (Unsafe.IsNullRef(ref entry))
				return new SheetScope(prev, false);

			MenuAnimation.sizeSheet(ref entry, extentPx);

			Gui.color = ColorMath.fade(prev, entry.m_light.sampleLight());
			return new SheetScope(prev, false);
		}

		internal static SheetScope beginReveal(string id, Rect popupRect, bool dropUp)
		{
			Color prev = Gui.color;

			ref OpenCloseEntry entry = ref MenuAnimation.openClose(id);
			if (Unsafe.IsNullRef(ref entry))
				return new SheetScope(prev, false);

			MenuAnimation.sizeSheet(ref entry, MotionMath.extent(RectMath.w(popupRect), RectMath.h(popupRect)));

			float height = RectMath.h(popupRect);
			entry.m_payout.retune(MotionProfile.payout(MotionMath.extent(RectMath.w(popupRect), MenuHost.kDropdownItemH), height, AnimationConfig.popupsEnabled));

			float paid = FastMath.clamp01(entry.m_payout.sample());

			Gui.color = ColorMath.fade(prev, entry.m_light.sampleLight());
			if (paid >= 1f || !entry.m_targetOpen)
				return new SheetScope(prev, false);

			float shown = FastMath.max(MenuAnimation.kMinReveal, height * paid);

			float hidden = height - shown;

			Rect clip = dropUp ? RectMath.create(RectMath.x(popupRect), RectMath.yMax(popupRect) - shown, RectMath.w(popupRect), shown) : RectMath.create(RectMath.x(popupRect), RectMath.y(popupRect), RectMath.w(popupRect), shown);

			Vector2 keepAbsolute = VectorMath.v2(-RectMath.x(clip), -RectMath.y(clip) + (dropUp ? hidden : -hidden));
			Gui.beginClip(clip, keepAbsolute);
			return new SheetScope(prev, true);
		}

		internal static void noteSwitch(string key, int fromValue, int toValue)
		{
			if (key == null || fromValue == toValue)
				return;

			MenuAnimation.kBlockMoves.Clear();

			if (!AnimationConfig.tabsEnabled)
				return;

			ref SwitchEntry entry = ref CollectionsMarshal.GetValueRefOrAddDefault(MenuAnimation.kSwitches, key.GetHashCode(), out bool exists);
			if (!exists)
			{
				entry.m_incoming = MotionValue.at(1f);
				entry.m_outgoing = MotionValue.at(0f);
			}

			bool rehook = exists && toValue == entry.m_prevValue && !MenuAnimation.dark(ref entry.m_outgoing);

			MotionValue departing = entry.m_incoming;

			entry.m_incoming = rehook ? entry.m_outgoing : MotionValue.at(0f);
			entry.m_outgoing = departing;

			MotionProfile profile = MotionProfile.light(true);
			entry.m_incoming.reel(1f, profile);
			entry.m_outgoing.vent(0f, profile);

			entry.m_prevValue = fromValue;
			entry.m_generation++;
			entry.m_startStep = MotionClock.step;
			entry.m_trigger = MenuHost.mousePosition;
		}

		internal static int outgoingValue(string key)
		{
			if (!AnimationConfig.tabsEnabled || key == null)
				return -1;

			ref SwitchEntry entry = ref CollectionsMarshal.GetValueRefOrNullRef(MenuAnimation.kSwitches, key.GetHashCode());
			if (Unsafe.IsNullRef(ref entry))
				return -1;

			return entry.m_outgoing.sampleLight() > 0f ? entry.m_prevValue : -1;
		}

		internal static TabScope beginSwitch(string key)
		{
			int savedSwitch = MenuAnimation.m_activeSwitch;

			Color prev = Gui.color;
			if (!AnimationConfig.tabsEnabled || key == null)
				return new TabScope(prev, savedSwitch);

			int switchKey = key.GetHashCode();

			ref SwitchEntry entry = ref CollectionsMarshal.GetValueRefOrNullRef(MenuAnimation.kSwitches, switchKey);
			if (Unsafe.IsNullRef(ref entry))
				return new TabScope(prev, savedSwitch);

			MenuAnimation.m_activeSwitch = switchKey;

			float alpha = entry.m_incoming.sampleLight();
			if (alpha < 1f)
				Gui.color = ColorMath.fade(prev, alpha);
			return new TabScope(prev, savedSwitch);
		}

		internal static OutgoingScope beginOutgoing(string key)
		{
			if (!AnimationConfig.tabsEnabled || key == null)
				return default;

			ref SwitchEntry entry = ref CollectionsMarshal.GetValueRefOrNullRef(MenuAnimation.kSwitches, key.GetHashCode());
			if (Unsafe.IsNullRef(ref entry))
				return default;

			float alpha = entry.m_outgoing.sampleLight();
			if (alpha <= 0f)
				return default;

			Color prev = Gui.color;

			bool savedEnabled = Gui.enabled;

			Vector2 savedMouse = MenuHost.scrollMouse;

			// a fading ghost must not take the wheel from the live content drawn after it
			Gui.color = ColorMath.fade(prev, alpha);
			Gui.enabled = false;
			MenuHost.scrollMouse = MenuHost.kOffscreenMouse;
			return new OutgoingScope
			{
				m_render = true,
				m_savedColor = prev,
				m_savedEnabled = savedEnabled,
				m_savedMouse = savedMouse
			};
		}

		internal static float drawSubTabs(string animKey, int currentSub, float w, float viewH, Func<int, float, float, float> drawSub)
		{
			int outgoing = MenuAnimation.outgoingValue(animKey);
			if (outgoing >= 0 && outgoing != currentSub)
			{
				using (MenuAnimation.beginOutgoing(animKey))
					drawSub(outgoing, w, viewH);
			}

			using (MenuAnimation.beginSwitch(animKey))
				return drawSub(currentSub, w, viewH);
		}

		internal static void setContent(Vector2 origin, float width)
		{
			MenuAnimation.m_contentOrigin = origin;
			MenuAnimation.m_contentWidth = FastMath.max(width, 1f);
		}

		internal static BlockEntryScope beginEntry(int blockKey, Rect blockRect)
		{
			if (!AnimationConfig.staggerEnabled || MenuAnimation.m_activeSwitch == 0 || !Gui.repainting)
				return default;

			ref SwitchEntry wave = ref CollectionsMarshal.GetValueRefOrNullRef(MenuAnimation.kSwitches, MenuAnimation.m_activeSwitch);
			if (Unsafe.IsNullRef(ref wave))
				return default;

			ref BlockEntryState state = ref CollectionsMarshal.GetValueRefOrAddDefault(MenuAnimation.kBlockEntries, blockKey, out bool exists);
			if (!exists)
			{
				state.m_light = MotionValue.at(1f);
				state.m_offsetX = MotionValue.at(0f);
				state.m_offsetY = MotionValue.at(0f);
			}
			state.m_lastTouched = Gui.unscaledTime;

			if (state.m_switchKey != MenuAnimation.m_activeSwitch || state.m_generation != wave.m_generation)
			{
				state.m_switchKey = MenuAnimation.m_activeSwitch;
				state.m_generation = wave.m_generation;
				if (!wave.m_incoming.atRest)
					MenuAnimation.launchBlock(ref state, in wave, blockRect);
			}

			float alpha = state.m_light.sampleLight();

			float offsetX = state.m_offsetX.sample();

			float offsetY = state.m_offsetY.sample();
			if (alpha >= 1f && FastMath.abs(offsetX) < MenuAnimation.restPx && FastMath.abs(offsetY) < MenuAnimation.restPx)
				return default;

			Color prev = Gui.color;

			Matrix4x4 prevMatrix = Gui.matrix;

			Gui.color = ColorMath.fade(prev, alpha);
			Gui.matrix = MatrixMath.multiply(prevMatrix, MatrixMath.translation(VectorMath.v3(offsetX, offsetY, 0f)));
			return new BlockEntryScope(prev, prevMatrix, true);
		}

		internal static SectionScope beginSection(ref float y, string key, bool visible)
		{
			string animId = MenuAnimation.sectionId(key);

			int hash = animId.GetHashCode();
			if (visible)
			{
				ref OpenCloseEntry opened = ref MenuAnimation.openEntry(hash, OpenCloseKind.Section);
				if (!(opened.m_payout.atRest && opened.m_payout.goal >= 1f))
					opened.m_payout.reel(1f, MenuAnimation.sectionPayout(hash));
			}
			else
				MenuAnimation.close(animId);

			ref OpenCloseEntry entry = ref CollectionsMarshal.GetValueRefOrNullRef(MenuAnimation.kOpenClose, hash);

			bool present = !Unsafe.IsNullRef(ref entry);

			bool render = present && (visible || !MenuAnimation.dark(ref entry));
			if (!render)
			{
				MenuAnimation.consumeClosed(animId);
				return new SectionScope
				{
					m_render = false,
					m_animId = animId
				};
			}

			float layoutProgress = FastMath.clamp01(entry.m_payout.sample());

			Color prev = Gui.color;
			Gui.color = ColorMath.fade(prev, entry.m_light.sampleLight());

			MenuAnimation.m_sectionDepth++;
			if (MenuAnimation.m_sectionDepth < SectionMeasure.kMaxDepth)
			{
				MenuAnimation.kChildLive[MenuAnimation.m_sectionDepth] = 0f;
				MenuAnimation.kChildSettled[MenuAnimation.m_sectionDepth] = 0f;
			}

			SectionScope scope = new SectionScope
			{
				m_render = true,
				m_animId = animId,
				m_startY = y,
				m_layoutProgress = layoutProgress,
				m_targetOpen = visible,
				m_savedColor = prev,
				m_clipped = MenuAnimation.beginClip(hash, layoutProgress, y)
			};

			MenuRowFlow.pushContainer(hash);
			return scope;
		}

		internal static void endSection(ref float y, SectionScope scope)
		{
			if (!scope.m_render)
				return;

			MenuRowFlow.popContainer();

			if (scope.m_clipped)
				ContentClip.pop();

			int depth = FastMath.min(MenuAnimation.m_sectionDepth, SectionMeasure.kMaxDepth - 1);

			float innerLive = y - scope.m_startY;

			float ownLive = innerLive - MenuAnimation.kChildLive[depth];

			float innerSettled = ownLive + MenuAnimation.kChildSettled[depth];

			MenuAnimation.kFullHeights[scope.m_animId.GetHashCode()] = innerLive;

			float liveContrib = innerLive * scope.m_layoutProgress;

			float settledContrib = scope.m_targetOpen ? innerSettled : 0f;

			if (MenuAnimation.m_sectionDepth > 0)
				MenuAnimation.m_sectionDepth--;

			int parent = FastMath.min(MenuAnimation.m_sectionDepth, SectionMeasure.kMaxDepth - 1);
			MenuAnimation.kChildLive[parent] += liveContrib;
			MenuAnimation.kChildSettled[parent] += settledContrib;

			Gui.color = scope.m_savedColor;

			y = scope.m_startY + liveContrib;
		}

		internal static void snapBlocks(int[] blocks)
		{
			if (blocks == null)
				return;

			foreach (int block in blocks)
				MenuAnimation.kBlockMoves.Remove(block);
		}

		internal static void beginShuffle(int[] blocks, bool markHops)
		{
			if (blocks == null || !AnimationConfig.shuffleEnabled)
				return;

			foreach (int block in blocks)
			{
				ref BlockMoveEntry entry = ref CollectionsMarshal.GetValueRefOrNullRef(MenuAnimation.kBlockMoves, block);
				if (Unsafe.IsNullRef(ref entry))
					continue;

				entry.m_armed = true;
				entry.m_markHop = markHops;
			}
		}

		internal static Vector4 displayedRect(int blockKey, Vector4 target)
		{
			Vector2 targetPosition = VectorMath.v2(target.x, target.y);

			ref BlockMoveEntry entry = ref CollectionsMarshal.GetValueRefOrAddDefault(MenuAnimation.kBlockMoves, blockKey, out bool exists);
			if (!exists || !AnimationConfig.shuffleEnabled)
			{
				entry = new BlockMoveEntry
				{
					m_target = targetPosition,
					m_offsetX = MotionValue.at(0f),
					m_offsetY = MotionValue.at(0f)
				};
				return target;
			}

			if (entry.m_armed)
				MenuAnimation.captureShuffle(ref entry, targetPosition, MotionMath.extent(target.z, target.w));

			entry.m_target = targetPosition;
			target.x += entry.m_offsetX.sample();
			target.y += entry.m_offsetY.sample();
			return target;
		}

		internal static float hopGlow(int blockKey)
		{
			ref BlockMoveEntry entry = ref CollectionsMarshal.GetValueRefOrNullRef(MenuAnimation.kBlockMoves, blockKey);
			if (Unsafe.IsNullRef(ref entry) || entry.m_hopSpan <= 0f)
				return 0f;

			if (entry.m_offsetX.atRest && entry.m_offsetY.atRest)
			{
				entry.m_hopSpan = 0f;
				return 0f;
			}

			float left = VectorMath.distance2D(entry.m_offsetX.sample(), entry.m_offsetY.sample());
			return FastMath.clamp01(left / entry.m_hopSpan);
		}

		internal static MarkerFrame displayedMarker(int key, Rect target, float origin, bool horizontal, bool snap = false)
		{
			ref MarkerEntry entry = ref CollectionsMarshal.GetValueRefOrAddDefault(MenuAnimation.kMarkers, key, out bool exists);
			entry.m_lastTouched = Gui.unscaledTime;
			if (!exists || snap || !MenuAnimation.enabled)
			{
				entry.m_target = target;
				entry.m_origin = target;
				entry.m_payout = MotionValue.at(1f);
				entry.m_leavingLight = MotionValue.at(0f);
				return new MarkerFrame { m_live = target, m_liveAlpha = 1f };
			}

			if (!MenuAnimation.sameRect(entry.m_target, target))
			{
				entry.m_leaving = MenuAnimation.markerRect(ref entry, horizontal);
				entry.m_leavingLight = MotionValue.at(1f);
				entry.m_leavingLight.vent(0f, MotionProfile.light(true));
				entry.m_target = target;
				entry.m_origin = MenuAnimation.collapsedAt(target, origin, horizontal);

				float length = horizontal ? RectMath.w(target) : RectMath.h(target);
				entry.m_payout = MotionValue.at(0f);
				entry.m_payout.reel(1f, MotionProfile.payout(MotionMath.extent(RectMath.w(target), RectMath.h(target)), length, true));
			}

			return new MarkerFrame
			{
				m_live = MenuAnimation.markerRect(ref entry, horizontal),
				m_liveAlpha = 1f,
				m_leaving = entry.m_leaving,
				m_leavingAlpha = entry.m_leavingLight.sampleLight()
			};
		}

		internal static float displayedValue(int key, float target, bool instant, float trackPx)
		{
			ref ValueEntry entry = ref CollectionsMarshal.GetValueRefOrAddDefault(MenuAnimation.kDisplayedValues, key, out bool exists);
			entry.m_lastTouched = Gui.unscaledTime;
			if (!exists || instant || !MenuAnimation.enabled)
			{
				entry.m_value = MotionValue.at(target);
				return target;
			}

			entry.m_value.reel(target, MotionProfile.hand(trackPx, true));
			return entry.m_value.sample();
		}

		internal static float fadeAlpha(int key)
		{
			int salted = unchecked(key * AnimKeys.kMix) ^ AnimKeys.kFadeSalt;
			return MenuAnimation.openEntry(salted, OpenCloseKind.FadeIn).m_light.sampleLight();
		}

		private static void launchBlock(ref BlockEntryState state, in SwitchEntry wave, Rect blockRect)
		{
			Vector2 center = VectorMath.v2(MenuAnimation.m_contentOrigin.x + RectMath.centerX(blockRect), MenuAnimation.m_contentOrigin.y + RectMath.centerY(blockRect));

			float towardX = wave.m_trigger.x - center.x;

			float towardY = wave.m_trigger.y - center.y;

			MotionProfile body = MotionProfile.body(MotionMath.extent(RectMath.w(blockRect), RectMath.h(blockRect)), 1f, true);

			float distance = VectorMath.distance2D(towardX, towardY);

			float elapsed = (MotionClock.step - wave.m_startStep) * MenuAnimation.stepSeconds;

			float delay = FastMath.max(0f, distance / MenuAnimation.m_contentWidth * MotionMath.halfRise(body.m_omega, body.m_damping) - elapsed);

			if (state.m_light.atRest && state.m_offsetX.atRest && state.m_offsetY.atRest)
			{
				float slack = MotionTokens.kCable.m_stretch * AnimationConfig.MotionAmount;
				state.m_light = MotionValue.at(0f);
				state.m_offsetX = MotionValue.at(towardX * slack);
				state.m_offsetY = MotionValue.at(towardY * slack);
			}

			state.m_light.reelAfter(1f, MotionProfile.light(true), delay);
			state.m_offsetX.reelAfter(0f, body, delay);
			state.m_offsetY.reelAfter(0f, body, delay);
		}

		private static Rect collapsedAt(Rect target, float origin, bool horizontal)
		{
			if (horizontal)
				return RectMath.create(FastMath.clamp(origin, RectMath.x(target), RectMath.xMax(target)), RectMath.y(target), 0f, RectMath.h(target));

			return RectMath.create(RectMath.x(target), FastMath.clamp(origin, RectMath.y(target), RectMath.yMax(target)), RectMath.w(target), 0f);
		}

		private static Rect markerRect(ref MarkerEntry entry, bool horizontal)
		{
			float paid = FastMath.clamp01(entry.m_payout.sample());
			if (paid >= 1f)
				return entry.m_target;

			Rect from = entry.m_origin;

			Rect to = entry.m_target;
			if (horizontal)
			{
				float left = FastMath.lerpUnclamped(RectMath.x(from), RectMath.x(to), paid);
				return RectMath.create(left, RectMath.y(to), FastMath.lerpUnclamped(RectMath.xMax(from), RectMath.xMax(to), paid) - left, RectMath.h(to));
			}

			float top = FastMath.lerpUnclamped(RectMath.y(from), RectMath.y(to), paid);
			return RectMath.create(RectMath.x(to), top, RectMath.w(to), FastMath.lerpUnclamped(RectMath.yMax(from), RectMath.yMax(to), paid) - top);
		}

		private static bool sameRect(Rect a, Rect b)
		{
			float epsilon = MenuAnimation.restPx;
			return FastMath.abs(RectMath.x(a) - RectMath.x(b)) < epsilon && FastMath.abs(RectMath.y(a) - RectMath.y(b)) < epsilon && FastMath.abs(RectMath.w(a) - RectMath.w(b)) < epsilon && FastMath.abs(RectMath.h(a) - RectMath.h(b)) < epsilon;
		}

		private static float sampleLight(Dictionary<int, LightEntry> states, int id, bool on, bool animate)
		{
			ref LightEntry entry = ref MenuAnimation.touchLight(states, id, on ? 1f : 0f);
			return Fade.cut(ref entry.m_value, on, animate);
		}

		private static ref LightEntry touchLight(Dictionary<int, LightEntry> states, int id, float seed)
		{
			ref LightEntry entry = ref CollectionsMarshal.GetValueRefOrNullRef(states, id);
			if (Unsafe.IsNullRef(ref entry))
			{
				states[id] = new LightEntry { m_value = MotionValue.at(seed) };
				entry = ref CollectionsMarshal.GetValueRefOrNullRef(states, id);
			}
			entry.m_lastTouched = Gui.unscaledTime;
			return ref entry;
		}

		private static ref OpenCloseEntry openEntry(int key, OpenCloseKind kind)
		{
			ref OpenCloseEntry entry = ref CollectionsMarshal.GetValueRefOrNullRef(MenuAnimation.kOpenClose, key);
			if (Unsafe.IsNullRef(ref entry))
			{
				MenuAnimation.kOpenClose[key] = new OpenCloseEntry
				{
					m_light = MotionValue.at(0f),
					m_payout = MotionValue.at(0f)
				};
				entry = ref CollectionsMarshal.GetValueRefOrNullRef(MenuAnimation.kOpenClose, key);
			}

			entry.m_kind = kind;
			entry.m_targetOpen = true;
			entry.m_lastTouched = Gui.unscaledTime;

			bool animate = MenuAnimation.kindEnabled(kind);
			entry.m_light.reel(1f, MenuAnimation.sheetLight(ref entry));
			if (kind == OpenCloseKind.Popup)
				entry.m_payout.reel(1f, MotionProfile.payout(MotionTokens.kAnchorLoad.m_extentPx, 1f, animate));
			return ref entry;
		}

		// popups are frames and light over their own size, sections and fade ins carry the anchor light
		private static MotionProfile sheetLight(ref OpenCloseEntry entry)
		{
			bool animate = MenuAnimation.kindEnabled(entry.m_kind);
			return entry.m_kind == OpenCloseKind.Popup ? MotionProfile.frameLight(entry.m_extent, animate) : MotionProfile.light(animate);
		}

		// the size is only known once the sheet lays out so the light is retuned in flight without losing where it is
		private static void sizeSheet(ref OpenCloseEntry entry, float extentPx)
		{
			if (entry.m_extent == extentPx)
				return;

			entry.m_extent = extentPx;
			entry.m_light.retune(MenuAnimation.sheetLight(ref entry));
		}

		private static MotionProfile sectionPayout(int hash)
		{
			MenuAnimation.kFullHeights.TryGetValue(hash, out float fullHeight);
			return MotionProfile.payout(MotionMath.extent(MenuHost.m_contentW, fullHeight), fullHeight, AnimationConfig.sectionsEnabled);
		}

		private static bool dark(ref OpenCloseEntry entry)
		{
			if (!MenuAnimation.dark(ref entry.m_light))
				return false;

			return entry.m_kind != OpenCloseKind.Section || MenuAnimation.dark(ref entry.m_payout);
		}

		private static bool dark(ref MotionValue value) => value.sample() <= 0f && value.goal <= 0f;

		private static bool kindEnabled(OpenCloseKind kind)
		{
			switch (kind)
			{
				case OpenCloseKind.Section:
					return AnimationConfig.sectionsEnabled;

				case OpenCloseKind.FadeIn:
					return MenuAnimation.enabled;

				default:
					return AnimationConfig.popupsEnabled;
			}
		}

		private static void gcLights(Dictionary<int, LightEntry> states, float now)
		{
			if (states.Count == 0)
				return;

			MenuAnimation.kGcScratch.Clear();
			foreach (KeyValuePair<int, LightEntry> pair in states)
			{
				if (!(now - pair.Value.m_lastTouched > Gc.kLifetimeSeconds && pair.Value.m_value.atRest && pair.Value.m_value.goal <= 0f))
					continue;

				MenuAnimation.kGcScratch.Add(pair.Key);
			}

			MenuAnimation.removeCollected(states);
		}

		private static void gcFades(float now)
		{
			if (MenuAnimation.kOpenClose.Count == 0)
				return;

			MenuAnimation.kGcScratch.Clear();
			foreach (KeyValuePair<int, OpenCloseEntry> pair in MenuAnimation.kOpenClose)
			{
				if (!(pair.Value.m_kind == OpenCloseKind.FadeIn && now - pair.Value.m_lastTouched > Gc.kLifetimeSeconds))
					continue;

				MenuAnimation.kGcScratch.Add(pair.Key);
			}

			MenuAnimation.removeCollected(MenuAnimation.kOpenClose);
		}

		private static void gcEntries(float now)
		{
			if (MenuAnimation.kBlockEntries.Count == 0)
				return;

			MenuAnimation.kGcScratch.Clear();
			foreach (KeyValuePair<int, BlockEntryState> pair in MenuAnimation.kBlockEntries)
			{
				if (!(now - pair.Value.m_lastTouched > Gc.kLifetimeSeconds && pair.Value.m_light.atRest))
					continue;

				MenuAnimation.kGcScratch.Add(pair.Key);
			}

			MenuAnimation.removeCollected(MenuAnimation.kBlockEntries);
		}

		private static void gcMarkers(float now)
		{
			if (MenuAnimation.kMarkers.Count == 0)
				return;

			MenuAnimation.kGcScratch.Clear();
			foreach (KeyValuePair<int, MarkerEntry> pair in MenuAnimation.kMarkers)
			{
				if (!(now - pair.Value.m_lastTouched > Gc.kLifetimeSeconds))
					continue;

				MenuAnimation.kGcScratch.Add(pair.Key);
			}

			MenuAnimation.removeCollected(MenuAnimation.kMarkers);
		}

		private static void gcValues(float now)
		{
			if (MenuAnimation.kDisplayedValues.Count == 0)
				return;

			MenuAnimation.kGcScratch.Clear();
			foreach (KeyValuePair<int, ValueEntry> pair in MenuAnimation.kDisplayedValues)
			{
				if (!(now - pair.Value.m_lastTouched > Gc.kLifetimeSeconds))
					continue;

				MenuAnimation.kGcScratch.Add(pair.Key);
			}

			MenuAnimation.removeCollected(MenuAnimation.kDisplayedValues);
		}

		private static void removeCollected<TEntry>(Dictionary<int, TEntry> states)
		{
			for (int i = 0; i < MenuAnimation.kGcScratch.Count; i++)
				states.Remove(MenuAnimation.kGcScratch[i]);
		}

		private static int buttonId(Rect rect, string text)
		{
			unchecked
			{
				int hash = RectMath.x(rect).GetHashCode();

				hash = (hash * AnimKeys.kMix) ^ RectMath.y(rect).GetHashCode();
				hash = (hash * AnimKeys.kMix) ^ RectMath.w(rect).GetHashCode();
				hash = (hash * AnimKeys.kMix) ^ (text?.GetHashCode() ?? 0);
				return hash;
			}
		}

		private static int contentId(Rect rect, string text)
		{
			unchecked
			{
				int hash = MenuHost.m_currentBlock;

				hash = (hash * AnimKeys.kMix) ^ (RectMath.x(rect) - MenuHost.m_blockOffset.x).GetHashCode();
				hash = (hash * AnimKeys.kMix) ^ (RectMath.y(rect) - MenuHost.m_blockOffset.y).GetHashCode();
				hash = (hash * AnimKeys.kMix) ^ RectMath.w(rect).GetHashCode();
				hash = (hash * AnimKeys.kMix) ^ (text?.GetHashCode() ?? 0);
				return hash;
			}
		}

		private static int idFor(Rect rect, string text) => MenuHost.m_inBlock ? MenuAnimation.contentId(rect, text) : MenuAnimation.buttonId(rect, text);
		private static int animKey(string id, int salt) => unchecked(id.GetHashCode() * AnimKeys.kMix) ^ salt;

		private static void snapToggle(int key, bool value)
		{
			ref LightEntry entry = ref MenuAnimation.touchLight(MenuAnimation.kToggleState, key, value ? 1f : 0f);
			entry.m_value.snap(value ? 1f : 0f);
		}

		private static ref OpenCloseEntry openClose(string id)
		{
			if (id == null)
				return ref Unsafe.NullRef<OpenCloseEntry>();

			return ref CollectionsMarshal.GetValueRefOrNullRef(MenuAnimation.kOpenClose, id.GetHashCode());
		}

		private static bool tryOpenClose(string id, out OpenCloseEntry entry)
		{
			if (id == null)
			{
				entry = default;
				return false;
			}
			return MenuAnimation.kOpenClose.TryGetValue(id.GetHashCode(), out entry);
		}

		private static string sectionId(string key)
		{
			int block = MenuHost.m_currentBlock;

			(int, string) cacheKey = (block, key);
			if (!MenuAnimation.kSectionIds.TryGetValue(cacheKey, out string animId))
			{
				animId = $"sec:{block}:{key}";
				MenuAnimation.kSectionIds[cacheKey] = animId;
			}
			return animId;
		}

		private static bool beginClip(int hash, float layoutProgress, float y)
		{
			if (!AnimationConfig.sectionsEnabled || layoutProgress >= 1f || layoutProgress <= 0f)
				return false;

			MenuAnimation.kFullHeights.TryGetValue(hash, out float fullHeight);
			ContentClip.push(y, FastMath.max(MenuAnimation.kMinReveal, fullHeight * layoutProgress));
			return true;
		}

		private static void captureShuffle(ref BlockMoveEntry entry, Vector2 targetPosition, float extentPx)
		{
			entry.m_armed = false;

			float jumpX = entry.m_target.x - targetPosition.x;

			float jumpY = entry.m_target.y - targetPosition.y;
			if (FastMath.abs(jumpX) < MenuAnimation.restPx && FastMath.abs(jumpY) < MenuAnimation.restPx)
				return;

			MotionProfile body = MotionProfile.body(extentPx, 1f, AnimationConfig.shuffleEnabled);
			entry.m_offsetX.offsetBy(jumpX);
			entry.m_offsetY.offsetBy(jumpY);
			entry.m_offsetX.reel(0f, body);
			entry.m_offsetY.reel(0f, body);

			if (entry.m_markHop && FastMath.abs(jumpX) >= MenuAnimation.restPx)
				entry.m_hopSpan = VectorMath.distance2D(entry.m_offsetX.sample(), entry.m_offsetY.sample());
		}
	}
}
