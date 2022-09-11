using System.Collections.Generic;

namespace DSODecompiler.Disassembler
{
	public class Instruction
	{
		public Opcodes.Ops Op { get; }
		public uint Addr { get; }

		public readonly List<uint> Operands = new List<uint> ();

		public Instruction (uint op, uint addr)
		{
			Op = (Opcodes.Ops) op;
			Addr = addr;
		}

		public uint this[int index] => index == 0 ? (uint) Op : Operands[index - 1];

		public int Size => Operands.Count + 1;
	}
}
