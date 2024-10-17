/**
 * FileReader.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the DSO Sharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

namespace DSO.Loader
{
	/// <summary>
	/// Mostly a wrapper for BinaryReader, but with some added methods specifically for DSO file reading.
	/// </summary>
	public class FileReader
	{
		private readonly BinaryReader reader = null;

		public bool IsEOF => reader.BaseStream.Position >= reader.BaseStream.Length;

		public FileReader() { }

		public FileReader(string filePath)
		{
			reader = new(new FileStream(filePath, FileMode.Open));
		}

		public void Close() => reader?.Close();

		public byte ReadByte() => reader.ReadByte();
		public uint ReadUInt() => reader.ReadUInt32();
		public double ReadDouble() => reader.ReadDouble();

		public uint ReadOp()
		{
			var op = ReadByte();

			return op == 0xFF ? ReadUInt() : op;
		}

		public string ReadString() => ReadString(ReadUInt());

		public string ReadString(uint chars)
		{
			string str = "";

			for (uint i = 0; i < chars; i++)
			{
				str += (char) ReadByte();
			}

			return str;
		}
	}
}
