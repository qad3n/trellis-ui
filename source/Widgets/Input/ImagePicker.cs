using System;
using UnityEngine;
using static Trellis.MenuHost;
using static Trellis.Widgets;

namespace Trellis
{
	internal sealed class ImagePicker
	{
		private string[] m_images = Array.Empty<string>();
		private string m_indexedName;
		private int m_index = -1;
		private bool m_scanned;

		private static class Layout
		{
			internal const float kStatusGap = 4f;
		}

		private static class Labels
		{
			internal const string kRefresh = "R";
			internal const string kRefreshTooltip = "Re-scan mod/data/menu_backgrounds for images.";
			internal const string kEmpty = "<i>No images found.</i>";
		}

		internal void draw(ref float y, float w, string label, string selected, string status, Action<string> onSelected, Action onRefreshed)
		{
			updateIndex(selected);

			MenuRowFlow.RowScope row = Widgets.beginRow(ref y, RowPrefix.kDropdown, label);

			float startY = y;

			float refreshReserve = Widgets.kRowEndReserve;
			if (m_images.Length > 0)
			{
				int next = Widgets.drawDropdown(ref y, w, label, m_images, m_index, refreshReserve);
				if (next != m_index && next >= 0)
				{
					m_index = next;
					m_indexedName = m_images[next];

					onSelected(m_images[next]);
				}
			}
			else
			{
				Widgets.fittedLabel(RectMath.create(MenuHost.m_blockOffset.x, MenuHost.m_blockOffset.y + y, w - refreshReserve, MenuHost.kFieldH), Labels.kEmpty, Theme.label, TextAnchor.MiddleLeft);

				y += MenuHost.kFieldH + Layout.kStatusGap;
			}

			if (Widgets.drawRowEndButton(startY, w, Labels.kRefresh, Labels.kRefreshTooltip))
			{
				rescan();

				onRefreshed();
			}

			Widgets.wrappedLabel(MenuHost.m_blockOffset.x, MenuHost.m_blockOffset.y + y, w, status, Theme.valueLabel, out float statusH);
			y += statusH + Layout.kStatusGap;
			Widgets.endRow(ref y, ref row);
		}

		private void rescan()
		{
			m_images = UiImages.list();
			m_indexedName = null;
			m_index = -1;
			m_scanned = true;
		}

		private void updateIndex(string selected)
		{
			if (m_scanned && m_indexedName == selected)
				return;

			if (!m_scanned)
				rescan();

			m_indexedName = selected;
			m_index = Array.IndexOf(m_images, selected);
		}
	}
}
