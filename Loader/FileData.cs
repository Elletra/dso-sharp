/**
 * FileData.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the DSO Sharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

namespace DSO.Loader
{
	public class StringTable
	{
		private readonly Dictionary<uint, string> table = [];

		public string RawString { get; private set; } = "";

		public int Size => RawString.Length;
		public string this[uint index] => Get(index);

		public StringTable() { }

		public StringTable(string rawStr)
		{
			table = [];
			RawString = rawStr;

			uint index = 0;
			string str = "";

			var length = RawString.Length;

			for (int i = 0; i < length; i++)
			{
				var ch = RawString[i];

				if (ch == '\0')
				{
					table[index] = str;

					str = "";
					index = (uint) i + 1;
				}
				else
				{
					str += ch;
				}
			}
		}

		public string? Get(uint index)
		{
			if (Has(index))
			{
				return table[index];
			}

			if (index >= RawString.Length)
			{
				return null;
			}

			/* If we can't find it in the lookup table, let's search for it in the raw string (this is very slow). */

			var substring = RawString[(int) index..];
			var end = substring.IndexOf('\0');

			return substring[..end];
		}

		public bool Has(uint index) => table.ContainsKey(index);
	}

	public class FileData(uint version)
	{
		public readonly uint Version = version;

		public StringTable GlobalStringTable { get; set; } = new();
		public StringTable FunctionStringTable { get; set; } = new();

		public double[] GlobalFloatTable = [];
		public double[] FunctionFloatTable = [];

		public readonly Dictionary<uint, uint> IdentifierTable = [];

		public uint[] Code { get; set; } = [];
	}
}
