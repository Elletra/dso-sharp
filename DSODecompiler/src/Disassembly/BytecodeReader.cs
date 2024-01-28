using DSODecompiler.Loader;

namespace DSODecompiler.Disassembly
{
	public class BytecodeReader
	{
		private FileData fileData = null;

		public uint Index { get; private set; } = 0;

		/**
		 * There's probably some stupid way to nest function declarations inside each other, but that
		 * would be much more complicated, so let's just keep it simple for now.
		 *
		 * TODO: Maybe someday.
		 */
		public FunctionInstruction Function { get; set; } = null;
		public bool InFunction => Function != null;

		public int Size => fileData.CodeSize;
		public bool IsAtEnd => Index >= fileData.CodeSize;

		public BytecodeReader (FileData data)
		{
			fileData = data;
		}

		public uint Read () => fileData.GetOp(Index++);
		public bool ReadBool () => Read() != 0;
		public char ReadChar () => (char) Read();
		public string ReadIdent () => fileData.GetIdentifer(Index, Read());
		public string ReadString () => fileData.GetStringTableValue(Read(), global: !InFunction);
		public double ReadDouble () => fileData.GetFloatTableValue(Read(), global: !InFunction);
	}
}
