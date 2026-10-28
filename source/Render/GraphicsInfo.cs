using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Trellis
{
	internal static class GraphicsInfo
	{
		internal static readonly string[] kModeNames = new[] { "Auto", "Off", "Colors", "sRGB Write" };

		[Config]
		public static GuiColorMode GuiColorCorrection = GuiColorMode.Auto;

		private static bool m_resolved;
		private static GraphicsDeviceType m_deviceType;
		private static string m_deviceName;
		private static ColorSpace m_colorSpace;
		private static bool m_uvTop;
		private static bool m_guiSampled;
		private static bool m_guiSrgb;
		private static bool m_guiTexture;

		internal readonly struct SrgbWriteScope : IDisposable
		{
			private readonly bool m_saved;
			private readonly bool m_applied;

			internal SrgbWriteScope(bool apply)
			{
				m_applied = apply;
				m_saved = apply && GL.sRGBWrite;

				if (!apply)
					return;

				GL.sRGBWrite = false;
			}

			public void Dispose()
			{
				if (!m_applied)
					return;

				GL.sRGBWrite = m_saved;
			}
		}

		internal static GraphicsDeviceType deviceType
		{
			get
			{
				GraphicsInfo.detect();
				return GraphicsInfo.m_deviceType;
			}
		}

		internal static bool linear => GraphicsInfo.m_colorSpace == ColorSpace.Linear;
		internal static int correctionVersion => GraphicsInfo.linearGui ? 1 : 0;

		private static GuiColorMode resolvedMode
		{
			get
			{
				if (GraphicsInfo.GuiColorCorrection != GuiColorMode.Auto)
					return GraphicsInfo.GuiColorCorrection;

				GraphicsInfo.detect();

				if (GraphicsInfo.m_colorSpace != ColorSpace.Linear || GraphicsInfo.openGl(GraphicsInfo.m_deviceType))
					return GuiColorMode.Off;

				// vulkan ignores GL.sRGBWrite
				if (GraphicsInfo.m_deviceType == GraphicsDeviceType.Vulkan)
					return GuiColorMode.Colors;

				// imgui sends 8 bit vertex colours so dark linear tones turn blue
				return GuiColorMode.SrgbWrite;
			}
		}

		private static bool linearGui => GraphicsInfo.resolvedMode == GuiColorMode.Colors;
		private static bool noSrgbWrite => GraphicsInfo.resolvedMode == GuiColorMode.SrgbWrite;

		internal static void detect()
		{
			if (GraphicsInfo.m_resolved)
				return;

			GraphicsInfo.m_resolved = true;
			GraphicsInfo.m_deviceType = SystemInfo.graphicsDeviceType;
			GraphicsInfo.m_deviceName = SystemInfo.graphicsDeviceName;
			GraphicsInfo.m_colorSpace = QualitySettings.activeColorSpace;
			GraphicsInfo.m_uvTop = SystemInfo.graphicsUVStartsAtTop;
			UiLog.debug($"[GraphicsInfo.detect] api={GraphicsInfo.m_deviceType} colorSpace={GraphicsInfo.m_colorSpace} uvTop={GraphicsInfo.m_uvTop} device={GraphicsInfo.m_deviceName}");
		}

		// unity RenderTexture.active only means anything mid render
		internal static void sampleGui()
		{
			if (GraphicsInfo.m_guiSampled)
				return;

			GraphicsInfo.m_guiSampled = true;

			RenderTexture active = RenderTexture.active;
			GraphicsInfo.m_guiTexture = Live.isUnityAlive(active);
			GraphicsInfo.m_guiSrgb = GraphicsInfo.m_guiTexture && active.sRGB;
			UiLog.debug($"[GraphicsInfo.sampleGui] guiTargetIsRenderTexture={GraphicsInfo.m_guiTexture} sRGB={GraphicsInfo.m_guiSrgb}");
		}

		// unity SetColor converts and SetVector doesnt
		internal static Color frameColor(Color display)
		{
			GraphicsInfo.detect();
			return GraphicsInfo.linear ? ColorMath.gammaToLinear(display) : display;
		}

		internal static SrgbWriteScope overlayPass() => new SrgbWriteScope(GraphicsInfo.noSrgbWrite);
		internal static bool openGl(GraphicsDeviceType type) => type == GraphicsDeviceType.OpenGLCore || type == GraphicsDeviceType.OpenGLES2 || type == GraphicsDeviceType.OpenGLES3;
		internal static Color shaderColor(Color color) => GraphicsInfo.linearGui ? ColorMath.gammaToLinear(color) : color;
		// raw texels read washed out on vulkan
		internal static Color texel(Color color) => GraphicsInfo.shaderColor(color);
		internal static float fadeAlpha(float alpha) => GraphicsInfo.linearGui ? ColorMath.linearToGamma(alpha) : alpha;
		internal static Color fromTexel(Color texel) => GraphicsInfo.linearGui ? ColorMath.linearToGamma(texel) : texel;

		internal static Color guiOver(Color top, Color bottom)
		{
			if (!GraphicsInfo.linearGui)
				return ColorMath.over(top, bottom);

			return ColorMath.linearToGamma(ColorMath.over(ColorMath.gammaToLinear(top), ColorMath.gammaToLinear(bottom)));
		}

		internal static string summary()
		{
			GraphicsInfo.detect();
			return $"{GraphicsInfo.m_deviceType}  |  {GraphicsInfo.m_colorSpace}  |  uvTop {GraphicsInfo.m_uvTop}";
		}

		internal static string correctionSummary() => GraphicsInfo.GuiColorCorrection == GuiColorMode.Auto ? $"Auto -> {GraphicsInfo.resolvedMode}" : GraphicsInfo.resolvedMode.ToString();

		internal static string targetSummary()
		{
			if (!GraphicsInfo.m_guiSampled)
				return "not sampled yet";

			return GraphicsInfo.m_guiTexture ? $"RenderTexture sRGB {GraphicsInfo.m_guiSrgb}" : "backbuffer";
		}

		internal static string cameraSummary(Camera camera)
		{
			if (!Live.isUnityAlive(camera))
				return "no camera";

			return $"hdr {camera.allowHDR}  |  target {camera.pixelWidth}x{camera.pixelHeight}  |  screen {Screen.width}x{Screen.height}";
		}
	}
}
