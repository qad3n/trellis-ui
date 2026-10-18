namespace Trellis
{
	internal static partial class Widgets
	{
		internal static class RowPrefix
		{
			internal const string kToggle = "r.tgl";
			internal const string kTextField = "r.tf";
			internal const string kTextArea = "r.ta";
			internal const string kPassword = "r.pw";
			internal const string kDropdown = "r.dd";
			internal const string kSlider = "r.sld";
			internal const string kKeybind = "r.key";
			internal const string kMultiSelect = "r.ms";
			internal const string kColor = "r.col";
			internal const string kInfo = "r.info";
			internal const string kButton = "r.btn";
			internal const string kHeader = "r.hdr";
			internal const string kSubHeader = "r.sub";
			internal const string kDisclosure = "r.dsc";
			internal const string kLabel = "r.lbl";
			internal const string kBanner = "r.ban";
			internal const string kDivider = "r.div";
			internal const string kChips = "r.chip";
			internal const string kList = "r.list";
			internal const string kCustom = "r.ui";
			internal const string kGroup = "r.grp";
		}

		internal static MenuRowFlow.RowScope beginRow(ref float y, string prefix, string label) => MenuRowFlow.beginRow(ref y, Widgets.rowKey(prefix, label));
		internal static void endRow(ref float y, ref MenuRowFlow.RowScope scope) => MenuRowFlow.endRow(ref y, ref scope);
		internal static MenuRowFlow.RowScope beginRow(ref float y, int key) => MenuRowFlow.beginRow(ref y, key);

		private static int rowKey(string prefix, string label)
		{
			if (!MenuHost.m_inBlock)
				return 0;

			int occurrence = Widgets.nextOccurrence(prefix, label);

			int key;
			unchecked
			{
				key = (int)MenuHost.m_currentBlock;
				key = (key * Widgets.kKeyMix) ^ prefix.GetHashCode();
				key = (key * Widgets.kKeyMix) ^ (label?.GetHashCode() ?? 0);
				key = (key * Widgets.kKeyMix) ^ occurrence;
			}
			return key == 0 ? Widgets.kKeyMix : key;
		}
	}
}
