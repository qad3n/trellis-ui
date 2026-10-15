namespace Trellis
{
	internal static class ThemeSlots
	{
		// update with the last enum member since persisted arrays are sized off it
		internal const int kCount = (int)ThemeSlot.HoverText + 1;
		private const string kPickerPrefix = "theme_slot_";

		private static readonly string[] kPickerIds = ThemeSlots.buildIds();

		internal static string pickerId(ThemeSlot slot) => ThemeSlots.kPickerIds[(int)slot];
		internal static string pickerId(int index) => ThemeSlots.kPickerIds[index];

		private static string[] buildIds()
		{
			string[] ids = new string[ThemeSlots.kCount];
			for (int i = 0; i < ids.Length; i++)
				ids[i] = $"{ThemeSlots.kPickerPrefix}{i}";
			return ids;
		}
	}
}
