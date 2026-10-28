using System;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Trellis
{
	internal static class FontGlyphCache
	{
		// verdana covers ascii and the legacy Font API is char based so surrogates cant be tested
		private const char kFirstTested = '\u0080';
		private const string kCodepointFormat = "X4";
		private const int kScanMax = 1 << 8;
		private const string kInjectedRequest = "RequestCharactersInTexture_Injected";
		private const BindingFlags kAnyStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

		private static readonly EvictingCache<string, bool> kScannedText = new EvictingCache<string, bool>(FontGlyphCache.kScanMax);

		// unity 6 passes the text through a span wrapper the interop layer generates broken so imgui fills its own glyphs there
		private static readonly Action<Font, string, int, FontStyle> kRequestCharacters = FontGlyphCache.bindRequest();

		private static readonly EvictingCache<RequestKey, int> kRequestedFrame = new EvictingCache<RequestKey, int>(Requests.kCacheMax);

		private static class Requests
		{
			internal const int kCacheMax = 1 << 10;
			internal const int kRefreshFrames = 120;
		}

		private readonly record struct RequestKey(IntPtr font, int fontSize, FontStyle style, string text);

		internal static void request(Font font, string text, int fontSize, FontStyle style)
		{
			if (FontGlyphCache.kRequestCharacters == null || !Live.isUnityAlive(font))
				return;
			if (string.IsNullOrEmpty(text))
				return;

			RequestKey key = new RequestKey(font.Pointer, fontSize, style, text);

			int frame = Live.frameCount;
			if (FontGlyphCache.kRequestedFrame.tryGetValue(key, out int last) && frame - last < Requests.kRefreshFrames)
				return;

			FontGlyphCache.kRequestedFrame.set(key, frame);

			FontGlyphCache.kRequestCharacters(font, text, fontSize, style);
		}

		private static Action<Font, string, int, FontStyle> bindRequest()
		{
			MethodInfo injected = typeof(Font).GetMethod(FontGlyphCache.kInjectedRequest, FontGlyphCache.kAnyStatic);
			if (injected == null || injected.GetParameters()[1].ParameterType.IsByRef)
				return null;

			MethodInfo request = typeof(Font).GetMethod(nameof(Font.RequestCharactersInTexture), new[] { typeof(string), typeof(int), typeof(FontStyle) });
			return request == null ? null : (Action<Font, string, int, FontStyle>)Delegate.CreateDelegate(typeof(Action<Font, string, int, FontStyle>), request);
		}

		internal static void requestBoth(Font font, string text, int fontSize)
		{
			FontGlyphCache.request(font, text, fontSize, FontStyle.Normal);
			FontGlyphCache.request(font, text, fontSize, FontStyle.Bold);
		}

		// tofu copied out of the game arrives as 25A1 which the font covers lol
		internal static void logMissing(string text, string ctx)
		{
			if (!UiLog.debugEnabled)
				return;
			if (string.IsNullOrEmpty(text))
				return;
			if (FontGlyphCache.kScannedText.tryGetValue(text, out bool valueValue))
				return;

			Font font = UiFont.resolved;
			if (!Live.isUnityAlive(font))
				return;

			FontGlyphCache.kScannedText.set(text, true);

			StringBuilder missing = null;
			for (int i = 0; i < text.Length; i++)
			{
				char character = text[i];
				if (character < FontGlyphCache.kFirstTested || char.IsSurrogate(character))
					continue;
				if (font.HasCharacter(character))
					continue;

				missing ??= new StringBuilder();
				if (missing.Length > 0)
					missing.Append(' ');

				missing.Append("U+").Append(((int)character).ToString(FontGlyphCache.kCodepointFormat));
			}
			if (missing == null)
				return;

			UiLog.debug($"[FontGlyphCache.logMissing] {ctx}: missing {missing} in \"{text}\"");
		}
	}
}
