/**
 * FileData.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the DSO Sharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using DSO.Disassembler;

namespace DSO.Loader
{
	public class StringTableEntry(string value, uint index, bool global)
	{
		public readonly string Value = value;
		public readonly uint Index = index;
		public readonly bool Global = global;

		static public bool operator==(StringTableEntry? entry1, StringTableEntry? entry2) => entry1?.Value == entry2?.Value;
		static public bool operator!=(StringTableEntry? entry1, StringTableEntry? entry2) => entry1?.Value != entry2?.Value;

		public static implicit operator string?(StringTableEntry? entry) => entry?.Value ?? null;

		public override bool Equals(object? obj) => obj is StringTableEntry entry && entry.Value.Equals(Value) && entry.Index.Equals(Index) && entry.Global.Equals(global);
		public override int GetHashCode() => base.GetHashCode() ^ Value.GetHashCode() ^ Index.GetHashCode() ^ Global.GetHashCode();
		public override string ToString() => Value;
	}

	public class StringTable()
	{
		private readonly Dictionary<uint, StringTableEntry> _table = [];

		public string RawString { get; private set; } = "";
		public readonly bool Global;

		public int Count => _table.Count;
		public int Size => RawString.Length;

		public StringTableEntry this[uint index] => Get(index);

		public StringTable(string rawStr, bool global) : this()
		{
			_table = [];
			RawString = rawStr;
			Global = global;

			uint index = 0;
			string str = "";

			var length = RawString.Length;

			for (int i = 0; i < length; i++)
			{
				var ch = RawString[i];

				if (ch == '\0')
				{
					_table[index] = new(str, index, global);

					str = "";
					index = (uint) i + 1;
				}
				else
				{
					str += ch;
				}
			}
		}

		public StringTableEntry? Get(uint index)
		{
			if (Has(index))
			{
				return _table[index];
			}

			if (index >= RawString.Length)
			{
				return null;
			}

			/* If we can't find it in the lookup table, let's search for it in the raw string (this is very slow). */

			var substring = RawString[(int) index..];
			var end = substring.IndexOf('\0');

			return new(substring[..end], index, Global);
		}

		public bool Has(uint index) => _table.ContainsKey(index);

		public void Visit(DisassemblyWriter writer)
		{
			writer.WriteCommentLine("------------------------------------------------------------------------");
			writer.WriteCommentLine("");
			writer.WriteCommentLine($" {(Global ? "Global" : "Function")} String Table ({Count} {(Count == 1 ? "entry" : "entries")})");
			writer.WriteCommentLine("");

			foreach (var (address, str) in _table)
			{
				writer.WriteCommentLine(string.Format("     {0,-16}    =>    \"{1}\"", address, Util.String.EscapeString(str.Value)));
			}

			if (Count > 0)
			{
				writer.WriteCommentLine("");
			}
		}
	}

	public class FloatTable(uint size, bool global)
	{
		private readonly double[] _table = new double[size];
		private readonly Dictionary<double, uint> _indices = [];
		public readonly bool Global = global;

		public int Count => _table.Length;

		public double this[uint index]
		{
			get => _table[index];
			set
			{
				_table[index] = value;
				_indices[value] = index;
			}
		}

		public uint this[double number] => _indices[number];

		public void Visit(DisassemblyWriter writer)
		{
			writer.WriteCommentLine("------------------------------------------------------------------------");
			writer.WriteCommentLine("");
			writer.WriteCommentLine($" {(Global ? "Global" : "Function")} Float Table ({Count} {(Count == 1 ? "entry" : "entries")})");
			writer.WriteCommentLine("");

			for (var i = 0; i < _table.Length; i++)
			{
				writer.WriteCommentLine(string.Format("     {0,-16}    =>    {1}", i, _table[i]));
			}

			if (Count > 0)
			{
				writer.WriteCommentLine("");
			}
		}
	}

	public class FileData(uint version)
	{
		public readonly uint Version = version;

		public StringTable GlobalStringTable { get; set; } = new();
		public StringTable FunctionStringTable { get; set; } = new();

		public FloatTable GlobalFloatTable = new(0, global: true);
		public FloatTable FunctionFloatTable = new(0, global: false);

		public readonly Dictionary<uint, uint> IdentifierTable = [];

		public uint[] Code { get; set; } = [];

		public virtual void Visit(DisassemblyWriter writer)
		{
			GlobalStringTable.Visit(writer);
			FunctionStringTable.Visit(writer);
			GlobalFloatTable.Visit(writer);
			FunctionFloatTable.Visit(writer);
		}
	}
}
