using System.Collections.Generic;

namespace DsoDecompiler.Disassembler
{
	public class Instruction
	{
		public uint Op { get; }
		public uint Addr { get; }

		public readonly List<uint> Operands = new List<uint> ();

		public Instruction (uint op, uint addr)
		{
			Op = op;
			Addr = addr;
		}

		public uint this[int index] => index == 0 ? Op : Operands[index - 1];

		public int Size => Operands.Count + 1;
	}
}
