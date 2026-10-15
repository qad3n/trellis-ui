using UnityEngine;

namespace TrellisSample
{
	internal enum SampleBlock
	{
		Welcome,
		Options,
		Danger
	}

	internal static class SampleMenu
	{
		internal const KeyCode kToggleKey = KeyCode.F1;

		// blocks pack into as many columns as the window fits
		private static readonly SampleBlock[] kMainBlocks = new[] { SampleBlock.Welcome, SampleBlock.Options, SampleBlock.Danger };

		private static readonly (SampleBlock id, BlockDraw draw)[] kBlocks = new (SampleBlock id, BlockDraw draw)[] { (SampleBlock.Welcome, SampleMenu.drawWelcome), (SampleBlock.Options, SampleMenu.drawOptions), (SampleBlock.Danger, SampleMenu.drawDanger) };

		private static readonly string[] kModes = new[] { "Calm", "Busy" };

		private static bool m_enabled = true;
		private static float m_speed = 1f;
		private static int m_mode;

		internal static void install()
		{
			MenuHost.setTitle("<b>SAMPLE</b>", "Trellis");
			MenuHost.setTabs(new[] { new TabRow("MAIN", (w, viewH) => MenuHost.drawBlocks(w, 0f, SampleMenu.kMainBlocks)) });
			MenuHost.addBlocks(SampleMenu.kBlocks);
			MenuHost.setToggle(key => key == SampleMenu.kToggleKey);
		}

		private static void drawWelcome(ref float y, float w)
		{
			Widgets.drawSectionHeader(ref y, w, "Welcome");
			MenuHost.drawNote(ref y, w, "Every block is a draw function. Rows stack down from y.");
		}

		private static void drawOptions(ref float y, float w)
		{
			Widgets.drawSectionHeader(ref y, w, "Options");
			SampleMenu.m_enabled = Widgets.drawToggle(ref y, w, SampleMenu.m_enabled, "Enabled", tooltip: "A plain toggle row.");
			SampleMenu.m_speed = Widgets.drawSlider(ref y, w, "Speed", SampleMenu.m_speed, 0f, 4f);
			SampleMenu.m_mode = Widgets.drawDropdown(ref y, w, "Mode", SampleMenu.kModes, SampleMenu.m_mode);
		}

		private static void drawDanger(ref float y, float w)
		{
			Widgets.drawSectionHeader(ref y, w, "Danger");
			Widgets.drawButton(ref y, w, "Reset", Theme.dangerButton, () => MenuHost.showConfirm("Reset every option?", SampleMenu.reset));
		}

		private static void reset()
		{
			SampleMenu.m_enabled = true;
			SampleMenu.m_speed = 1f;
			SampleMenu.m_mode = 0;
		}
	}
}
