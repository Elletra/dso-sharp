using System;

using DSODecompiler.Loader;
using DSODecompiler.Opcodes;

namespace DSODecompiler.Disassembly
{
	public class BytecodeReader
	{
		protected FileData fileData = null;
		protected uint index = 0;

		public uint FunctionEnd { get; set; } = 0;

		/* There's probably a stupid way to nest function declarations inside each other, and that
		   would require having a function stack instead, but since we're keeping it simple for now,
		   let's just do it this way.

		   Tentative TODO: Maybe someday. */
		public bool InFunction => FunctionEnd > 0;

		public uint Index => index;
		public int Size => fileData.CodeSize;
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
		public string ReadString () => fileData.StringTableValue(Read(), global: !InFunction);
		public double ReadDouble () => fileData.FloatTableValue(Read(), global: !InFunction);

		public uint Peek () => fileData.Op(index);
		public bool PeekBool () => Peek() != 0;
		public char PeekChar () => (char) Peek();

		public void Skip (uint amount = 1) => index += amount;
	}
}
