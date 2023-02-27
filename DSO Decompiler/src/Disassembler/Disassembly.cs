using System.Collections.Generic;

namespace DSODecompiler.Disassembler
{
	public class Disassembly
	{
		protected Dictionary<uint, Instruction> instructions = new();
		protected HashSet<uint> jumpTargets = new();

		public Instruction EntryPoint { get => Get(0); }

		public bool Has (uint addr) => instructions.ContainsKey(addr);
		public Instruction Get (uint addr) => Has(addr) ? instructions[addr] : null;
		public Instruction this[uint addr] => Get(addr);

		public void AddJumpTarget (uint addr) => jumpTargets.Add(addr);
		public bool IsJumpTarget (uint addr) => jumpTargets.Contains(addr);

		public bool Add (Instruction instruction)
		{
			if (Has(instruction.Addr))
			{
				return false;
			}

			instructions.Add(instruction.Addr, instruction);

			return true;
		}
	}
}
