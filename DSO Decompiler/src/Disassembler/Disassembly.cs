using System.Collections.Generic;

namespace DSODecompiler.Disassembler
{
	public class Disassembly
	{
		public Instruction EntryPoint { get; set; } = null;

		public Dictionary<uint, Instruction> Instructions { get; } = new Dictionary<uint, Instruction> ();

		public Instruction Add (Instruction instruction)
		{
			Instructions[instruction.Addr] = instruction;

			if (EntryPoint == null)
			{
				EntryPoint = instruction;
			}

			return instruction;
		}

		public bool Has (uint addr) => Instructions.ContainsKey (addr);
		public Instruction Get (uint addr) => Has (addr) ? Instructions[addr] : null;
		public Instruction this[uint addr] => Get (addr);
	}
}
