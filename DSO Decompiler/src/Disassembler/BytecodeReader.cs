using DSODecompiler.Loader;

namespace DSODecompiler.Disassembler
{
	public class BytecodeReader
	{
		protected FileData fileData = null;
		protected uint index = 0;

		public uint Index => index;
		public int CodeSize => fileData.CodeSize;
		public bool IsAtEnd => index >= fileData.CodeSize;

		public BytecodeReader(FileData data)
		{
			fileData = data;
			index = 0;
		}

		public uint Read () => fileData.Op(index++);
		public bool ReadBool () => Read() != 0;
		public char ReadChar () => (char) Read();
		public string ReadIdent () => fileData.Identifer(index, Read());
		public string ReadString (uint value, bool global) => fileData.StringTableValue(value, global);
		public double ReadFloat (uint value, bool global) => fileData.FloatTableValue(value, global);

		public uint Peek () => fileData.Op(index);
		public bool PeekBool () => Peek() != 0;
		public char PeekChar () => (char) Peek();
	}
}
