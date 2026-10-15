using System;

namespace Trellis
{
	internal static class RowTable
	{
		internal static TValue[] column<TRow, TValue>(TRow[] rows, Func<TRow, TValue> field)
		{
			TValue[] column = new TValue[rows.Length];
			for (int i = 0; i < rows.Length; i++)
				column[i] = field(rows[i]);
			return column;
		}

		internal static TValue[] select<TRow, TValue>(TRow[] rows, Func<TRow, bool> keep, Func<TRow, TValue> field)
		{
			int count = 0;
			for (int i = 0; i < rows.Length; i++)
			{
				if (keep(rows[i]))
					count++;
			}

			TValue[] column = new TValue[count];
			int next = 0;
			for (int i = 0; i < rows.Length; i++)
			{
				if (keep(rows[i]))
					column[next++] = field(rows[i]);
			}
			return column;
		}

		internal static TRow at<TRow>(TRow[] rows, int index, TRow fallback) => index >= 0 && index < rows.Length ? rows[index] : fallback;
	}
}
