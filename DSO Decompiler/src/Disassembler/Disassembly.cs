using System.Collections.Generic;

namespace DSODecompiler.Disassembler
{
	public class Disassembly
	{
		protected List<Instruction> instructions = new();
		protected Dictionary<uint, Instruction> addrToInstruction = new();

		public int Count => instructions.Count;

		public Instruction EntryPoint { get => Get(0); }

		public bool Has (uint addr) => addrToInstruction.ContainsKey(addr);
		public bool HasAt (uint index) => index < instructions.Count;

		public Instruction Get (uint addr) => Has(addr) ? addrToInstruction[addr] : null;
		public Instruction GetAt (uint index) => HasAt(index) ? instructions[(int) index] : null;

		public bool Add (Instruction instruction)
		{
			if (Has(instruction.Addr))
			{
				return false;
			}

			instructions.Add(instruction);
			addrToInstruction.Add(instruction.Addr, instruction);

			return true;
		}

		public IEnumerable<Instruction> GetInstructions ()
		{
			foreach (var instruction in instructions)
			{
				yield return instruction;
			}
		}
	}
}
