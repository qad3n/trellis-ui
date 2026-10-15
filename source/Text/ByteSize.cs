namespace Trellis
{
	internal static class ByteSize
	{
		internal const long kKilobyte = 1024L;
		internal const long kMegabyte = ByteSize.kKilobyte * ByteSize.kKilobyte;

		internal static string format(long bytes)
		{
			if (bytes >= ByteSize.kMegabyte)
				return $"{bytes / (float)ByteSize.kMegabyte:F1} MB";
			if (bytes >= ByteSize.kKilobyte)
				return $"{bytes / (float)ByteSize.kKilobyte:F0} KB";

			return $"{bytes} B";
		}
	}
}
