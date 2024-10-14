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

		public string? Get(uint index) => Has(index) ? table[index] : null;
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
