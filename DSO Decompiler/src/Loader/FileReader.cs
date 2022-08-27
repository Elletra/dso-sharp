using System.IO;
using System.Text;

namespace DsoDecompiler.Loader
{
	/// <summary>
	/// Mostly a wrapper for BinaryReader, but with some added methods specifically for DSO file reading.
	/// </summary>
	public class FileReader
	{
		protected BinaryReader reader = null;

		public FileReader (string filePath)
		{
			reader = new BinaryReader (new FileStream (filePath, FileMode.Open));
		}

		public bool IsEOF () => reader.BaseStream.Position >= reader.BaseStream.Length;

		public byte ReadByte () => reader.ReadByte ();
		public uint ReadUInt () => reader.ReadUInt32 ();
		public ulong ReadULong () => reader.ReadUInt64 ();
		public double ReadDouble () => reader.ReadDouble ();

		public uint ReadOp ()
		{
			var op = ReadByte ();

			return op == 0xFF ? ReadUInt () : op;
		}

		public string ReadString (uint numChars)
		{
			string str = "";

			for (uint i = 0; i < numChars; i++)
			{
				str += (char) ReadByte ();
			}

			return str;
		}
	}
}
