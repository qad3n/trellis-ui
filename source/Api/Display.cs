using System.Collections.Generic;
using UnityEngine;

namespace Trellis
{
	// rows that only read state
	internal static partial class Ui
	{
		internal static void header(string text, string tooltip = null, string trailing = null) => Widgets.drawSectionHeader(ref Ui.m_y, Ui.m_w, text, tooltip, trailing);
		internal static void subHeader(string text, string tooltip = null) => Widgets.drawSubHeader(ref Ui.m_y, Ui.m_w, text, tooltip);
		internal static void status(string text) => MenuHost.drawStatus(ref Ui.m_y, Ui.m_w, text);
		internal static void note(string text) => MenuHost.drawNote(ref Ui.m_y, Ui.m_w, text);
		internal static void note(string text, Color tone) => MenuHost.drawNote(ref Ui.m_y, Ui.m_w, text, tone);
		internal static void info(string label, string value, string tooltip = null) => Widgets.drawInfoRow(ref Ui.m_y, Ui.m_w, label, value, tooltip);
		internal static void banner(string title, string detail, Color accent) => MenuHost.drawBanner(ref Ui.y, Ui.width, title, detail, accent);
		internal static void wrapped(string text, GUIStyle baseStyle = null, float gap = Widgets.kFieldGap) => MenuHost.drawWrapped(ref Ui.y, Ui.width, text, baseStyle, gap);
		internal static void divider() => Widgets.drawDivider(ref Ui.m_y, Ui.m_w);
		internal static void loading(string text) => Widgets.drawLoadingLine(ref Ui.m_y, Ui.m_w, text);
		internal static int chips(IReadOnlyList<string> labels, Color color, string tooltip = null) => Widgets.drawChipFlow(ref Ui.m_y, Ui.m_w, labels, color, tooltip);
	}
}
