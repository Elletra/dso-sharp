namespace DSO.Decompiler.Loader
{
	public class FileLoaderException : Exception
	{
		public FileLoaderException() { }
		public FileLoaderException(string message) : base(message) { }
		public FileLoaderException(string message, Exception inner) : base(message, inner) { }
	}

	/// <summary>
	/// Loads a DSO file and splits it up into different sections.
	/// </summary>
	public class FileLoader
	{
		private FileReader reader = new();

		/// <summary>
		/// Loads a DSO file, parses it, and returns the parsed data.
		/// </summary>
		/// <exception cref="FileLoaderException">
		/// <see cref="ReadHeader"/> throws if the DSO file has the wrong version.
		/// </exception>
		public FileData LoadFile(string filePath, uint version)
		{
			reader = new(filePath);

			var data = new FileData(version);

			ReadHeader(data);
			ReadStringTable(data, global: true);
			ReadFloatTable(data, global: true);
			ReadStringTable(data, global: false);
			ReadFloatTable(data, global: false);
			
			var (codeSize, lineBreaks) = ReadCode(data);

			ReadLineBreaks(codeSize, lineBreaks);
			ReadIdentifierTable(data);

			return data;
		}

		/// <summary>
		/// Reads the DSO file header to make sure it has the right version, and throws an exception
		/// if it does not.
		/// </summary>
		/// <exception cref="FileLoaderException">If file has the wrong version.</exception>
		private void ReadHeader(FileData data)
		{
			var fileVersion = reader.ReadUInt();

			if (fileVersion != data.Version)
			{
				throw new FileLoaderException($"Invalid DSO version: Expected {data.Version}, got {fileVersion}");
			}
		}

		private void ReadStringTable(FileData data, bool global)
		{
			var table = new StringTable(UnencryptString(reader.ReadString()));

			if (global)
			{
				data.GlobalStringTable = table;
			}
			else
			{
				data.FunctionStringTable = table;
			}
		}

		private void ReadFloatTable(FileData data, bool global)
		{
			var size = reader.ReadUInt();
			var table = new double[size];

			for (uint i = 0; i < size; i++)
			{
				table[i] = reader.ReadDouble();
			}

			if (global)
			{
				data.GlobalFloatTable = table;
			}
			else
			{
				data.FunctionFloatTable = table;
			}
		}

		private Tuple<uint, uint> ReadCode(FileData data)
		{
			var size = reader.ReadUInt();
			var lineBreaks = reader.ReadUInt();

			data.Code = new uint[size];

			for (uint i = 0; i < size; i++)
			{
				data.Code[i] = reader.ReadOp();
			}

			return new(size, lineBreaks);
		}

		/// <summary>
		/// To be perfectly honest, I don't really know what the line break stuff is for, but it's
		/// part of the file format so we have to parse it.
		/// </summary>
		private void ReadLineBreaks(uint codeSize, uint lineBreaks)
		{
			var totalSize = codeSize + lineBreaks * 2;

			for (uint i = codeSize; i < totalSize; i++)
			{
				reader.ReadUInt();
			}
		}

		/// <summary>
		/// Reads identifier table to insert proper string table indices into parts of the code.
		/// </summary>
		private void ReadIdentifierTable(FileData data)
		{
			var identifiers = reader.ReadUInt();

			while (identifiers-- > 0)
			{
				var index = reader.ReadUInt();
				var count = reader.ReadUInt();

				while (count-- > 0)
				{
					var ip = reader.ReadUInt();

					data.Code[ip] = index;
					data.IdentifierTable[ip] = index;
				}
			}
		}

		/// <summary>
		/// Unencrypt Blockland's string tables.
		/// </summary>
		/// <returns>The unencrypted string.</returns>
		static private string UnencryptString(string str)
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
