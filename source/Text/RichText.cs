using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Trellis
{
	internal static class RichText
	{
		internal const string kColorClose = "</color>";
		private const int kMaxUrl = 256;
		private const int kMinUrl = 4;
		private const int kUrlSlack = 32;
		private const string kWebPrefix = "www.";
		private const string kHttpsScheme = "https://";
		private const int kGradientChars = 24;
		private const string kEscapedOpen = "<\u200B";
		private const char kFirstPrintable = ' ';
		private const char kLastPrintable = '~';
		private const string kBracketReset = "[-]";
		private const char kBracketOpen = '[';
		private const char kBracketClose = ']';
		private const string kResetCapture = "-";
		private const int kCacheMax = 256;

		// the hash covers TMP shorthand colour tags
		private static readonly Regex kTagRegex = new Regex(@"</?[a-zA-Z#][^>]*>", RegexOptions.Compiled);
		private static readonly Regex kColorTag = new Regex(@"</?color[^>]*>", RegexOptions.Compiled);
		private static readonly Regex kColorOpen = new Regex(@"<color=([^>]*)>", RegexOptions.Compiled);
		private static readonly Regex kColorCloseRegex = new Regex(@"</color>", RegexOptions.Compiled);
		private static readonly Regex kBracketSpan = new Regex(@"\[([0-9a-fA-F]{3}(?:[0-9a-fA-F]{3})?|-)\]", RegexOptions.Compiled);
		private static readonly Regex kControlChars = new Regex(@"[\x00-\x08\x0B\x0E-\x1F\x7F]", RegexOptions.Compiled);
		private static readonly Regex kSpaceControl = new Regex(@"[\t\n\r\f\v]", RegexOptions.Compiled);
		private static readonly Regex kUrlRegex = new Regex(@"\b(?:https?://|www\.)[^\s<>""'\[\]]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex kTrailingPunct = new Regex(@"[.,)\]!?;:]+$", RegexOptions.Compiled);
		private static readonly MarkupRules kNameRules = new MarkupRules(true, true, false);
		private static readonly MarkupRules kColorRules = new MarkupRules(false, true, false);
		private static readonly MarkupRules kDisplayRules = new MarkupRules(true, false, false);
		private static readonly MarkupRules kWireRules = new MarkupRules(true, false, true);
		private static readonly EvictingCache<string, string> kStripped = new EvictingCache<string, string>(RichText.kCacheMax);
		private static readonly EvictingCache<string, string> kColorless = new EvictingCache<string, string>(RichText.kCacheMax);
		private static readonly char[] kNameEnds = new[] { Markup.kValueSeparator, Markup.kAttributeSeparator, Markup.kEnd };

		private static int m_nameMarkup = int.MaxValue;
		private static int m_nameVisible = int.MaxValue;

		internal struct UrlSpan
		{
			internal int m_renderedStart;
			internal int m_renderedLength;
			internal string m_url;
		}

		private static class Markup
		{
			internal const string kColor = "color";
			internal const string kBold = "b";
			internal const string kItalic = "i";
			internal const char kOpen = '<';
			internal const char kEnd = '>';
			internal const char kCloser = '/';
			internal const char kValueSeparator = '=';
			internal const char kAttributeSeparator = ' ';
			internal const string kColorPrefix = "<color=#";
			internal const char kNewline = '\n';
			internal const int kWireDigits = 6;
		}

		private readonly struct MarkupRules
		{
			internal readonly bool m_keepStyle;
			internal readonly bool m_dropUnknown;
			internal readonly bool m_wireHex;

			internal MarkupRules(bool keepStyle, bool dropUnknown, bool wireHex)
			{
				m_keepStyle = keepStyle;
				m_dropUnknown = dropUnknown;
				m_wireHex = wireHex;
			}
		}

		private readonly struct OpenSpan
		{
			internal readonly string m_name;
			internal readonly string m_tag;

			internal OpenSpan(string name, string tag)
			{
				m_name = name;
				m_tag = tag;
			}
		}

		// maps a string index to the same spot in the text imgui lays out once its tags are parsed away
		// imgui eats every tag shaped token including ones it cannot draw like link so all of them count
		internal static int visibleIndex(string text, int rawIndex)
		{
			int skipped = 0;
			foreach (Match tag in RichText.kTagRegex.Matches(text))
			{
				if (tag.Index >= rawIndex)
					break;

				skipped += FastMath.min(tag.Length, rawIndex - tag.Index);
			}
			return rawIndex - skipped;
		}

		internal static string wrapColor(string text, string hex) => $"{Markup.kColorPrefix}{hex}{Markup.kEnd}{text}{RichText.kColorClose}";
		internal static string wrapColor(string text, Color color) => $"{RichText.colorOpen(color)}{text}{RichText.kColorClose}";

		internal static string wrapColor(string text, (byte r, byte g, byte b) rgb)
		{
			StringBuilder builder = new StringBuilder(Markup.kColorPrefix.Length + text.Length + RichText.kColorClose.Length + Markup.kWireDigits + 1);
			RichText.appendColor(builder, rgb);
			return builder.Append(text).Append(RichText.kColorClose).ToString();
		}

		internal static string colorOpen(Color color) => $"{Markup.kColorPrefix}{ColorHex.of(color)}{Markup.kEnd}";

		internal static void appendColor(StringBuilder builder, (byte r, byte g, byte b) rgb)
		{
			builder.Append(Markup.kColorPrefix);

			ColorHex.append(builder, rgb);

			builder.Append(Markup.kEnd);
		}

		internal static string buildGradient(string text, (byte r, byte g, byte b)[] colors) => RichText.buildGradient(text, colors, true);

		internal static string buildGradient(string text, (byte r, byte g, byte b)[] colors, bool cyclic)
		{
			if (string.IsNullOrEmpty(text) || colors == null || colors.Length == 0)
				return text;

			int visibleCount = RichText.countGlyphs(text);
			if (visibleCount == 0)
				return text;

			StringBuilder builder = new StringBuilder(text.Length * RichText.kGradientChars);

			int glyphIndex = 0;

			int cursor = 0;
			foreach (Match tag in RichText.kTagRegex.Matches(text))
			{
				RichText.appendRun(builder, text, cursor, tag.Index, colors, visibleCount, cyclic, ref glyphIndex);

				builder.Append(tag.Value);

				cursor = tag.Index + tag.Length;
			}
			RichText.appendRun(builder, text, cursor, text.Length, colors, visibleCount, cyclic, ref glyphIndex);
			return builder.ToString();
		}

		internal static bool hasColor(string text) => !string.IsNullOrEmpty(text) && RichText.kColorOpen.IsMatch(text);

		internal static int countSpans(string text)
		{
			if (string.IsNullOrEmpty(text))
				return 0;

			return RichText.kColorOpen.Matches(text).Count;
		}

		internal static string lowercaseHex(string text)
		{
			if (string.IsNullOrEmpty(text))
				return text ?? string.Empty;

			return RichText.kColorOpen.Replace(text, RichText.lowerMatch);
		}

		internal static int countGlyphs(string text)
		{
			int cursor = 0;

			int count = 0;
			foreach (Match tag in RichText.kTagRegex.Matches(text))
			{
				count += RichText.countIn(text, cursor, tag.Index);

				cursor = tag.Index + tag.Length;
			}

			count += RichText.countIn(text, cursor, text.Length);
			return count;
		}

		internal static string escape(string text)
		{
			if (string.IsNullOrEmpty(text))
				return text;

			return text.Replace("<", RichText.kEscapedOpen);
		}

		internal static string unescape(string text)
		{
			if (string.IsNullOrEmpty(text))
				return text;

			return text.Replace(RichText.kEscapedOpen, "<");
		}

		internal static string toAscii(string text)
		{
			if (string.IsNullOrEmpty(text))
				return text ?? string.Empty;

			int length = text.Length;

			StringBuilder builder = null;
			for (int i = 0; i < length; i++)
			{
				char c = text[i];

				bool keep = c >= RichText.kFirstPrintable && c <= RichText.kLastPrintable || c == '\n' || c == '\t';
				if (keep)
					builder?.Append(c);
				else if (builder == null)
				{
					builder = new StringBuilder(length);
					builder.Append(text, 0, i);
				}
			}
			return builder == null ? text : builder.ToString();
		}

		internal static string fromGameHex(string text)
		{
			if (string.IsNullOrEmpty(text))
				return text ?? string.Empty;

			return RichText.balanceMarkup(RichText.expandBrackets(text), RichText.kWireRules);
		}

		internal static string toGameHexSource(string text)
		{
			if (string.IsNullOrEmpty(text))
				return text ?? string.Empty;

			string balanced = RichText.balanceMarkup(text, RichText.kWireRules);

			string bracketed = RichText.kColorOpen.Replace(balanced, RichText.toBracket);
			return RichText.kColorCloseRegex.Replace(bracketed, (Match match) =>
			{
				int next = match.Index + match.Length;
				return next >= bracketed.Length || bracketed[next] == RichText.kBracketOpen ? string.Empty : RichText.kBracketReset;
			});
		}

		internal static string stripTags(string text)
		{
			if (string.IsNullOrEmpty(text))
				return text ?? string.Empty;
			if (RichText.kStripped.tryGetValue(text, out string cached))
				return cached;

			string result = RichText.kTagRegex.Replace(text, string.Empty);
			RichText.kStripped.set(text, result);
			return result;
		}

		internal static string stripColors(string text)
		{
			if (string.IsNullOrEmpty(text))
				return text ?? string.Empty;
			if (text.IndexOf(Markup.kColor, StringComparison.Ordinal) < 0)
				return text;
			if (RichText.kColorless.tryGetValue(text, out string cached))
				return cached;

			string result = RichText.kColorTag.Replace(text, string.Empty);
			RichText.kColorless.set(text, result);
			return result;
		}

		internal static string stripMarkup(string text)
		{
			if (string.IsNullOrEmpty(text))
				return text ?? string.Empty;

			string stripped = RichText.kTagRegex.Replace(text, string.Empty);
			return RichText.kBracketSpan.Replace(stripped, string.Empty);
		}

		internal static string visibleText(string text)
		{
			if (string.IsNullOrEmpty(text))
				return text ?? string.Empty;

			return RichText.stripTags(RichText.fromGameHex(text));
		}

		// player names stay uncapped until the host sets its caps
		internal static void nameLimits(int maxMarkup, int maxVisible)
		{
			RichText.m_nameMarkup = maxMarkup;
			RichText.m_nameVisible = maxVisible;
		}

		internal static string displayName(string text) => RichText.sanitizeName(text, RichText.m_nameMarkup, RichText.m_nameVisible);

		internal static string keepColors(string text)
		{
			if (string.IsNullOrEmpty(text))
				return text ?? string.Empty;

			return RichText.balanceMarkup(RichText.fromGameHex(text), RichText.kColorRules);
		}

		internal static string normalizeMarkup(string text) => RichText.balanceMarkup(text, RichText.kDisplayRules);

		internal static string truncateVisible(string text, int maxVisible)
		{
			if (string.IsNullOrEmpty(text))
				return text ?? string.Empty;

			StringBuilder builder = new StringBuilder(text.Length);

			int position = 0;

			int visible = 0;
			foreach (Match tag in RichText.kTagRegex.Matches(text))
			{
				if (tag.Index > position)
				{
					int gap = tag.Index - position;

					int take = RichText.safeTake(text, position, Math.Min(maxVisible - visible, gap));
					builder.Append(text, position, take);

					visible += take;
					if (take < gap || visible >= maxVisible)
						return RichText.balanceMarkup(builder.ToString(), RichText.kDisplayRules);
				}

				builder.Append(tag.Value);

				position = tag.Index + tag.Length;
			}

			int tail = RichText.safeTake(text, position, Math.Min(maxVisible - visible, text.Length - position));
			if (tail == text.Length - position)
				return text;

			builder.Append(text, position, tail);
			return RichText.balanceMarkup(builder.ToString(), RichText.kDisplayRules);
		}

		internal static string sanitizeName(string text, int maxTotal, int maxVisible)
		{
			string cleaned = RichText.stripControls(text, maxTotal);

			string sanitized = RichText.balanceMarkup(RichText.fromGameHex(cleaned), RichText.kNameRules);

			string truncated = RichText.truncateVisible(sanitized, maxVisible);
			FontGlyphCache.logMissing(truncated, nameof(RichText.sanitizeName));
			return truncated;
		}

		internal static string decorateUrls(string formatted, List<UrlSpan> spans, string hex, int maxSpans)
		{
			spans.Clear();

			if (string.IsNullOrEmpty(formatted))
				return formatted ?? string.Empty;

			bool[] inTag = RichText.markRanges(formatted);

			MatchCollection matches = RichText.kUrlRegex.Matches(formatted);
			if (matches.Count == 0)
				return formatted;

			StringBuilder builder = new StringBuilder(formatted.Length + RichText.kUrlSlack);

			int produced = 0;

			const string kSuffix = "</u></color>";

			int cursor = 0;
			for (int i = 0; i < matches.Count && spans.Count < maxSpans; i++)
			{
				Match match = matches[i];

				int rawStart = match.Index;
				if (inTag[rawStart])
					continue;

				string raw = match.Value;

				string trimmed = RichText.trimPunctuation(raw);
				if (trimmed.Length < RichText.kMinUrl)
					continue;

				int copyLength = rawStart - cursor;
				if (copyLength > 0)
				{
					builder.Append(formatted, cursor, copyLength);

					produced += copyLength;
				}

				string url = trimmed.Length > RichText.kMaxUrl ? trimmed[..RichText.kMaxUrl] : trimmed;

				string href = url.StartsWith(RichText.kWebPrefix, StringComparison.OrdinalIgnoreCase) ? $"{RichText.kHttpsScheme}{url}" : url;

				string prefix = $"{Markup.kColorPrefix}{hex}{Markup.kEnd}<u>";
				builder.Append(prefix);

				int spanStart = produced + prefix.Length;

				builder.Append(url);
				builder.Append(kSuffix);

				spans.Add(new UrlSpan
				{
					m_renderedStart = spanStart,
					m_renderedLength = url.Length,
					m_url = href
				});

				produced += prefix.Length + url.Length + kSuffix.Length;

				cursor = rawStart + trimmed.Length;
			}

			if (cursor < formatted.Length)
				builder.Append(formatted, cursor, formatted.Length - cursor);
			return builder.ToString();
		}

		private static string lowerMatch(Match match) => match.Value.ToLowerInvariant();
		private static string toBracket(Match match) => $"{RichText.kBracketOpen}{match.Groups[1].Value.TrimStart(ColorHex.kPrefix).ToUpperInvariant()}{RichText.kBracketClose}";

		private static void appendRun(StringBuilder builder, string text, int start, int end, (byte r, byte g, byte b)[] colors, int visibleCount, bool cyclic, ref int glyphIndex)
		{
			int i = start;
			while (i < end)
			{
				int step = char.IsHighSurrogate(text[i]) && i + 1 < end && char.IsLowSurrogate(text[i + 1]) ? 2 : 1;

				float t = EasingMath.indexFraction(glyphIndex, visibleCount);
				(byte r, byte g, byte b) = cyclic ? ColorMath.interpolate(colors, t) : ColorMath.interpolateLinear(colors, t);
				RichText.appendColor(builder, (r, g, b));

				builder.Append(text, i, step);
				builder.Append(RichText.kColorClose);

				glyphIndex++;

				i += step;
			}
		}

		private static int countIn(string text, int start, int end)
		{
			int i = start;

			int count = 0;
			while (i < end)
			{
				i += char.IsHighSurrogate(text[i]) && i + 1 < end && char.IsLowSurrogate(text[i + 1]) ? 2 : 1;

				count++;
			}
			return count;
		}

		// vanilla HexColor ends a span at the next bracket so flat siblings are what it sends
		private static string expandBrackets(string text)
		{
			bool spanOpen = false;
			return RichText.kBracketSpan.Replace(text, (Match match) =>
			{
				string closer = spanOpen ? RichText.kColorClose : string.Empty;

				string value = match.Groups[1].Value;
				if (value == RichText.kResetCapture)
				{
					spanOpen = false;
					return closer;
				}

				spanOpen = true;
				return $"{closer}{Markup.kColorPrefix}{value}{Markup.kEnd}";
			});
		}

		// cutting between a surrogate pair leaves a lone half the font cant draw
		private static int safeTake(string text, int start, int take)
		{
			if (take <= 0)
				return 0;

			int end = start + take;

			bool splitsPair = end < text.Length && char.IsLowSurrogate(text[end]) && char.IsHighSurrogate(text[end - 1]);
			return splitsPair ? take - 1 : take;
		}

		// peers send colours vanilla never checks
		private static string balanceMarkup(string text, in MarkupRules rules)
		{
			if (string.IsNullOrEmpty(text))
				return text ?? string.Empty;

			string trimmed = RichText.trimDangling(text);

			MatchCollection tags = RichText.kTagRegex.Matches(trimmed);
			if (tags.Count == 0)
				return trimmed;

			StringBuilder builder = new StringBuilder(trimmed.Length);

			List<OpenSpan> open = new List<OpenSpan>();

			int cursor = 0;
			foreach (Match tag in tags)
			{
				builder.Append(trimmed, cursor, tag.Index - cursor);

				cursor = tag.Index + tag.Length;

				RichText.appendTag(builder, open, tag.Value, rules);
			}
			builder.Append(trimmed, cursor, trimmed.Length - cursor);
			for (int i = open.Count - 1; i >= 0; i--)
				RichText.appendCloser(builder, open[i].m_name);
			return builder.ToString();
		}

		private static void appendTag(StringBuilder builder, List<OpenSpan> open, string tag, in MarkupRules rules)
		{
			bool closing = tag[1] == Markup.kCloser;

			int nameStart = closing ? 2 : 1;

			int nameEnd = tag.IndexOfAny(RichText.kNameEnds, nameStart);

			ReadOnlySpan<char> name = tag.AsSpan(nameStart, nameEnd - nameStart);
			if (name.Equals(Markup.kColor, StringComparison.OrdinalIgnoreCase))
			{
				if (closing)
					RichText.popSpan(builder, open, Markup.kColor);
				else if (tag[nameEnd] == Markup.kValueSeparator)
					RichText.pushColor(builder, open, tag[(nameEnd + 1)..^1], rules);
				return;
			}

			// imgui prints the tmp shorthand raw
			if (!closing && name.Length > 0 && name[0] == ColorHex.kPrefix)
			{
				RichText.pushColor(builder, open, tag[nameStart..^1], rules);
				return;
			}

			string style = RichText.styleName(name);
			if (style != null && tag[nameEnd] == Markup.kEnd)
			{
				if (!rules.m_keepStyle)
					return;

				if (closing)
					RichText.popSpan(builder, open, style);
				else
					RichText.pushSpan(builder, open, style, $"{Markup.kOpen}{style}{Markup.kEnd}");
				return;
			}

			if (rules.m_dropUnknown)
				return;

			builder.Append(tag);
		}

		private static void pushColor(StringBuilder builder, List<OpenSpan> open, string value, in MarkupRules rules)
		{
			if (!ColorHex.tryExpand(value, out string digits))
				return;

			RichText.pushSpan(builder, open, Markup.kColor, $"{Markup.kColorPrefix}{(rules.m_wireHex ? digits[..Markup.kWireDigits] : digits)}{Markup.kEnd}");
		}

		private static string styleName(ReadOnlySpan<char> name)
		{
			if (name.Equals(Markup.kBold, StringComparison.OrdinalIgnoreCase))
				return Markup.kBold;
			if (name.Equals(Markup.kItalic, StringComparison.OrdinalIgnoreCase))
				return Markup.kItalic;

			return null;
		}

		private static void pushSpan(StringBuilder builder, List<OpenSpan> open, string name, string tag)
		{
			open.Add(new OpenSpan(name, tag));

			builder.Append(tag);
		}

		private static void popSpan(StringBuilder builder, List<OpenSpan> open, string name)
		{
			int target = open.Count - 1;
			while (target >= 0 && open[target].m_name != name)
				target--;
			if (target < 0)
				return;

			for (int i = open.Count - 1; i > target; i--)
				RichText.appendCloser(builder, open[i].m_name);

			RichText.appendCloser(builder, name);

			open.RemoveAt(target);
			for (int i = target; i < open.Count; i++)
				builder.Append(open[i].m_tag);
		}

		private static void appendCloser(StringBuilder builder, string name) => builder.Append(Markup.kOpen).Append(Markup.kCloser).Append(name).Append(Markup.kEnd);

		private static string trimDangling(string text)
		{
			int open = text.LastIndexOf(Markup.kOpen);
			if (open < 0 || text.IndexOf(Markup.kEnd, open) >= 0)
				return text;

			ReadOnlySpan<char> fragment = text.AsSpan(open + 1);

			bool markup = fragment.Length > 0 && fragment[0] == Markup.kCloser || fragment.StartsWith(Markup.kColor, StringComparison.OrdinalIgnoreCase);
			return markup ? text[..open] : text;
		}

		internal static string stripControls(string text, int maxLength)
		{
			if (string.IsNullOrEmpty(text))
				return string.Empty;

			string limited = text.Length <= maxLength ? text : RichText.trimTag(text[..maxLength]);
			return RichText.kControlChars.Replace(RichText.kSpaceControl.Replace(limited, " "), string.Empty);
		}

		private static string trimTag(string text)
		{
			int open = text.LastIndexOf(Markup.kOpen);
			if (open < 0 || text.IndexOf(Markup.kEnd, open) >= 0)
				return text;

			return text[..open];
		}

		private static bool[] markRanges(string text)
		{
			bool[] flags = new bool[text.Length];

			int depth = 0;
			for (int i = 0; i < text.Length; i++)
			{
				char c = text[i];
				if (c == Markup.kOpen)
					depth++;

				if (depth > 0)
					flags[i] = true;

				if (!(c == Markup.kEnd && depth > 0))
					continue;

				depth--;
			}
			return flags;
		}

		private static string trimPunctuation(string url) => RichText.kTrailingPunct.Replace(url, string.Empty);
	}
}
