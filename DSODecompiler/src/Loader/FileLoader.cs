namespace DSODecompiler.Loader
{
	/// <summary>
	/// Loads a DSO file and splits it up into different sections.
	/// </summary>
	public class FileLoader
	{
		public class Exception : System.Exception
		{
			public Exception () {}
			public Exception (string message) : base (message) {}
			public Exception (string message, System.Exception inner) : base (message, inner) {}
		}

		private FileReader reader = null;

		/// <summary>
		/// Loads a DSO file, parses it, and returns the parsed data.
		/// </summary>
		/// <exception cref="Exception">
		/// <see cref="ReadHeader"/> throws if the DSO file has the wrong version.
		/// </exception>
		/// <param name="filePath"></param>
		/// <param name="version"></param>
		/// <returns><see cref="FileData"/> instance containing the parsed data.</returns>
		public FileData LoadFile (string filePath, uint version)
		{
			reader = new FileReader(filePath);

			var data = new FileData(version);

			ReadHeader(data);
			ReadStringTable(data, global: true);
			ReadFloatTable(data, global: true);
			ReadStringTable(data, global: false);
			ReadFloatTable(data, global: false);
			ReadCode(data);
			ReadIdentifierTable(data);

			return data;
		}

		/// <summary>
		/// Reads the DSO file header to make sure it has the right version, and throws an exception
		/// if it does not.
		/// </summary>
		/// <exception cref="Exception">If file has the wrong version.</exception>
		/// <param name="data"></param>
		private void ReadHeader (FileData data)
		{
			var fileVersion = reader.ReadUInt();

			if (fileVersion != data.Version)
			{
				throw new Exception($"Invalid DSO version: Expected {data.Version}, got {fileVersion}");
			}
		}

		private void ReadStringTable (FileData data, bool global)
		{
			var table = reader.ReadString(reader.ReadUInt());

			data.CreateStringTable(UnencryptString(table), global);
		}

		private void ReadFloatTable (FileData data, bool global)
		{
			var size = reader.ReadUInt();

			data.CreateFloatTable(size, global);

			for (uint i = 0; i < size; i++)
			{
				data.SetFloat(i, reader.ReadDouble(), global);
			}
		}

		private void ReadCode (FileData data)
		{
			var size = reader.ReadUInt();
			var lineBreaks = reader.ReadUInt();

			data.InitCode(size);

			for (uint i = 0; i < size; i++)
			{
				data.SetOp(i, reader.ReadOp());
			}

			ReadLineBreaks(data, size, lineBreaks);
		}

		/// <summary>
		/// To be perfectly honest, I don't really know what the line break stuff is for, but it's
		/// part of the file format so we have to parse it.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="codeSize"></param>
		/// <param name="numPairs"></param>
		private void ReadLineBreaks (FileData data, uint codeSize, uint numPairs)
		{
			var totalSize = codeSize + numPairs * 2;

			for (uint i = codeSize; i < totalSize; i++)
			{
				data.AddLineBreakPair(reader.ReadUInt());
			}
		}

		/// <summary>
		/// Reads identifier table to insert proper string table indices into parts of the code.
		/// </summary>
		/// <param name="data"></param>
		private void ReadIdentifierTable (FileData data)
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
		private string UnencryptString (string str)
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
