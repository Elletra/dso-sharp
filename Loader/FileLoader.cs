/**
 * FileLoader.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the DSO Sharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */
namespace DSO.Loader
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
		static public uint ReadFileVersion(string filePath)
		{
			var reader = new FileReader(filePath);
			var version = reader.ReadUInt();

			reader.Close();

			return version;
		}

		protected FileReader _reader = new();

		/// <summary>
		/// Loads a DSO file, parses it, and returns the parsed data.
		/// </summary>
		/// <exception cref="FileLoaderException">
		/// <see cref="ReadHeader"/> throws if the DSO file has the wrong version.
		/// </exception>
		public virtual FileData LoadFile(string filePath)
		{
			_reader?.Close();
			_reader = new(filePath);

			var data = ReadHeader();

			ReadTables(data);

			var (codeSize, lineBreaks) = ReadCode(data);

			ReadLineBreaks(codeSize, lineBreaks);
			ReadIdentifierTable(data);

			_reader?.Close();

			return data;
		}

		public void Close() => _reader?.Close();

		/// <summary>
		/// Reads the DSO file header and returns a <see cref="FileData"/> instance.
		/// </summary>
		protected virtual FileData ReadHeader() => new(_reader.ReadUInt());

		protected virtual void ReadTables(FileData data)
		{
			ReadStringTable(data, global: true);
			ReadFloatTable(data, global: true);
			ReadStringTable(data, global: false);
			ReadFloatTable(data, global: false);
		}

		protected virtual void ReadStringTable(FileData data, bool global)
		{
			var table = new StringTable(_reader.ReadString(), global);

			if (global)
			{
				data.GlobalStringTable = table;
			}
			else
			{
				data.FunctionStringTable = table;
			}
		}

		protected virtual void ReadFloatTable(FileData data, bool global)
		{
			var size = _reader.ReadUInt();
			var table = new FloatTable(size, global);

			for (uint i = 0; i < size; i++)
			{
				table[i] = _reader.ReadDouble();
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

		protected virtual Tuple<uint, uint> ReadCode(FileData data)
		{
			var size = _reader.ReadUInt();
			var lineBreaks = _reader.ReadUInt();

			data.Code = new uint[size];

			bool v34 = data.Version == 34;

			for (uint i = 0; i < size; i++)
			{
				data.Code[i] = _reader.ReadOp(v34);
			}

			return new(size, lineBreaks);
		}

		/// <summary>
		/// To be perfectly honest, I don't really know what the line break stuff is for, but it's
		/// part of the file format so we have to parse it.
		/// </summary>
		protected virtual void ReadLineBreaks(uint codeSize, uint lineBreaks)
		{
			var totalSize = codeSize + lineBreaks * 2;

			for (uint i = codeSize; i < totalSize; i++)
			{
				_reader.ReadUInt();
			}
		}

		/// <summary>
		/// Reads identifier table to insert proper string table indices into parts of the code.
		/// </summary>
		protected virtual void ReadIdentifierTable(FileData data)
		{
			var identifiers = _reader.ReadUInt();

			while (identifiers-- > 0)
			{
				var index = _reader.ReadUInt();
				var count = _reader.ReadUInt();

				while (count-- > 0)
				{
					var ip = _reader.ReadUInt();

					data.Code[ip] = index;
					data.IdentifierTable[ip] = index;
				}
			}
		}
	}
}
