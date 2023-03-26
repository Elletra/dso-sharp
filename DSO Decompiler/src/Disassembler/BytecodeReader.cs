using DSODecompiler.Loader;

namespace DSODecompiler.Disassembler
{
	public class BytecodeReader
	{
		protected FileData fileData = null;

		public uint Index { get; protected set; } = 0;
		public FunctionInstruction Function { get; set; } = null;

		/// <summary>
		/// There's probably a stupid way to nest function declarations inside each other, but that
		/// would require having something more complicated. We're keeping it simple for now, so
		/// let's just do it this way.<br/><br/>
		///
		/// TODO: Maybe someday.
		/// </summary>
		public bool InFunction => Function != null;

		public int Size => fileData.CodeSize;
		public bool IsAtEnd => Index >= fileData.CodeSize;

		public BytecodeReader(FileData data)
		{
			fileData = data;
			Index = 0;
		}

		public uint Read () => fileData.GetOp(Index++);
		public bool ReadBool () => Read() != 0;
		public char ReadChar () => (char) Read();
		public string ReadIdent () => fileData.GetIdentifer(Index, Read());
		public string ReadString () => fileData.GetStringTableValue(Read(), global: !InFunction);
		public double ReadDouble () => fileData.GetFloatTableValue(Read(), global: !InFunction);

		public uint Peek () => fileData.GetOp(Index);
		public bool PeekBool () => Peek() != 0;
		public char PeekChar () => (char) Peek();

		public void Skip (uint amount = 1) => Index += amount;
	}
}
