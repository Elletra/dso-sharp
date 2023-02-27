namespace DSODecompiler.Loader
{
	/// <summary>
	/// Loads DSO files, parses them, and returns the parsed data.<br/>
	/// <br/>
	/// NOTE: Does not parse bytecode! It simply breaks up DSO files into their different sections.
	/// </summary>
	public class FileLoader
	{
		public class Exception : System.Exception
		{
			public Exception () {}
			public Exception (string message) : base (message) {}
			public Exception (string message, System.Exception inner) : base (message, inner) {}
		}

		protected FileReader reader = null;

		/// <summary>
		/// Loads a DSO file, parses it, and returns the parsed data.
		/// </summary>
		/// <exception cref="FileLoader.Exception">
		/// `ParseHeader()` throws FileLoader.Exception if the DSO file has the wrong version.
		/// </exception>
		/// <param name="filePath"></param>
		/// <param name="version"></param>
		/// <returns>The parsed data.</returns>
		public FileData LoadFile (string filePath, uint version)
		{
			reader = new FileReader(filePath);

			var data = new FileData(version);

			ParseHeader(data);
			ParseStringTable(data, global: true);
			ParseFloatTable(data, global: true);
			ParseStringTable(data, global: false);
			ParseFloatTable(data, global: false);
			ParseCode(data);
			ParseIdentTable(data);

			return data;
		}

		/// <summary>
		/// Parses the DSO file header to make sure it has the right version, throwing an exception
		/// if it does not.
		/// </summary>
		/// <exception cref="FileLoader.Exception">If file has the wrong version.</exception>
		/// <param name="data"></param>
		protected void ParseHeader (FileData data)
		{
			var fileVersion = reader.ReadUInt();

			if (fileVersion != data.Version)
			{
				throw new Exception($"Invalid DSO version: Expected {data.Version}, got {fileVersion}");
			}
		}

		protected void ParseStringTable (FileData data, bool global)
		{
			var table = reader.ReadString(reader.ReadUInt());

			data.SetStringTable(UnencryptString(table), global);
		}

		protected void ParseFloatTable (FileData data, bool global)
		{
			var size = reader.ReadUInt();

			data.InitFloatTable(size, global);

			for (uint i = 0; i < size; i++)
			{
				data.SetFloat(i, reader.ReadDouble(), global);
			}
		}

		protected void ParseCode (FileData data)
		{
			var size = reader.ReadUInt();
			var lineBreaks = reader.ReadUInt();

			data.InitCode(size);

			for (uint i = 0; i < size; i++)
			{
				data.SetOp(i, reader.ReadOp());
			}

			ParseLineBreaks(data, size, lineBreaks);
		}

		/// <summary>
		/// To be perfectly honest, I don't really know what the line break stuff is for, but it's
		/// part of the file format so we have to parse it.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="codeSize"></param>
		/// <param name="numPairs"></param>
		protected void ParseLineBreaks (FileData data, uint codeSize, uint numPairs)
		{
			var totalSize = codeSize + numPairs * 2;

			for (uint i = codeSize; i < totalSize; i++)
			{
				data.AddLineBreakPair(reader.ReadUInt());
			}
		}

		/// <summary>
		/// Parses identifier table, replacing bits of the code with proper string table indices.
		/// </summary>
		/// <param name="data"></param>
		protected void ParseIdentTable (FileData data)
		{
			var identifiers = reader.ReadUInt();

			while (identifiers-- > 0)
			{
				var index = reader.ReadUInt();
				var count = reader.ReadUInt();

				while (count-- > 0)
				{
					var ip = reader.ReadUInt();

					data.SetOp(ip, index);
					data.SetIdentifier(ip, index);
				}
			}
		}

		/// <summary>
		/// Unencrypt Blockland's string tables.
		/// </summary>
		/// <param name="str"></param>
		/// <returns>The unencrypted string.</returns>
		protected string UnencryptString (string str)
		{
			var key = "cl3buotro";
			var unencrypted = "";

			for (var i = 0; i < str.Length; i++)
			{
				unencrypted += (char) (str[i] ^ key[i % 9]);
			}

			return unencrypted;
		}
	}
}
