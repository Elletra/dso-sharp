using System.Collections.Generic;

namespace DSO.Decompiler.Loader
{
	public class FileData
	{
		public class StringTable
		{
			private Dictionary<uint, string> table;

			public string RawString { get; private set; }

			public int Size => RawString.Length;
			public string this[uint index] => Get(index);

			public StringTable(string rawStr)
			{
				table = new Dictionary<uint, string>();
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

			public string Get(uint index) => Has(index) ? table[index] : null;
			public bool Has(uint index) => table.ContainsKey(index);
		}

		private uint version;

		private uint[] code;

		private StringTable globalStrings;
		private StringTable functionStrings;

		private double[] globalFloats;
		private double[] functionFloats;

		private Dictionary<uint, uint> identifierTable = new();

		private List<uint> lineBreakPairs = new();

		public uint Version => version;
		public int CodeSize => code.Length;

		public FileData(uint version)
		{
			this.version = version;
		}

		public void InitCode(uint size) => code = new uint[size];

		public bool HasString(uint index, bool global) => (global ? globalStrings : functionStrings).Has(index);
		public bool HasFloat(uint index, bool global) => index < (global ? globalFloats : functionFloats).Length;
		public bool HasIdentifierAt(uint addr) => identifierTable.ContainsKey(addr);

		public string GetStringTableValue(uint index, bool global) => (global ? globalStrings : functionStrings)[index];
		public double GetFloatTableValue(uint index, bool global) => (global ? globalFloats : functionFloats)[index];
		public string GetIdentifer(uint addr, uint index) => HasIdentifierAt(addr) ? GetStringTableValue(index, true) : null;
		public uint GetOp(uint index) => code[index];

		public void SetFloat(uint index, double value, bool global) => (global ? globalFloats : functionFloats)[index] = value;
		public void SetIdentifier(uint addr, uint index) => identifierTable[addr] = index;
		public void SetOp(uint index, uint op) => code[index] = op;

		public void AddLineBreakPair(uint value) => lineBreakPairs.Add(value);

		public void CreateStringTable(string table, bool global)
		{
			if (global)
			{
				globalStrings = new StringTable(table);
			}
			else
			{
				functionStrings = new StringTable(table);
			}
		}

		public void CreateFloatTable(uint size, bool global)
		{
			if (global)
			{
				globalFloats = new double[size];
			}
			else
			{
				functionFloats = new double[size];
			}
		}
	}
}
