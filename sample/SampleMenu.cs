using UnityEngine;

namespace TrellisSample
{
	internal enum SampleMode
	{
		Calm,
		Busy
	}

	internal static class SampleMenu
	{
		internal const KeyCode kToggleKey = KeyCode.F1;

		private static readonly string[] kModes = new[] { "Calm", "Busy" };

		// blocks pack into as many columns as the window fits
		private static readonly Page kMain = Ui.page(SampleMenu.drawWelcome, SampleMenu.drawOptions, SampleMenu.drawDanger);

		private static bool m_enabled = true;
		private static float m_speed = 1f;
		private static SampleMode m_mode;
		private static KeyChord m_boostKey = new KeyChord(KeyCode.LeftShift);

		internal static void install()
		{
			MenuHost.setTitle("<b>SAMPLE</b>", "Trellis");
			MenuHost.setTabs(new[] { new TabRow("MAIN", SampleMenu.kMain.draw) });
			MenuHost.setToggle(key => key == SampleMenu.kToggleKey);
		}

		private static void drawWelcome()
		{
			Ui.header("Welcome");
			Ui.note("Every block is a plain method. Widgets stack down on their own.");

			using (Ui.Row bar = Ui.row(6f))
				Gui.drawTexture(bar.rect, Theme.accentBar);
		}

		private static void drawOptions()
		{
			Ui.header("Options");
			Ui.toggle(ref SampleMenu.m_enabled, "Enabled", "A plain toggle row.");

			using (Ui.Reveal open = Ui.reveal(SampleMenu.m_enabled))
			{
				if (!open)
					return;

				Ui.slider(ref SampleMenu.m_speed, "Speed", 0f, 4f);
				Ui.dropdown(ref SampleMenu.m_mode, "Mode", SampleMenu.kModes);
				Ui.keybind(ref SampleMenu.m_boostKey, "Boost Key");
			}
		}

		private static void drawDanger()
		{
			Ui.header("Danger");

			using (Ui.disabled(!SampleMenu.m_enabled))
				Ui.button("Reset", () => MenuHost.showConfirm("Reset every option?", SampleMenu.reset));
		}

		private static void reset()
		{
			SampleMenu.m_enabled = true;
			SampleMenu.m_speed = 1f;
			SampleMenu.m_mode = SampleMode.Calm;
			SampleMenu.m_boostKey = new KeyChord(KeyCode.LeftShift);
		}
	}
}
