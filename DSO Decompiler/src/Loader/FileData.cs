using System.Collections.Generic;

namespace DSODecompiler.Loader
{
	public class FileData
	{
		public class StringTable
		{
			protected Dictionary<uint, string> table;
			protected string rawString;

			public StringTable (string rawStr)
			{
				table = new Dictionary<uint, string> ();
				rawString = rawStr;

				uint index = 0;
				string str = "";

				var length = rawString.Length;

				for (int i = 0; i < length; i++)
				{
					var ch = rawString[i];

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

			public int Size => rawString.Length;
			public string RawString => rawString;
			public string this[uint index] => table[index];

			public string At (uint index) => table[index];
			public bool Has (uint index) => table.ContainsKey (index);
		}

		protected uint version;

		protected uint[] code;

		protected StringTable globalStrings;
		protected StringTable funcStrings;

		protected double[] globalFloats;
		protected double[] funcFloats;

		protected Dictionary<uint, uint> identTable = new Dictionary<uint, uint> ();

		protected List<uint> lineBreakPairs = new List<uint> ();

		public uint Version => version;

		public FileData (uint version)
		{
			this.version = version;
		}

		/**
		 * Methods for getting various values
		 */

		public string StringTableValue (uint index, bool global) => (global ? globalStrings : funcStrings)[index];
		public double FloatTableValue (uint index, bool global) => (global ? globalFloats : funcFloats)[index];
		public string Identifer (uint addr, uint index) => HasIdentifierAt (addr) ? StringTableValue (index, true) : null;
		public uint Op (uint index) => code[index];

		/**
		 * Methods for setting/initializing/clearing various sections
		 */

		public void SetStringTable (string table, bool global)
		{
			if (global)
			{
				globalStrings = new StringTable (table);
			}
			else
			{
				funcStrings = new StringTable (table);
			}
		}

		public void InitFloatTable (uint size, bool global)
		{
			if (global)
			{
				globalFloats = new double[size];
			}
			else
			{
				funcFloats = new double[size];
			}
		}

		public void InitCode (uint size) => code = new uint[size];
		public void ClearLineBreaks () => lineBreakPairs.Clear ();
		public void ClearIdentTable () => identTable.Clear ();

		/**
		 * Methods for setting/adding values
		 */

		public void SetFloat (uint index, double value, bool global) => (global ? globalFloats : funcFloats)[index] = value;
		public void SetIdentifier (uint addr, uint index) => identTable[addr] = index;
		public void SetOp (uint index, uint op) => code[index] = op;
		public void AddLineBreakPair (uint value) => lineBreakPairs.Add (value);

		/**
		 * Methods for validating values
		 */

		public bool HasString (uint index, bool global) => (global ? globalStrings : funcStrings).Has (index);
		public bool HasFloat (uint index, bool global) => index < (global ? globalFloats : funcFloats).Length;
		public bool HasIdentifierAt (uint addr) => identTable.ContainsKey (addr);

		/**
		 * Methods for getting the size of various sections
		 */

		public int StringTableSize (bool global) => (global ? globalStrings : funcStrings).Size;
		public int FloatTableSize (bool global) => (global ? globalFloats : funcFloats).Length;
		public int IdentTableSize => identTable.Count;
		public int CodeSize => code.Length;
	}
}
