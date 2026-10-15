using System;
using System.Collections.Generic;
using System.Reflection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace Trellis
{
	[Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
	internal static class Gui
	{
		private const float kCoverageEpsilon = 0.004f;
		private const float kOpaqueCoverage = 0.999f;
		internal const float kDesignWidth = 1920f;

		private static readonly EvictingCache<string, GUIContent> kContentCache = new EvictingCache<string, GUIContent>(ContentCache.kMax);
		private static readonly EvictingCache<string, GUIContent> kPlainContents = new EvictingCache<string, GUIContent>(ContentCache.kMax);

		private static GUIContent m_emptyContent;
		private static Texture2D m_whiteTexture;
		private static Il2CppStructArray<Vector3> m_worldCorners;
		private static Color m_colorShadow = ColorMath.kWhite;
		private static bool m_enabledShadow = true;
		private static Matrix4x4 m_matrixShadow = VectorMath.kIdentityMatrix;
		private static float m_matrixScale = 1f;
		private static bool m_repainting;
		private static int m_passToken;
		private static int m_frameCount;
		private static float m_unscaledTime;
		private static float m_unscaledDelta;
		private static float m_screenWidth;
		private static float m_screenHeight;
		private static int m_layoutFrame = -1;
		private static int m_dragFrame = -1;
		private static bool m_passRedundant;
		private static int m_unsnappedDepth;
		private static float m_clipX;
		private static float m_clipY;
		private static readonly List<Vector2> kClipOrigins = new List<Vector2>();

		[Config, Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
		internal static float DpiScale = 1f;

		private static class ContentCache
		{
			internal const int kMax = 1 << 10;
		}

		private static class TextOffset
		{
			internal const float kPx = 1f;
		}

		private static class RoundedTexture
		{
			internal const float kNativeAspect = 0f;
		}

		private static class WorldCorner
		{
			internal const int kCount = 4;
			internal const int kBottomLeft = 0;
			internal const int kTopLeft = 1;
			internal const int kTopRight = 2;
		}

		internal static bool repainting => Gui.m_repainting;
		// unity runs one OnGUI pass per queued input event plus a Layout each
		internal static bool passRedundant => Gui.m_passRedundant;
		internal static int passToken => Gui.m_passToken;
		internal static int frameCount => Gui.m_frameCount;
		internal static float unscaledTime => Gui.m_unscaledTime;
		internal static float unscaledDelta => Gui.m_unscaledDelta;

		internal static Color color
		{
			get => Gui.m_colorShadow;
			set
			{
				if (ColorMath.engineEquals(Gui.m_colorShadow, value))
					return;

				Gui.m_colorShadow = value;

				GUI.color = GraphicsInfo.shaderColor(value);
			}
		}

		internal static bool enabled
		{
			get => Gui.m_enabledShadow;
			set
			{
				if (Gui.m_enabledShadow == value)
					return;

				Gui.m_enabledShadow = value;

				GUI.enabled = value;
			}
		}

		internal static Matrix4x4 matrix
		{
			get => Gui.m_matrixShadow;
			set
			{
				Gui.m_matrixShadow = value;
				Gui.m_matrixScale = value.m00;

				GUI.matrix = value;
			}
		}

		internal static float devicePixel => Gui.m_matrixScale > 0f ? 1f / Gui.m_matrixScale : 1f;
		// whiteTexture is a property invoke every read
		internal static Texture2D whiteTexture => Gui.m_whiteTexture ??= Texture2D.whiteTexture;
		internal static float spaceWidth => Gui.m_matrixScale > 0f ? Gui.m_screenWidth / Gui.m_matrixScale : Gui.m_screenWidth;
		internal static float spaceHeight => Gui.m_matrixScale > 0f ? Gui.m_screenHeight / Gui.m_matrixScale : Gui.m_screenHeight;
		internal static float screenWidth => Gui.m_screenWidth;
		internal static float screenHeight => Gui.m_screenHeight;
		// a design unit offset lands between device pixels and smears the glyph
		private static float offsetStep => Gui.snapThickness(TextOffset.kPx);

		internal static void beginPass(EventType eventType)
		{
			Gui.m_repainting = eventType == EventType.Repaint;
			Gui.m_colorShadow = GUI.color;
			Gui.m_enabledShadow = GUI.enabled;
			Gui.m_matrixShadow = GUI.matrix;
			Gui.m_matrixScale = Gui.m_matrixShadow.m00;
			Gui.m_frameCount = Live.frameCount;
			Gui.m_unscaledTime = Time.unscaledTime;
			Gui.m_unscaledDelta = Time.unscaledDeltaTime;
			Gui.m_screenWidth = Screen.width;
			Gui.m_screenHeight = Screen.height;

			Gui.m_passToken++;

			Gui.m_passRedundant = Gui.redundant(eventType, Gui.m_frameCount);
			Gui.resetUnsnapped();
			Gui.resetClips();

			if (!Gui.m_repainting)
				return;

			GraphicsInfo.sampleGui();
		}

		internal static void setTextColor(GUIStyle style, Color color) => style.normal.textColor = GraphicsInfo.shaderColor(color);
		// the setter short circuits on an unchanged shadow so a native GUI.color write desyncs them forever
		internal static void reapplyColor() => GUI.color = GraphicsInfo.shaderColor(Gui.m_colorShadow);

		// unity ScaleAroundPivot writes GUI.matrix directly so hand the shadow back
		internal static void resyncMatrix()
		{
			Gui.m_matrixShadow = GUI.matrix;
			Gui.m_matrixScale = Gui.m_matrixShadow.m00;
		}

		internal static Rect snapRect(Rect rect)
		{
			float scale = Gui.m_matrixScale;
			if (scale <= 0f || Gui.m_unsnappedDepth > 0)
				return rect;

			float x = RectMath.x(rect);

			float y = RectMath.y(rect);

			float phaseX = Gui.phaseX;

			float phaseY = Gui.phaseY;
			return RectMath.minMax(PixelGrid.snap(x, scale, phaseX), PixelGrid.snap(y, scale, phaseY), PixelGrid.snap(x + RectMath.w(rect), scale, phaseX), PixelGrid.snap(y + RectMath.h(rect), scale, phaseY));
		}

		internal static float snapThickness(float size) => PixelGrid.snapSize(size, Gui.m_matrixScale);
		internal static float snapValue(float coordinate) => PixelGrid.snap(coordinate, Gui.m_matrixScale);
		internal static float ceilPixel(float value) => PixelGrid.ceil(value, Gui.m_matrixScale);

		internal static Rect snapStroke(Rect rect)
		{
			float scale = Gui.m_matrixScale;
			if (scale <= 0f || Gui.m_unsnappedDepth > 0)
				return rect;

			float x = RectMath.x(rect);

			float y = RectMath.y(rect);

			float width = RectMath.w(rect);

			float height = RectMath.h(rect);

			float phaseX = Gui.phaseX;

			float phaseY = Gui.phaseY;
			if (width <= height)
			{
				float thin = PixelGrid.snapSize(width, scale);

				float left = PixelGrid.snapBand(x, width, thin, scale, phaseX);
				return RectMath.minMax(left, PixelGrid.snap(y, scale, phaseY), left + thin, PixelGrid.snap(y + height, scale, phaseY));
			}

			float thick = PixelGrid.snapSize(height, scale);

			float top = PixelGrid.snapBand(y, height, thick, scale, phaseY);
			return RectMath.minMax(PixelGrid.snap(x, scale, phaseX), top, PixelGrid.snap(x + width, scale, phaseX), top + thick);
		}

		// snapping the stroke alone drifts it a pixel off the edge
		internal static Rect topEdge(Rect rect, float thickness)
		{
			Rect snapped = Gui.snapRect(rect);
			return RectMath.create(RectMath.x(snapped), RectMath.y(snapped), RectMath.w(snapped), Gui.edgeThickness(RectMath.h(snapped), thickness));
		}

		internal static Rect bottomEdge(Rect rect, float thickness)
		{
			Rect snapped = Gui.snapRect(rect);

			float edge = Gui.edgeThickness(RectMath.h(snapped), thickness);
			return RectMath.create(RectMath.x(snapped), RectMath.yMax(snapped) - edge, RectMath.w(snapped), edge);
		}

		internal static Rect leftEdge(Rect rect, float thickness)
		{
			Rect snapped = Gui.snapRect(rect);
			return RectMath.create(RectMath.x(snapped), RectMath.y(snapped), Gui.edgeThickness(RectMath.w(snapped), thickness), RectMath.h(snapped));
		}

		internal static void pushUnsnapped() => Gui.m_unsnappedDepth++;

		// every clip moves the local origin and snapping must follow it onto the device grid
		internal static void beginClip(Rect rect)
		{
			GUI.BeginClip(rect);
			Gui.pushOrigin(RectMath.x(rect), RectMath.y(rect));
		}

		internal static void beginClip(Rect rect, Vector2 scrollOffset)
		{
			GUI.BeginClip(rect, scrollOffset, VectorMath.kZero2, false);
			Gui.pushOrigin(RectMath.x(rect) + scrollOffset.x, RectMath.y(rect) + scrollOffset.y);
		}

		internal static void endClip()
		{
			GUI.EndClip();
			Gui.popOrigin();
		}

		internal static void beginGroup(Rect rect)
		{
			GUI.BeginGroup(rect);
			Gui.pushOrigin(RectMath.x(rect), RectMath.y(rect));
		}

		internal static void endGroup()
		{
			GUI.EndGroup();
			Gui.popOrigin();
		}

		// unity own bar is an 8px strip with no hit padding so both bars stay hidden
		// unity rounds the content offset half to even before it pushes the clip
		internal static Vector2 beginScrollView(Rect viewRect, Vector2 scrollPosition, Rect virtualRect)
		{
			Vector2 applied = GUI.BeginScrollView(viewRect, scrollPosition, virtualRect, GUIStyle.none, GUIStyle.none);
			Gui.pushOrigin(RectMath.x(viewRect) + MathF.Round(-applied.x - RectMath.x(virtualRect)), RectMath.y(viewRect) + MathF.Round(-applied.y - RectMath.y(virtualRect)));
			return applied;
		}

		internal static void endScrollView()
		{
			GUI.EndScrollView();
			Gui.popOrigin();
		}

		internal static void popUnsnapped()
		{
			if (Gui.m_unsnappedDepth <= 0)
				return;

			Gui.m_unsnappedDepth--;
		}

		internal static Rect clampScreen(Rect rect)
		{
			float maxX = Gui.spaceWidth;

			float maxY = Gui.spaceHeight;

			float width = FastMath.min(RectMath.w(rect), maxX);

			float height = FastMath.min(RectMath.h(rect), maxY);

			float x = FastMath.clamp(RectMath.x(rect), 0f, FastMath.max(0f, maxX - width));

			float y = FastMath.clamp(RectMath.y(rect), 0f, FastMath.max(0f, maxY - height));
			return RectMath.create(x, y, width, height);
		}

		internal static void drawTextureSnapped(Rect rect, Texture texture)
		{
			if (!Gui.m_repainting)
				return;

			GUI.DrawTexture(Gui.snapRect(rect), texture);
		}

		internal static void drawStroke(Rect rect, Texture texture)
		{
			if (!Gui.m_repainting)
				return;
			if (RectMath.w(rect) <= 0f || RectMath.h(rect) <= 0f)
				return;

			if (Gui.m_unsnappedDepth > 0 && Gui.m_matrixScale > 0f)
			{
				Gui.drawFeathered(rect, texture);
				return;
			}

			Rect snapped = Gui.snapStroke(rect);
			if (RectMath.w(snapped) <= 0f || RectMath.h(snapped) <= 0f)
				return;

			GUI.DrawTexture(snapped, texture);
		}

		internal static void drawStroke(Rect rect, Color color)
		{
			if (!Gui.m_repainting || color.a <= 0f)
				return;

			Color prev = Gui.m_colorShadow;
			Gui.color = ColorMath.modulate(color, prev);
			Gui.drawStroke(rect, Gui.whiteTexture);
			Gui.color = prev;
		}

		internal static void fillRect(Rect rect, Color color)
		{
			if (!Gui.m_repainting || color.a <= 0f)
				return;

			Color prev = Gui.m_colorShadow;
			Gui.color = ColorMath.modulate(color, prev);

			GUI.DrawTexture(Gui.snapRect(rect), Gui.whiteTexture);

			Gui.color = prev;
		}

		internal static GUIContent content(string text)
		{
			if (string.IsNullOrEmpty(text))
				return Gui.m_emptyContent ??= new GUIContent(string.Empty);
			if (Gui.kContentCache.tryGetValue(text, out GUIContent cached))
				return cached;

			GUIContent created = new GUIContent(text);
			Gui.kContentCache.set(text, created);
			return created;
		}

		internal static void label(Rect rect, string text, GUIStyle style)
		{
			if (!Gui.m_repainting)
				return;

			GUI.Label(rect, Gui.content(text), style);
		}

		internal static void label(Rect rect, GUIContent content, GUIStyle style)
		{
			if (!Gui.m_repainting)
				return;

			GUI.Label(rect, content, style);
		}

		internal static void drawTexture(Rect rect, Texture texture)
		{
			if (!Gui.m_repainting)
				return;

			GUI.DrawTexture(rect, texture);
		}

		// blending a camera texture punches holes where meshes wrote low alpha
		internal static void drawTextureOpaque(Rect rect, Texture texture)
		{
			if (!Gui.m_repainting || !Live.isUnityAlive(texture))
				return;

			GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, false);
		}

		// unity DrawTextureWithTexCoords crashes natively in this build so emulate it with a clip lol
		internal static void drawTextureUv(Rect rect, Texture texture, Rect textureCoords)
		{
			if (!Gui.m_repainting)
				return;
			if (!Live.isUnityAlive(texture))
				return;
			if (RectMath.w(textureCoords) <= 0f || RectMath.h(textureCoords) <= 0f)
				return;

			float fullWidth = RectMath.w(rect) / RectMath.w(textureCoords);

			float fullHeight = RectMath.h(rect) / RectMath.h(textureCoords);

			// gui rects run top down and uv runs bottom up
			float offsetX = -RectMath.x(textureCoords) * fullWidth;

			float offsetY = -(1f - (RectMath.y(textureCoords) + RectMath.h(textureCoords))) * fullHeight;

			Gui.beginGroup(rect);

			try
			{
				GUI.DrawTexture(RectMath.create(offsetX, offsetY, fullWidth, fullHeight), texture);
			}
			finally
			{
				Gui.endGroup();

				Gui.reapplyColor();
			}
		}

		internal static void drawRoundedTexture(Rect rect, Color color, Vector4 borderWidths, Vector4 borderRadii) => Gui.drawRoundedTexture(rect, Gui.whiteTexture, ScaleMode.StretchToFill, color, borderWidths, borderRadii);

		internal static void drawRoundedTexture(Rect rect, Texture texture, ScaleMode scaleMode, Color color, Vector4 borderWidths, Vector4 borderRadii)
		{
			if (!Gui.m_repainting)
				return;

			Color resolved = GraphicsInfo.shaderColor(color);
			GUI.DrawTexture(rect, texture, scaleMode, true, RoundedTexture.kNativeAspect, resolved, borderWidths, borderRadii);
		}

		internal static float designScale(float extraFactor = 1f) => Gui.m_screenWidth / Gui.kDesignWidth * Gui.DpiScale * extraFactor;
		internal static float baseScale(float extraFactor = 1f) => Gui.m_screenWidth / Gui.kDesignWidth * extraFactor;

		internal static Matrix4x4 beginScaled(out float scale, float extraFactor = 1f)
		{
			scale = Gui.designScale(extraFactor);
			return Gui.applyScale(scale);
		}

		internal static Matrix4x4 beginScaled(float extraFactor = 1f) => Gui.beginScaled(out float scale, extraFactor);

		internal static Matrix4x4 beginBase(out float scale, float extraFactor = 1f)
		{
			scale = Gui.baseScale(extraFactor);
			return Gui.applyScale(scale);
		}

		internal static Matrix4x4 beginBase(float extraFactor = 1f) => Gui.beginBase(out float scale, extraFactor);

		internal static Matrix4x4 beginScaledAt(float scale, float pivotX, float pivotY)
		{
			Matrix4x4 prev = Gui.matrix;
			Gui.matrix = LayoutMath.scaleAt(prev, scale, pivotX, pivotY);
			return prev;
		}

		internal static void restoreGui(Matrix4x4 prevMatrix) => Gui.matrix = prevMatrix;
		internal static bool tryBaseRect(RectTransform rectTransform, out Rect rect) => Gui.tryDesignRect(rectTransform, Gui.baseScale(), out rect);

		internal static Rect shadowRect(Rect rect, int depth = 1)
		{
			float offset = Gui.offsetStep * depth;
			return RectMath.offset(rect, offset, offset);
		}

		// colour tags override the shadow tint
		internal static void drawShadow(Rect rect, string text, GUIStyle style, Color shadowColor, int depth = 1) => Gui.drawShadow(rect, Gui.plainContent(text), style, shadowColor, depth);

		internal static void drawShadow(Rect rect, GUIContent content, GUIStyle style, Color shadowColor, int depth = 1)
		{
			if (!Gui.m_repainting)
				return;

			Color saved = Gui.color;

			Gui.color = shadowColor;
			Gui.label(Gui.shadowRect(rect, depth), content, style);
			Gui.color = saved;
		}

		internal static void drawShadowed(Rect rect, string text, GUIStyle style, Color shadowColor)
		{
			if (!Gui.m_repainting)
				return;

			Gui.drawShadow(rect, text, style, shadowColor);
			Gui.label(rect, text, style);
		}

		private static bool redundant(EventType eventType, int frame)
		{
			switch (eventType)
			{
				case EventType.MouseMove:
					return true;

				case EventType.Layout:
					if (Gui.m_layoutFrame == frame)
						return true;

					Gui.m_layoutFrame = frame;
					return false;

				case EventType.MouseDrag:
					if (Gui.m_dragFrame == frame)
						return true;

					Gui.m_dragFrame = frame;
					return false;

				default:
					return false;
			}
		}

		private static float edgeThickness(float span, float thickness) => FastMath.min(Gui.snapThickness(thickness), FastMath.max(0f, span));

		private static float phaseX => Gui.m_clipX + Gui.m_matrixShadow.m03 / Gui.m_matrixScale;
		private static float phaseY => Gui.m_clipY + Gui.m_matrixShadow.m13 / Gui.m_matrixScale;

		private static void pushOrigin(float x, float y)
		{
			Gui.kClipOrigins.Add(VectorMath.v2(Gui.m_clipX, Gui.m_clipY));

			Gui.m_clipX += x;
			Gui.m_clipY += y;
		}

		private static void popOrigin()
		{
			int top = Gui.kClipOrigins.Count - 1;
			if (top < 0)
				return;

			Vector2 parent = Gui.kClipOrigins[top];

			Gui.kClipOrigins.RemoveAt(top);

			Gui.m_clipX = parent.x;
			Gui.m_clipY = parent.y;
		}

		private static void resetClips()
		{
			if (Gui.kClipOrigins.Count > 0)
				UiLog.debug($"[Gui.resetClips] {Gui.kClipOrigins.Count} unbalanced clip(s) at pass start");

			Gui.kClipOrigins.Clear();

			Gui.m_clipX = 0f;
			Gui.m_clipY = 0f;
		}

		private static void resetUnsnapped()
		{
			if (Gui.m_unsnappedDepth == 0)
				return;

			UiLog.debug($"[Gui.resetUnsnapped] {Gui.m_unsnappedDepth} unbalanced unsnapped scope(s) at pass start");

			Gui.m_unsnappedDepth = 0;
		}

		private static void drawFeathered(Rect rect, Texture texture)
		{
			float scale = Gui.m_matrixScale;

			float width = RectMath.w(rect);

			float height = RectMath.h(rect);

			bool thinX = width <= height;

			float thinStart = thinX ? RectMath.x(rect) : RectMath.y(rect);

			float thinSize = thinX ? width : height;

			float longStart = thinX ? RectMath.y(rect) : RectMath.x(rect);

			float longEnd = longStart + (thinX ? height : width);

			float minThin = 1f / scale;
			if (thinSize < minThin)
			{
				thinStart -= (minThin - thinSize) * 0.5f;

				thinSize = minThin;
			}

			float deviceStart = thinStart * scale;

			float deviceEnd = deviceStart + thinSize * scale;

			int firstRow = FastMath.floorToInt(deviceStart);

			int lastRow = FastMath.ceilToInt(deviceEnd) - 1;
			if (lastRow <= firstRow)
			{
				Gui.emitBand(texture, thinX, firstRow, firstRow + 1, longStart, longEnd, deviceEnd - deviceStart);
				return;
			}

			Gui.emitBand(texture, thinX, firstRow, firstRow + 1, longStart, longEnd, firstRow + 1 - deviceStart);

			if (lastRow > firstRow + 1)
				Gui.emitBand(texture, thinX, firstRow + 1, lastRow, longStart, longEnd, 1f);

			Gui.emitBand(texture, thinX, lastRow, lastRow + 1, longStart, longEnd, deviceEnd - lastRow);
		}

		private static void emitBand(Texture texture, bool thinX, int fromRow, int toRow, float longStart, float longEnd, float coverage)
		{
			if (coverage <= Gui.kCoverageEpsilon)
				return;

			float inverseScale = 1f / Gui.m_matrixScale;

			float thinMin = fromRow * inverseScale;

			float thinMax = toRow * inverseScale;

			Rect band = thinX ? RectMath.minMax(thinMin, longStart, thinMax, longEnd) : RectMath.minMax(longStart, thinMin, longEnd, thinMax);
			if (coverage >= Gui.kOpaqueCoverage)
			{
				GUI.DrawTexture(band, texture);
				return;
			}

			Color prev = Gui.m_colorShadow;
			Gui.color = ColorMath.fade(prev, coverage);

			GUI.DrawTexture(band, texture);

			Gui.color = prev;
		}

		private static GUIContent plainContent(string text)
		{
			if (string.IsNullOrEmpty(text))
				return Gui.content(text);
			if (Gui.kPlainContents.tryGetValue(text, out GUIContent cached))
				return cached;

			GUIContent plain = Gui.content(RichText.stripColors(text));
			Gui.kPlainContents.set(text, plain);
			return plain;
		}

		private static Matrix4x4 applyScale(float scale)
		{
			Matrix4x4 prev = Gui.matrix;

			Gui.matrix = MatrixMath.scale(VectorMath.v3(scale, scale, 1f));
			return prev;
		}

		private static bool tryDesignRect(RectTransform rectTransform, float scale, out Rect rect)
		{
			rect = default;

			if (!Live.isUnityAlive(rectTransform))
				return false;
			if (scale <= 0f)
				return false;

			Gui.m_worldCorners ??= new Il2CppStructArray<Vector3>(WorldCorner.kCount);
			rectTransform.GetWorldCorners(Gui.m_worldCorners);

			Span<Vector3> corners = Gui.m_worldCorners.AsSpan();

			float xMin = corners[WorldCorner.kBottomLeft].x / scale;

			float xMax = corners[WorldCorner.kTopRight].x / scale;

			float yTop = (Gui.m_screenHeight - corners[WorldCorner.kTopLeft].y) / scale;

			float yBottom = (Gui.m_screenHeight - corners[WorldCorner.kBottomLeft].y) / scale;
			rect = RectMath.minMax(xMin, yTop, xMax, yBottom);
			return true;
		}
	}
}
